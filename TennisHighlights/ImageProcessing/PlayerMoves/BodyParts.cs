namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The body parts
    /// </summary>
    public class BodyParts
    {
        /// <summary>
        /// Gets the left shoulder.
        /// </summary>
        public Accord.Point LeftShoulder { get; }
        /// <summary>
        /// Gets the right shoulder.
        /// </summary>
        public Accord.Point RightShoulder { get; }
        /// <summary>
        /// Gets the left wrist.
        /// </summary>
        public Accord.Point LeftWrist { get; }
        /// <summary>
        /// Gets the right wrist.
        /// </summary>
        public Accord.Point RightWrist { get; }
        /// <summary>
        /// Gets the left knee.
        /// </summary>
        public Accord.Point LeftKnee { get; }
        /// <summary>
        /// Gets the right knee.
        /// </summary>
        public Accord.Point RightKnee { get; }
        /// <summary>
        /// Gets the torso.
        /// </summary>
        public Accord.Point Torso { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="BodyParts"/> class.
        /// </summary>
        /// <param name="leftWrist">The left wrist.</param>
        /// <param name="rightWrist">The right wrist.</param>
        /// <param name="leftKnee">The left knee.</param>
        /// <param name="rightKnee">The right knee.</param>
        /// <param name="leftShoulder">The left shoulder.</param>
        /// <param name="rightShoulder">The right shoulder.</param>
        /// <param name="torso">The torso.</param>
        public BodyParts(Accord.Point leftWrist, Accord.Point rightWrist, Accord.Point leftKnee, Accord.Point rightKnee, Accord.Point leftShoulder, Accord.Point rightShoulder, Accord.Point torso)
        {
            LeftWrist = leftWrist;
            RightWrist = rightWrist;
            LeftKnee = leftKnee;
            RightKnee = rightKnee;
            LeftShoulder = leftShoulder;
            RightShoulder = rightShoulder;
            Torso = torso;
        }
    }
}
