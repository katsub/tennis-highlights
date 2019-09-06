using System.Drawing;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The progress info
    /// </summary>
    public class ProgressInfo
    {
        /// <summary>
        /// Gets the preview image.
        /// </summary>
        public Bitmap PreviewImage { get; }
        /// <summary>
        /// Gets the progress percent.
        /// </summary>
        public int ProgressPercent { get; }
        /// <summary>
        /// Gets the progress details.
        /// </summary>
        public string ProgressDetails { get; }
        /// <summary>
        /// Gets the progress speed.
        /// </summary>
        public double ProgressSpeed { get; }
        /// <summary>
        /// Gets the remaining seconds.
        /// </summary>
        public double RemainingSeconds { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressInfo" /> class.
        /// </summary>
        /// <param name="previewImage">The preview image.</param>
        /// <param name="progressPercent">The progress percent.</param>
        /// <param name="progressDetails">The progress details.</param>
        /// <param name="remainingSeconds">The remaining seconds.</param>
        public ProgressInfo(Bitmap previewImage, int progressPercent, string progressDetails, double remainingSeconds)
        {
            PreviewImage = previewImage;
            ProgressPercent = progressPercent;
            ProgressSpeed = ProgressSpeed;
            ProgressDetails = progressDetails;
            RemainingSeconds = remainingSeconds;
        }
    }
}
