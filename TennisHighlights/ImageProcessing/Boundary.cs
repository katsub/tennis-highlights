namespace TennisHighlights
{
    public struct Boundary
    {
        public readonly double minX;
        public readonly double minY;
        public readonly double maxX;
        public readonly double maxY;

        public Boundary(double minX, double maxX, double minY, double maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }
    }
}
