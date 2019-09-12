using OpenCvSharp;
using System;
using System.Drawing;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The frame extraction arguments
    /// </summary>
    public class FrameExtractionArguments
    {
        /// <summary>
        /// Gets the frame identifier.
        /// </summary>
        public int FrameId { get; }
        /// <summary>
        /// The previous mat
        /// </summary>
        public MatOfByte3 PreviousMat { get; }
        /// <summary>
        /// The current mat
        /// </summary>
        public MatOfByte3 CurrentMat { get; }
        /// <summary>
        /// Gets the background.
        /// </summary>
        public MatOfByte3 Background { get; }
        /// <summary>
        /// Gets the on gizmo drawn.
        /// </summary>
        public Action<Bitmap> OnGizmoDrawn { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameExtractionArguments" /> class.
        /// </summary>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="previousMat">The previous mat.</param>
        /// <param name="currentMat">The current mat.</param>
        /// <param name="background">The background.</param>
        /// <param name="onGizmoDrawn">The on gizmo drawn.</param>
        public FrameExtractionArguments(int frameId, MatOfByte3 previousMat, MatOfByte3 currentMat, MatOfByte3 background, Action<Bitmap> onGizmoDrawn = null)
        {
            FrameId = frameId;
            PreviousMat = previousMat;
            CurrentMat = currentMat;
            Background = background;
            OnGizmoDrawn = onGizmoDrawn;
        }
    }
}
