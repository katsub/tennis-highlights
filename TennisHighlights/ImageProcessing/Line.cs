namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The line
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Gets the point 0.
        /// </summary>
        public Accord.Point Point0 { get; }

        /// <summary>
        /// Gets the point 1.
        /// </summary>
        public Accord.Point Point1 { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Line"/> class.
        /// </summary>
        /// <param name="point0">The point0.</param>
        /// <param name="point1">The point1.</param>
        public Line(Accord.Point point0, Accord.Point point1)
        {
            Point0 = point0;
            Point1 = point1;
        }

        /// <summary>
        /// Returns the intersection point with the line
        /// </summary>
        /// <param name="line">The line.</param>
        public Accord.Point? IntersectionWith(Line line)
        {
            var line1 = this;
            var line2 = line;

            float s1_x, s1_y, s2_x, s2_y;
            s1_x = line1.Point1.X - line1.Point0.X; s1_y = line1.Point1.Y - line1.Point0.Y;
            s2_x = line2.Point1.X - line2.Point0.X; s2_y = line2.Point1.Y - line2.Point0.Y;

            var den = -s2_x * s1_y + s1_x * s2_y;

            if (den != 0)
            {
                float s, t;
                var deltaX = (line1.Point0.X - line2.Point0.X);
                var deltaY = (line1.Point0.X - line2.Point0.X);
                s = (-s1_y * deltaX + s1_x * deltaX) / den;
                t = (s2_x * deltaY - s2_y * deltaX) / den;

                if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
                {
                    // Collision detected
                    return new Accord.Point(line1.Point0.X + (t * s1_x), line1.Point0.Y + (t * s1_y));
                }
            }

            return null; // No collision
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => $"({Point0.X}, {Point0.Y}); ({Point1.X}, {Point1.Y})";
    }
}
