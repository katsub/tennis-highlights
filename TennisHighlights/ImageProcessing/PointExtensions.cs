using Accord;
using Accord.Math.Geometry;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The point extensions
    /// </summary>
    public static class PointExtensions
    {
        /// <summary>
        /// The origin
        /// </summary>
        private static Point _origin = new Point(0f, 0f);

        /// <summary>
        /// Calculates the squared length of this point from the origin.
        /// </summary>
        /// <param name="p">The point.</param>
        public static double SquaredLength(this Point p) => p.SquaredDistanceTo(_origin);

        /// <summary>
        /// Calculates the angle (in degrees) between the vector formed by origin and this point and the vector formed by
        /// origin and the other point
        /// </summary>
        /// <param name="p">This point.</param>
        /// <param name="other">The other point.</param>
        public static double AngleBetween(this Point p, Point other) => GeometryTools.GetAngleBetweenVectors(_origin, p, other);

        /// <summary>
        /// Multiplies this point by the given constant.
        /// </summary>
        /// <param name="p">This point.</param>
        /// <param name="c">The constant.</param>
        public static Point Multiply(this Point p, double c) => Point.Multiply(p, (float)c);
    }
}
