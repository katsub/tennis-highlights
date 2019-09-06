using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;

namespace TennisHighlights
{
    /// <summary>
    /// The GUI utils
    /// </summary>
    public class GUIUtils
    {
        /// <summary>
        /// Gets the video preview image.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static Bitmap GetVideoPreviewImage(string filePath)
        {
            using (var video = new VideoCapture(filePath))
            using (var frame = new Mat())
            {
                video.Read(frame);

                return BitmapConverter.ToBitmap(frame);
            }
        }
    }
}
