using Accord;
using Accord.Math.Geometry;
using System;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The point extensions
    /// </summary>
    public static class PointExtensions
    {
        /// <summary>
        /// The radians to degrees
        /// </summary>
        private const double _radiansToDegrees = (180d / Math.PI);
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
        public static double AngleBetween(this Point p, Point other)
        {
            var theta1 = Math.Atan2(_origin.Y - p.Y, _origin.X - p.X);
            var theta2 = Math.Atan2(_origin.Y - other.Y, _origin.X - other.X);

            var diff = Math.Abs(theta1 - theta2);

            return _radiansToDegrees * Math.Min(diff, Math.Abs(180 - diff));
        }

        /// <summary>
        /// Multiplies this point by the given constant.
        /// </summary>
        /// <param name="p">This point.</param>
        /// <param name="c">The constant.</param>
        public static Point Multiply(this Point p, double c) => Point.Multiply(p, (float)c);
    }
}
