using TennisHighlights.Moves;

namespace TennisHighlights.Rallies
{
    /// <summary>
    /// The candidate arc, a class describing a connection between two arcs that could make sense
    /// </summary>
    public class CandidateArcConnection
    {
        /// <summary>
        /// Gets the preceding arc.
        /// </summary>
        public Arc PrecedingArc { get; }
        /// <summary>
        /// Gets the candidate following arc.
        /// </summary>
        public Arc CandidateFollowingArc { get; }
        /// <summary>
        /// Gets the discarded preceding balls (balls that need to be discarded from the end of the preceding arc for the connection to work).
        /// </summary>
        public int DiscardedPrecedingBalls { get; }
        /// <summary>
        /// Gets the discard following balls. (balls that need to be discarded from the beginning of the following arc for the connection to work).
        /// </summary>
        public int DiscardFollowingBalls { get; }

        /// <summary>
        /// Gets the number of undetected frames between original and candidate.
        /// </summary>
        public int NumberOfUndetectedFramesBetweenOriginalAndCandidate => CandidateFollowingArc.Balls.Values[DiscardFollowingBalls].FrameIndex
                                                                          - PrecedingArc.Balls.Values[PrecedingArc.Balls.Count - 1 - DiscardedPrecedingBalls].FrameIndex;
        /// <summary>
        /// Gets the new balls in rally.
        /// </summary>
        public int NewBallsInRally => CandidateFollowingArc.Balls.Count - DiscardFollowingBalls - DiscardedPrecedingBalls;

        /// <summary>
        /// Initializes a new instance of the <see cref="CandidateArcConnection"/> class.
        /// </summary>
        /// <param name="preceding">The preceding.</param>
        /// <param name="following">The following.</param>
        /// <param name="precedingBalls">The preceding balls.</param>
        /// <param name="followingBalls">The following balls.</param>
        public CandidateArcConnection(Arc preceding, Arc following, int precedingBalls, int followingBalls)
        {
            PrecedingArc = preceding;
            CandidateFollowingArc = following;
            DiscardedPrecedingBalls = precedingBalls;
            DiscardFollowingBalls = followingBalls;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"UndetectedFrames = {NumberOfUndetectedFramesBetweenOriginalAndCandidate}, NewBallsInRally = {NewBallsInRally}, CandidateArcId: {CandidateFollowingArc.Id}, CandidateDiscardedBalls: {DiscardFollowingBalls}, OriginalDiscardedBalls: {DiscardedPrecedingBalls}";
    }
}
