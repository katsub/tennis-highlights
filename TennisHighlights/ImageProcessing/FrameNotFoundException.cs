using System;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The frame not found exception
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class FrameNotFoundException : Exception
    {
        /// <summary>
        /// Gets the index of the frame.
        /// </summary>
        public int FrameIndex { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameNotFoundException"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        public FrameNotFoundException(int index) : base (GetMessage(index)) => FrameIndex = index;

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <param name="index">The index.</param>
        public static string GetMessage(int index) => "Frame " + index + " was requested and not found, and extractor has already finished extracting";

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => GetMessage(FrameIndex) + "\n" + StackTrace.ToString();
    }
}
