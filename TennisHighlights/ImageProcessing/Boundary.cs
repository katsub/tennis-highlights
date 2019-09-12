namespace TennisHighlights
{
    /// <summary>
    /// The boundary
    /// </summary>
    public struct Boundary
    {
        public readonly double minX;
        public readonly double minY;
        public readonly double maxX;
        public readonly double maxY;

        /// <summary>
        /// Initializes a new instance of the <see cref="Boundary"/> struct.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        public Boundary(double minX, double maxX, double minY, double maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }
    }
}
