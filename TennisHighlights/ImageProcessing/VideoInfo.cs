using OpenCvSharp;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The video info
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// Gets the frame rate.
        /// </summary>
        public double FrameRate { get; }
        /// <summary>
        /// Gets the total frames.
        /// </summary>
        public int TotalFrames { get; }
        /// <summary>
        /// Gets the height.
        /// </summary>
        public int Height { get; }
        /// <summary>
        /// Gets the width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInfo" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public VideoInfo(string filePath)
        {
            using (var video = new VideoCapture(filePath))
            {
                FrameRate = video.Fps;

                TotalFrames = video.FrameCount;

                Width = video.FrameWidth;
                Height = video.FrameHeight;
            }
        }
    }
}
