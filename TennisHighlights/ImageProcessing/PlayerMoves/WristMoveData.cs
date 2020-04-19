namespace TennisHighlights.ImageProcessing.PlayerMoves
{
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
        /// Initializes a new instance of the <see cref="WristSpeedData"/> class.
        /// </summary>
        /// <param name="currentWrist">The current wrist.</param>
        /// <param name="previousWrist">The previous wrist.</param>
        /// <param name="usingOlderPreviousFrame">if set to <c>true</c> [using older previous frame].</param>
        public WristSpeedData(Accord.Point currentWrist, Accord.Point previousWrist, bool usingOlderPreviousFrame)
        {
            Speed = currentWrist - previousWrist;

            if (usingOlderPreviousFrame)
            {
                Speed /= 2;
            }

            SquaredAbs = (float)Speed.SquaredLength();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => "Speed: (" + (int)Speed.X + ", " + Speed.Y + ") , Abs: " + (int)SquaredAbs;
    }
}
