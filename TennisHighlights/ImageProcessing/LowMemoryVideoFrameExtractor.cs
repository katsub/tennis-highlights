using OpenCvSharp;
using System;
using System.Diagnostics.Contracts;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The low memory video frame extractor
    /// </summary>
    public class LowMemoryVideoFrameExtractor : IDisposable
    {
        /// <summary>
        /// The video information
        /// </summary>
        public readonly VideoInfo VideoInfo;
        /// <summary>
        /// The target size
        /// </summary>
        public readonly Size TargetSize;
        /// <summary>
        /// The video capture
        /// </summary>
        private readonly VideoCapture _videoCapture;
        /// <summary>
        /// The current frame
        /// </summary>
        private int _currentFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="LowMemoryVideoFrameExtractor"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="videoInfo">The video information.</param>
        public LowMemoryVideoFrameExtractor(string filePath, Size targetSize, VideoInfo videoInfo)
        {
            VideoInfo = videoInfo;

            _videoCapture = new VideoCapture(filePath);
            TargetSize = targetSize;

            _mat = new MatOfByte3(VideoInfo.Height, VideoInfo.Width);
        }

        /// <summary>
        /// The mat
        /// </summary>
        private readonly MatOfByte3 _mat;

        /// <summary>
        /// Gets the frame.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="resizedMat">The resized mat.</param>
        public void GetFrame(int i, MatOfByte3 resizedMat)
        {
            if (i < _currentFrame) 
            {
                Contract.Assert(false);

                return; 
            }
                       
            while (_currentFrame < VideoInfo.TotalFrames)
            {
                _videoCapture.Read(_mat);

                _currentFrame++;

                if (_currentFrame - 1 == i)
                {
                    break;
                }
            }

            Cv2.Resize(_mat, resizedMat, TargetSize, 0, 0, InterpolationFlags.Nearest);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _videoCapture.Dispose();

            _mat.Dispose();
        }
    }
}
