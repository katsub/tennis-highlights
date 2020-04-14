namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The wrist
    /// </summary>
    public enum Wrist
    {
        Left,
        Right
    }

    /// <summary>
    /// The wrist speed data
    /// </summary>
    public class WristSpeedData
    {
        /// <summary>
        /// Gets the speed.
        /// </summary>
        public Accord.Point Speed { get; }
        /// <summary>
        /// Gets the squared abs.
        /// </summary>
        public float SquaredAbs { get; }
        /// <summary>
        /// Gets the wrist.
        /// </summary>
        public Wrist Wrist { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WristSpeedData"/> class.
        /// </summary>
        /// <param name="curretLeftWrist">The current left wrist.</param>
        /// <param name="currentRightWrist">The current right wrist.</param>
        /// <param name="previousLeftWrist">The previous left wrist.</param>
        /// <param name="previousRightWrist">The previous right wrist.</param>
        /// <param name="usingOlderPreviousFrame">set to true if using the previous previous frame instead of the previous frame.</param>
        public WristSpeedData(Accord.Point currentLeftWrist, Accord.Point currentRightWrist, 
                             Accord.Point previousLeftWrist, Accord.Point previousRightWrist, bool usingOlderPreviousFrame)

        {
            var leftIsFaster = currentLeftWrist.SquaredDistanceTo(previousLeftWrist) >= currentRightWrist.SquaredDistanceTo(previousRightWrist);

            var currentWristToUse = leftIsFaster ? currentLeftWrist : currentRightWrist;
            var previousWristToUse = leftIsFaster ? previousLeftWrist : previousRightWrist;

            Wrist = leftIsFaster ? Wrist.Left : Wrist.Right;
            Speed = currentWristToUse - previousWristToUse;

            if (usingOlderPreviousFrame)
            {
                Speed /= 2;
            }

            SquaredAbs = (float)Speed.SquaredLength();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => "Speed: (" + (int)Speed.X + ", " + Speed.Y + ") , Abs: " + (int)SquaredAbs + ", " + Wrist;
    }
}
