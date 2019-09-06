using System;

namespace TennisHighlights.Moves
{
    /// <summary>
    /// The arc stats
    /// </summary>
    public class ArcStats
    {
        /// <summary>
        /// Gets the average speed.
        /// </summary>
        public double AverageSpeed { get; }
        /// <summary>
        /// Gets the average angles.
        /// </summary>
        public double AverageAngles { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArcStats"/> class.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <param name="angles">The angles.</param>
        public ArcStats(double speed, double angles)
        {
            AverageSpeed = speed;
            AverageAngles = angles;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Av Speed: {Math.Round(AverageSpeed,1)}, Av Angles: {Math.Round(AverageAngles,1)}";
    }
}
