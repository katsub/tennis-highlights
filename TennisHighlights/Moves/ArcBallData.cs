using Accord;
using System;

namespace TennisHighlights.Moves
{
    /// <summary>
    /// The arc ball data
    /// </summary>
    public class ArcBallData
    {
        /// <summary>
        /// The frame index
        /// </summary>
        public int FrameIndex { get; }
        /// <summary>
        /// The ball index
        /// </summary>
        public int BallIndex { get; }
        /// <summary>
        /// The position
        /// </summary>
        public Point Position { get; }
        /// <summary>
        /// The speed direction
        /// </summary>
        public Point SpeedDirection { get; }
        /// <summary>
        /// Gets the speed magnitude.
        /// </summary>
        public double SpeedSquaredMagnitude { get; }
        /// <summary>
        /// Gets the correlation.
        /// </summary>
        public double Correlation { get; }
        /// <summary>
        /// Gets the angles.
        /// </summary>
        public double Angles { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArcBallData" /> class.
        /// </summary>
        /// <param name="frameIndex">Index of the frame.</param>
        /// <param name="ballIndex">Index of the ball.</param>
        /// <param name="position">The position.</param>
        /// <param name="speedDirection">The speed direction.</param>
        /// <param name="speedSquaredMagnitude">The speed squared magnitude.</param>
        /// <param name="correlation">The correlation.</param>
        /// <param name="angles">The angles.</param>
        public ArcBallData(int frameIndex, int ballIndex, Point position, Point speedDirection, double speedSquaredMagnitude, double correlation, double angles)
        {
            FrameIndex = frameIndex;
            BallIndex = ballIndex;
            Position = position;
            SpeedDirection = speedDirection;
            SpeedSquaredMagnitude = speedSquaredMagnitude;
            Correlation = correlation;
            Angles = Math.Abs(angles);
        }

        /// <summary>
        /// To the short string.
        /// </summary>
        public string GetFramePositionString() => $"Frame: {FrameIndex}, Pos: ({Math.Round(Position.X, 1)},{Math.Round(Position.Y, 1)})";

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Frame: {FrameIndex}, Pos: ({Math.Round(Position.X,1)},{Math.Round(Position.Y,1)}), Speed: {Math.Round(SpeedSquaredMagnitude,1)}, Cor: {Math.Round(Correlation,1)}, Angles: {Math.Round(Angles,1)}";
    }
}
