using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

namespace TennisHighlights
{
    /// <summary>
    /// The background extractor. Supplies backgrounds formed by clustering multiple frames sampled over a (relatively) long interval
    /// </summary>
    public class BackgroundExtractor : IDisposable
    {
         /// <summary>
        /// The loaded backgrounds lock
        /// </summary>
        private readonly object _loadedBackgroundsLock = new object();
        /// <summary>
        /// The allocated mat pool lock
        /// </summary>
        private readonly object _allocatedMatPoolLock = new object();
        /// <summary>
        /// The size
        /// </summary>
        private readonly Size _size;
        /// <summary>
        /// The frame extractor
        /// </summary>
        private readonly VideoFrameExtractor _frameExtractor;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly BackgroundExtractionSettings _settings;
        /// <summary>
        /// The video information
        /// </summary>
        private readonly VideoInfo _videoInfo;
        /// <summary>
        /// The frames per background: the amount of frames that can use this background (which are those in the interval used to calculate this background)
        /// </summary>
        private readonly int _framesPerBackground;
        /// <summary>
        /// The loaded backgrounds
        /// </summary>
        private readonly SortedList<int, MatOfByte3> _loadedBackgrounds = new SortedList<int, MatOfByte3>();
        /// <summary>
        /// The allocated mat pool
        /// </summary>
        private readonly List<MatOfByte3> _allocatedMatPool = new List<MatOfByte3>();
        /// <summary>
        /// The last built frame with a built background (the last frame that has a valid built background, this tells us the frameballextractor can
        /// process up to that frame, because after it we still don't have the background frame needed)
        /// </summary>
        public int LastFrameWithBuiltBackground { get; private set; }
        /// <summary>
        /// The last parsed frame (since it came from the log, we don't need backgrounds up to that point: all those frames have already been extracted
        /// and analysed in a previous run)
        /// </summary>
        private readonly int _lastParsedFrame;
        /// <summary>
        /// The last frame to extract (might be the last frame of the video or an early frame, if the user has selected the option to stop early
        /// </summary>
        private readonly int _lastFrameToExtract;
        /// <summary>
        /// The asked to stop: signals an early source has demanded the extractoin to stop
        /// </summary>
        private bool _askedToStop;
        /// <summary>
        /// The background cache. Used so that extractors can access background without locking
        /// </summary>
        private readonly MatOfByte3[] _backgroundCache;
        /// <summary>
        /// Gets a value indicating whether this instance has frames left to extract.
        /// </summary>
        private bool _hasFramesLeftToExtract => LastFrameWithBuiltBackground < _lastFrameToExtract;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundExtractor" /> class.
        /// </summary>
        /// <param name="frameExtractor">The frame extractor.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="startingFrame">The starting frame.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="lastParsedFrame">The last parsed frame.</param>
        /// <param name="lastFrameToExtract">The last frame to extract.</param>
        public BackgroundExtractor(VideoFrameExtractor frameExtractor, Size targetSize, int startingFrame, BackgroundExtractionSettings settings, VideoInfo videoInfo,
                                   int lastParsedFrame, int lastFrameToExtract)
        {
            _lastParsedFrame = lastParsedFrame;

            _lastFrameToExtract = lastFrameToExtract;
            _videoInfo = videoInfo;
            _frameExtractor = frameExtractor;
            _settings = settings;
            _size = targetSize;
            _framesPerBackground = settings.NumberOfSamples * settings.FramesPerSample;

            _backgroundCache = new MatOfByte3[(int)Math.Ceiling((double)videoInfo.TotalFrames / _framesPerBackground)];

            while (LastFrameWithBuiltBackground + _framesPerBackground < _lastParsedFrame)
            {
                LastFrameWithBuiltBackground += _framesPerBackground;
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop() => _askedToStop = true;

        /// <summary>
        /// Extracts the backgrounds in background task.
        /// </summary>
        public void ExtractBackgroundsInBackgroundTask() => Task.Run(() => ExtractBackgroundsInBackgroundTaskInternal());

        /// <summary>
        /// Extracts the backgrounds in background task internal.
        /// </summary>
        private async Task ExtractBackgroundsInBackgroundTaskInternal()
        {
            _askedToStop = false;

            while (!_askedToStop && _hasFramesLeftToExtract)
            {
                if (_loadedBackgrounds.Count < 4 && _hasFramesLeftToExtract)
                {
                    await LoadBackgroundAndAddIt();
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Loads the background and add it.
        /// </summary>
        private async Task LoadBackgroundAndAddIt()
        {
            var background = await LoadBackground(LastFrameWithBuiltBackground);

            //We update lastbuiltbackground so that we know it has been built for this frame. This needs the lock because the disposer will be trying
            //to access loaded backgrounds at the same time
            lock (_loadedBackgroundsLock)
            {
                if (background != null)
                {
                    _loadedBackgrounds.Add(LastFrameWithBuiltBackground + _framesPerBackground, background);

                    _backgroundCache[((LastFrameWithBuiltBackground + _framesPerBackground) / _framesPerBackground) - 1] = background;
                }

                LastFrameWithBuiltBackground += _framesPerBackground;
            }
        }

        /// <summary>
        /// Gets the mat from pool.
        /// </summary>
        private MatOfByte3 GetMatFromPool()
        {
            MatOfByte3 mat = null;

            lock (_allocatedMatPoolLock)
            {
                if (_allocatedMatPool.Any())
                {
                    mat = _allocatedMatPool.First();

                    _allocatedMatPool.RemoveAt(0);
                }
            }

            if (mat == null)
            {
                mat = new MatOfByte3(_size.Height, _size.Width);
            }

            return mat;
        }

        /// <summary>
        /// Gets the background.
        /// </summary>
        /// <param name="frameIndex">Index of the frame.</param>
        public async Task<MatOfByte3> GetBackground(int frameIndex)
        {
            while (LastFrameWithBuiltBackground <= frameIndex)
            {
                await Task.Delay(200);

                //If it's done extracting and the index isn't available, return the last one added
                if (!_hasFramesLeftToExtract)
                {
                    //Might be null if it didn't need to build any backgrounds because all frames have been parsed, for example
                    return _backgroundCache.LastOrDefault(c => c != null);
                }
            }

            var indexInCache = (int)Math.Floor((double)frameIndex / _framesPerBackground);
            //We return the oldest background that has a bigger index than the asked one
            //(For a refresh rate = 150 , a background with index 150 is built over using 0 to 150, so if we ask for frame 93,
            //that's the background we want)
            var background = _backgroundCache[indexInCache];

            if (background == null)
            {
                Logger.Log(LogType.Warning, "Background was null for frame " + frameIndex);
            }

            //Might be null if it didn't need to build any backgrounds because all frames have been parsed, for example
            return background ?? _backgroundCache.LastOrDefault(c => c != null);
        }

        /// <summary>
        /// Disposes backgrounds coming before the given frame index.
        /// </summary>
        /// <param name="frameIndex">Index of the frame.</param>
        public void DisposeBackgroundOlderThan(int frameIndex)
        {          
            foreach (var key in _loadedBackgrounds.Keys.ToList())
            {
                if (frameIndex > key)
                {
                    var backgroundToUnload = _loadedBackgrounds[key];

                    lock (_loadedBackgroundsLock)
                    {
                        _loadedBackgrounds.Remove(key);
                    }

                    lock (_allocatedMatPoolLock)
                    {
                        _allocatedMatPool.Add(backgroundToUnload);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the background.
        /// </summary>
        /// <param name="startFrame">The start frame.</param>
        private async Task<MatOfByte3> LoadBackground(int startFrame)
        {
            var bgFinalFrame = startFrame + _framesPerBackground;

            MatOfByte3 background = null;

            if (background == null)
            {
                Logger.Log(LogType.Information, "Calculating background " + bgFinalFrame);

                //If the newBackground is null, this means it has reached the end of the video: we use the previous background then
                var newBackground = await ExtractBackground(startFrame);
                if (newBackground != null)
                {
                    background = newBackground;
                }
            }

            return background;
        }

        /// <summary>
        /// Extracts the background.
        /// </summary>
        /// <param name="startFrame">The start frame.</param>
        /// <exception cref="System.Exception">Sample size must be an odd number</exception>
        private async Task<MatOfByte3> ExtractBackground(int startFrame)
        {
            MatOfByte3 firstFrame = null;

            //Extractor might begin waiting for a frame that's far away and will throw an exception when the video frame extractor is stopped.
            //Ideally, this should be able to stop waiting for the frame when the extractor is asked to stop, but it'll need a little refactorization
            //for that...
            try
            {
                firstFrame = await _frameExtractor.GetFrameAsync(startFrame);
            }
            catch
            {
                if (!_askedToStop)
                {
                    Logger.Log(LogType.Warning, "Could not get frame " + firstFrame + " althought the background extractor was not asked to stop.");

                    return null;
                }
            }

            var maxNumberOfSamples = _settings.NumberOfSamples;
            var framesPerSample = _settings.FramesPerSample;

            var sampledFrames = new List<MatOfByte3>();

            while (sampledFrames.Count < maxNumberOfSamples)
            {
                //Jump (framesPerSample) frames, sample a frame, then repeat: this gets us spaced frames which will have plenty of player movement on the
                //background (players and balls and other moving objects will spend little time on each part of the background and thus be ignored in the
                //clustering, which will be dominated by the actual background
                var getFrameIndex = startFrame + sampledFrames.Count * framesPerSample;

                if (getFrameIndex >= _videoInfo.TotalFrames) { return null; }

                if (_askedToStop) { return null; }

                MatOfByte3 frame = null;

                try
                {
                    frame = await _frameExtractor.GetFrameAsync(getFrameIndex);
                }
                catch
                {
                    if (!_askedToStop)
                    {
                        Logger.Log(LogType.Warning, "Could not get frame " + getFrameIndex + " although the background extractor was not asked to stop.");

                        return null;
                    }
                }

                sampledFrames.Add(frame);
            }

            return BuildBackgroundByClustering(sampledFrames);
        }

        /// <summary>
        /// Builds the background by clustering.
        /// </summary>
        /// <param name="sampledFrames">The sampled frames.</param>
        private MatOfByte3 BuildBackgroundByClustering(List<MatOfByte3> sampledFrames)
        {
            var sampleSize = _settings.ClusteringSize;

            if (sampleSize % 2 == 0) { throw new Exception("Sample size must be an odd number"); }

            var halfStep = (sampleSize - 1) / 2;

            var bgMat = GetMatFromPool();
            var bgIndexer = bgMat.GetIndexer();

            //We're gonna call those (height * width) / (sampleSize * sampleSize) times, might as well cache them since that number can be pretty big
            //And I recall seeing in the profiler that GetIndexer() was kinda slow
            var sampledFramesIndexers = sampledFrames.Select(s => s.GetIndexer()).ToList();

            //We build small patches of sampleSize we clusterize each one of them, in order to pick only the frames where that patch didn't have any
            //movement on it
            for (int x = 0; x < _size.Height; x += sampleSize)
            {
                for (int y = 0; y < _size.Width; y += sampleSize)
                {
                    var centerX = x + halfStep;
                    var centerY = y + halfStep;

                    if (centerX > _size.Height - 1)
                    {
                        centerX = _size.Height - 1;
                    }
                    if (centerY > _size.Width - 1)
                    {
                        centerY = _size.Width - 1;
                    }

                    var chosenFrameIndex = -1;

                    var k = 2;
                    using (var samplesMat = new MatOfFloat(sampledFrames.Count, 3))
                    using (var resultMat = new MatOfByte())
                    {
                        var samplesIndexer = samplesMat.GetIndexer();

                        for (int i = 0; i < sampledFrames.Count; i++)
                        {
                            var sampledPixel = sampledFrames[i].Get<Vec3b>(centerX, centerY);

                            samplesIndexer[i, 0] = sampledPixel[0];
                            samplesIndexer[i, 1] = sampledPixel[1];
                            samplesIndexer[i, 2] = sampledPixel[2];
                        }

                        //Classify between "is background" and "is moving object" (2 classes)
                        //We assume most of the time the image shows the background, and the moving object expects little time
                        //in any pixel but a little bit in all of them. Using k = 2 supposes there's only one moving object passing through 
                        //(if there are multiple, one of them could be placed in the background cluster by 'mistake')
                        Cv2.Kmeans(samplesMat, k, resultMat, TermCriteria.Both(5, 0.001), 3, KMeansFlags.PpCenters);

                        Span<int> clusterCounts = stackalloc int[k];

                        var resultMatIndexer = resultMat.GetIndexer();

                        //Count the number of samples in each cluster
                        for (int p = 0; p < sampledFrames.Count; p++)
                        {
                            var label = resultMatIndexer[p, 0];

                            clusterCounts[label]++;
                        }

                        //We assume the biggest one is the background
                        var biggestClusterIndex = 0;
                        var biggestClusterSize = clusterCounts[0];

                        for (int s = 1; s < k; s++)
                        {
                            if (biggestClusterSize < clusterCounts[s])
                            {
                                biggestClusterIndex = s;
                                biggestClusterSize = clusterCounts[s];
                            }
                        }

                        //We get one of the frames corresponding to that biggest cluster and assume it shows the background
                        for (int s = 0; s < sampledFrames.Count; s++)
                        {
                            if (resultMatIndexer[s, 0] == biggestClusterIndex)
                            {
                                chosenFrameIndex = s;

                                break;
                            }
                        }
                    }

                    var minX = Math.Max(0, centerX - halfStep);
                    var maxX = Math.Min(_size.Height - 1, centerX + halfStep);
                    var minY = Math.Max(0, centerY - halfStep);
                    var maxY = Math.Min(_size.Width - 1, centerY + halfStep);

                    var chosenFrameIndexer = sampledFramesIndexers[chosenFrameIndex];

                    //We write that patch on the final image
                    for (int ii = minX; ii <= maxX; ii++)
                    {
                        for (int jj = minY; jj <= maxY; jj++)
                        {
                            bgIndexer[ii, jj] = chosenFrameIndexer[ii, jj];
                        }
                    }
                }
            }

            return bgMat;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            Stop();

            foreach (var bg in _loadedBackgrounds)
            {
                bg.Value.Dispose();
            }

            foreach (var mat in _allocatedMatPool)
            {
                mat.Dispose();
            }
        }
    }
}
