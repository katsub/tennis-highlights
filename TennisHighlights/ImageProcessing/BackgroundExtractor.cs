using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

namespace TennisHighlights
{
    /// <summary>
    /// The background extractor
    /// </summary>
    public class BackgroundExtractor : IDisposable
    {
        /// <summary>
        /// The cache backgrounds
        /// </summary>
        private const bool _cacheBackgrounds = false;
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
        private readonly OpenCvSharp.Size _size;
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
        /// The refresh increment
        /// </summary>
        private readonly int _refreshIncrement;
        /// <summary>
        /// The loaded backgrounds
        /// </summary>
        private readonly SortedList<int, MatOfByte3> _loadedBackgrounds = new SortedList<int, MatOfByte3>();
        /// <summary>
        /// The allocated mat pool
        /// </summary>
        private readonly List<MatOfByte3> _allocatedMatPool = new List<MatOfByte3>();
        /// <summary>
        /// The sampled frames
        /// </summary>
        private readonly List<MatOfByte3> _sampledFrames = new List<MatOfByte3>();
        /// <summary>
        /// The last built background
        /// </summary>
        public int LastBuiltBackground { get; private set; }
        /// <summary>
        /// The parsed frames
        /// </summary>
        private readonly int _parsedFrames;
        /// <summary>
        /// The frames to extract
        /// </summary>
        private readonly int _framesToExtract;
        /// <summary>
        /// The is busy
        /// </summary>
        private bool _isBusy;
        /// <summary>
        /// The asked to stop
        /// </summary>
        private bool _askedToStop;
        /// <summary>
        /// The background cache. Used so that extractors can access background without locking
        /// </summary>
        private readonly MatOfByte3[] _backgroundCache;
        /// <summary>
        /// Gets a value indicating whether this instance has frames left to extract.
        /// </summary>
        private bool _hasFramesLeftToExtract => LastBuiltBackground < _videoInfo.TotalFrames && LastBuiltBackground < _framesToExtract;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundExtractor" /> class.
        /// </summary>
        /// <param name="frameExtractor">The frame extractor.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="startingFrame">The starting frame.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="parsedFrames">The parsed frames.</param>
        /// <param name="framesToExtract">The frames to extract.</param>
        public BackgroundExtractor(VideoFrameExtractor frameExtractor, OpenCvSharp.Size targetSize, int startingFrame, BackgroundExtractionSettings settings, VideoInfo videoInfo,
                                   int parsedFrames, int framesToExtract)
        {
            _parsedFrames = parsedFrames;

            _framesToExtract = framesToExtract;
            _videoInfo = videoInfo;
            _frameExtractor = frameExtractor;
            _settings = settings;
            _size = targetSize;
            _refreshIncrement = settings.NumberOfSamples * settings.FramesPerSample;

            _backgroundCache = new MatOfByte3[(int)Math.Ceiling((double)videoInfo.TotalFrames / _refreshIncrement)];

            while (LastBuiltBackground + _refreshIncrement < _parsedFrames)
            {
                LastBuiltBackground += _refreshIncrement;
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
                if (!_isBusy && _loadedBackgrounds.Count < 4 && _hasFramesLeftToExtract)
                {
                    _isBusy = true;

                    await Task.Run(() => LoadBackgroundAndAddIt());
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
        private async void LoadBackgroundAndAddIt()
        {
            var background = await LoadBackground(LastBuiltBackground);

            //We update lastbuiltbackground so that we know it has been built for this frame. This needs the lock because last built background
            //decides if we get loadedbackgrounds or not
            lock (_loadedBackgroundsLock)
            {
                if (background != null)
                {
                    _loadedBackgrounds.Add(LastBuiltBackground + _refreshIncrement, background);

                    _backgroundCache[((LastBuiltBackground + _refreshIncrement) / _refreshIncrement) - 1] = background;
                }

                LastBuiltBackground += _refreshIncrement;
            }

            _isBusy = false;
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
            while (LastBuiltBackground <= frameIndex)
            {
                await Task.Delay(200);

                //If it's done extracting and the index isn't available, return the last one added
                if (!_hasFramesLeftToExtract)
                {
                    //Might be null if it didn't need to build any backgrounds because all frames have been parsed, for example
                    return _backgroundCache.LastOrDefault(c => c != null);
                }
            }

            var indexInCache = (int)Math.Floor((double)frameIndex / _refreshIncrement);
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
            var bgFinalFrame = startFrame + _refreshIncrement;

            var backgroundFileName = "background_" + bgFinalFrame + ".bmp";

            MatOfByte3 background = null;

            using (var cachedBMP = FileManager.ReadPersistentBitmapFile(backgroundFileName))
            {
                if (cachedBMP != null)
                {
                    Logger.Log(LogType.Information, "Loading background " + bgFinalFrame);

                    background = GetMatFromPool();

                    BitmapConverter.ToMat(cachedBMP, background);
                }
            }

            if (background == null)
            {
                Logger.Log(LogType.Information, "Calculating background " + bgFinalFrame);

                //If the newBackground is null, this means it has reached the end of the video: we use the previous background then
                var newBackground = await ExtractBackground(startFrame);
                if (newBackground != null)
                {
                    background = newBackground;
                }
                else if (background == null)
                {
                    //If the new background was null and we didn't have a background before (because reading from cache), we restore the most
                    //recent background from cache
                    using (var cachedBMP = FileManager.ReadPersistentBitmapFile("background_" + startFrame + ".bmp"))
                    {
                        if (cachedBMP != null)
                        {
                            background = GetMatFromPool();

                            BitmapConverter.ToMat(cachedBMP, background);
                        }
                    }
                }

                if (ConditionalCompilation.Debug && _cacheBackgrounds)
                {
                    FileManager.WritePersistentFile(backgroundFileName, background);
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
            //for that... not a huge priority right now
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

            var sampleSize = _settings.ClusteringSize;
            var maxNumberOfSamples = _settings.NumberOfSamples;
            var framesPerSample = _settings.FramesPerSample;

            if (sampleSize % 2 == 0) { throw new Exception("Sample size must be an odd number"); }

            var i = 0;
            var numberOfSampledFrames = 0;

            _sampledFrames.Clear();

            while (numberOfSampledFrames < maxNumberOfSamples)
            {
                var getFrameIndex = startFrame + i;

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
                        Logger.Log(LogType.Warning, "Could not get frame " + getFrameIndex + " althought the background extractor was not asked to stop.");

                        return null;
                    }
                }

                i++;

                if (i % framesPerSample == 0)
                {
                    _sampledFrames.Add(frame);

                    numberOfSampledFrames++;
                }

                if (numberOfSampledFrames >= maxNumberOfSamples) { break; }
            }

            return BuildBackgroundByClustering(sampleSize, _sampledFrames);
        }

        /// <summary>
        /// Builds the background by clustering.
        /// </summary>
        /// <param name="sampleSize">Size of the sample.</param>
        /// <param name="sampledFrames">The sampled frames.</param>
        private MatOfByte3 BuildBackgroundByClustering(int sampleSize, List<MatOfByte3> sampledFrames)
        {
            var halfStep = (sampleSize - 1) / 2;

            var bgMat = GetMatFromPool();
            var bgIndexer = bgMat.GetIndexer();

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

                        for (int p = 0; p < sampledFrames.Count; p++)
                        {
                            var label = resultMatIndexer[p, 0];

                            clusterCounts[label]++;
                        }

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

                    var chosenFrameIndexer = sampledFrames[chosenFrameIndex].GetIndexer();

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
