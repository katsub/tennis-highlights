using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

namespace TennisHighlights
{
    /// <summary>
    /// The video frame extractor
    /// </summary>
    public class VideoFrameExtractor : IDisposable
    {
        /// <summary>
        /// The buffer size
        /// </summary>
        public const int BufferSize = 400;
        /// <summary>
        /// The video information
        /// </summary>
        public readonly VideoInfo VideoInfo;
        /// <summary>
        /// The target size
        /// </summary>
        public readonly OpenCvSharp.Size TargetSize;
        /// <summary>
        /// The video capture
        /// </summary>
        private readonly VideoCapture _videoCapture;
        /// <summary>
        /// The frame cache
        /// </summary>
        private readonly BusyMat[] _frameCache;
        /// <summary>
        /// The original size frames to dispose. When the target size is different from the original size, store allocated frames
        /// here so they can be disposed and recycled
        /// </summary>
        private readonly BusyMat[] _originalSizeFramesToDispose;
        /// <summary>
        /// The busy mat pool
        /// </summary>
        private readonly List<BusyMat> _busyMatPool = new List<BusyMat>();
        /// <summary>
        /// The resize busy mat pool
        /// </summary>
        private readonly List<BusyMat> _resizeBusyMatPool = new List<BusyMat>();
        /// <summary>
        /// The worker
        /// </summary>
        private readonly List<VideoFrameExtractorWorker> _workers;
        /// <summary>
        /// The destination rectangle
        /// </summary>
        public readonly Rectangle DestRect;
        /// <summary>
        /// The is extracting
        /// </summary>
        private bool _isExtracting = false;
        /// <summary>
        /// Gets the cache used size.
        /// </summary>
        private int _cacheUsedSize;
        /// <summary>
        /// Gets the free workers.
        /// </summary>
        public int FreeWorkers => _workers.Where(w => !w.IsBusy).Count();
        /// <summary>
        /// Gets the frames loaded.
        /// </summary>
        public int FramesLoaded { get; private set; }

        /// <summary>
        /// The has stopped. Confirms that the extraction has been stopped
        /// </summary>
        private bool _hasStopped;

        //TODO: use GetFrame() instead of GetFrame(index) so user is forced to get frames in sequence and dispose in sequence (it is already the case
        //but since code is not written intuitively, user may not do this and cause bugs
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoFrameExtractor" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="videoInfo">The video information.</param>
        public VideoFrameExtractor(string filePath, OpenCvSharp.Size targetSize, VideoInfo videoInfo)
        {
            VideoInfo = videoInfo;

            _frameCache = new BusyMat[VideoInfo.TotalFrames];

            if (targetSize.Height != VideoInfo.Height)
            {
                _originalSizeFramesToDispose = new BusyMat[VideoInfo.TotalFrames];
            }

            _videoCapture = new VideoCapture(filePath);
            TargetSize = targetSize;
            DestRect = new Rectangle(0, 0, TargetSize.Width, TargetSize.Height);

            _workers = Enumerable.Range(1, GeneralSettings.FrameExtractionWorkers).Select(i => new VideoFrameExtractorWorker(this)).ToList();
        }

        /// <summary>
        /// Extracts the frames from the video in a background task. 
        /// Extraction will stop if there are 50 frames currently non disposed, in order to prevent memory issues.
        /// </summary>
        public void ExtractFramesInBackgroundTask()
        {
            _isExtracting = true;

            Task.Run(() => ExtractFramesInBackgroundTaskInternal());
        }

        /// <summary>
        /// Extracts the frames in background task internal.
        /// </summary>
        private async void ExtractFramesInBackgroundTaskInternal()
        {
            try
            {
                for (int i = 0; i < VideoInfo.TotalFrames; i++)
                {
                    if (!_isExtracting) { break; }

                    var busyMat = GetMatFromAllocatedMatPool();

                    busyMat.SetBusy();

                    _videoCapture.Read(busyMat.Mat);

                    if (busyMat.Mat == null)
                    {
                        Logger.Log(LogType.Warning, "Video ended unexpectedly at frame " + i);
                        break;
                    }

                    var freeWorker = _workers.FirstOrDefault(w => !w.IsBusy);

                    while (freeWorker == null || _cacheUsedSize > BufferSize)
                    {
                        await Task.Delay(200);

                        freeWorker = _workers.FirstOrDefault(w => !w.IsBusy);
                    }

                    if (_isExtracting)
                    {
                        var resizeBusyMat = GetResizeMatFromAllocatedMatPool();

                        resizeBusyMat.SetBusy();

                        freeWorker.AssignFrame(i, busyMat, resizeBusyMat);

                        _cacheUsedSize++;
                        FramesLoaded++;

                        freeWorker.ProcessFrame();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Extraction exception: " + e.ToString());
            }
            finally
            {
                _isExtracting = false;
                _hasStopped = true;
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            //Tell the extractor to stop and wait until it actually stops
            _isExtracting = false;

            while (!_hasStopped)
            {
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Gets the mat from allocated mat pool.
        /// </summary>
        private BusyMat GetMatFromAllocatedMatPool()
        {
            //Not thread safe, but as long as there is only one task using it, it should be ok
            var freeMat = _busyMatPool.FirstOrDefault(b => !b.IsBusy);

            if (freeMat == null)
            {
                freeMat = new BusyMat(new MatOfByte3(VideoInfo.Height, VideoInfo.Width));

                _busyMatPool.Add(freeMat);
            }

            return freeMat;
        }

        /// <summary>
        /// Gets the resize mat from allocated mat pool.
        /// </summary>
        private BusyMat GetResizeMatFromAllocatedMatPool()
        {
            //Not thread safe, but as long as there is only one task using it, it should be ok
            var freeMat = _resizeBusyMatPool.FirstOrDefault(b => !b.IsBusy);

            if (freeMat == null)
            {
                freeMat = new BusyMat(new MatOfByte3(TargetSize.Height, TargetSize.Width));

                _resizeBusyMatPool.Add(freeMat);
            }

            return freeMat;
        }

        /// <summary>
        /// Adds the frame.
        /// </summary>
        /// <param name="frameIndex">Index of the frame.</param>
        /// <param name="mat">The mat.</param>
        public void AddFrame(int frameIndex, BusyMat mat, BusyMat originalFrame)
        {
            _frameCache[frameIndex] = mat;

            if (_originalSizeFramesToDispose != null)
            {
                _originalSizeFramesToDispose[frameIndex] = originalFrame;
            }
        }

        /// <summary>
        /// Gets the frame.
        /// </summary>
        /// <param name="frameIndex">The frame index.</param>
        public async Task<MatOfByte3> GetFrameAsync(int frameIndex)
        {
            while (_frameCache[frameIndex] == null)
            {
                if (!_isExtracting && !_workers.Any(w => w.IsBusy))
                { 
                    //Needs to check once more in case frame is added exactly between when we check it's null and if anyone is busy. If noone is busy and !extracting, then we know for sure that if
                    //frame is still null, then there's a problem
                    if (_frameCache[frameIndex] == null)
                    {
                        throw new FrameNotFoundException(frameIndex);
                    }
                }

                await Task.Delay(200);
            }

            return _frameCache[frameIndex].Mat;
        }

        /// <summary>
        /// Disposes the frame. In reality, finishes an access. After all accesses are finished, the mat will be recycled for reading another frame.
        /// </summary>
        /// <param name="frameIndex">The frame index.</param>
        public void DisposeFramesBefore(int frameIndex)
        {
            var removedFrames = 0;

            for (int i = frameIndex - 1; i >= 0; i--)
            {
                if (_frameCache[i] != null)
                {
                    _frameCache[i].FreeForUse();
                    _originalSizeFramesToDispose[i].FreeForUse();

                    _frameCache[i] = null;
                    _originalSizeFramesToDispose[i] = null;

                    removedFrames++;
                }
            }

            Interlocked.Add(ref _cacheUsedSize, -removedFrames);
        }

        /// <summary>
        /// Disposes this instance's unmanaged resources;
        /// </summary>
        public void Dispose()
        {
            Task.Run(() =>
            {
                Stop();

                while (_workers.Any(w => w.IsBusy)) { Task.Delay(1000); }

                foreach (var mat in _busyMatPool)
                {
                    mat.Mat.Dispose();
                }

                foreach (var worker in _workers)
                {
                    worker.Dispose();
                }

                foreach (var frame in _frameCache)
                {
                    if (frame != null)
                    {
                        frame.Mat.Dispose();
                    }
                }

                if (_originalSizeFramesToDispose != null)
                {
                    foreach (var frame in _originalSizeFramesToDispose)
                    {
                        if (frame != null)
                        {
                            frame.Mat.Dispose();
                        }
                    }
                }

                _videoCapture.Dispose();
            });
        }
    }
}
