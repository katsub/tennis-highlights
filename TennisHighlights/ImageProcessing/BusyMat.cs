using OpenCvSharp;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// A mat with a busy flag
    /// </summary>
    public class BusyMat
    {
        /// <summary>
        /// Gets the mat.
        /// </summary>
        public MatOfByte3 Mat { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is busy.
        /// </summary>
        public bool IsBusy { get; private set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="OwnedMat"/> class.
        /// </summary>
        /// <param name="mat">The mat.</param>
        public BusyMat(MatOfByte3 mat) => Mat = mat;
        /// <summary>
        /// Marks this instance as busy so it wouldn't be used for other purposes.
        /// </summary>
        public void SetBusy() => IsBusy = true;
        /// <summary>
        /// Frees this instance for use.
        /// </summary>
        public void FreeForUse() => IsBusy = false;
    }
}
