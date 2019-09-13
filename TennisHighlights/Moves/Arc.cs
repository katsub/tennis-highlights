using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights.ImageProcessing;

namespace TennisHighlights.Moves
{
    /// <summary>
    /// The arc
    /// </summary>
    public class Arc
    {
        /// <summary>
        /// The similar points distance
        /// </summary>
        private static readonly ResolutionDependentParameter _similarPointsDistance = new ResolutionDependentParameter(10d, 2d);
        /// <summary>
        /// The current identifier
        /// </summary>
        private static int _currentId;

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        private static int GetId()
        {
            _currentId++;

            return _currentId;
        }

        /// <summary>
        /// Gets the identifier. 
        /// </summary>
        public int Id { get; } = GetId();

        /// <summary>
        /// Gets the balls.
        /// </summary>
        public SortedList<int, ArcBallData> Balls { get; } = new SortedList<int, ArcBallData>();

        /// <summary>
        /// Gets the range.
        /// </summary>
        public int Range => Balls.Last().Key - Balls.First().Key;

        /// <summary>
        /// Gets or sets the stats.
        /// </summary>
        public ArcStats Stats { get; private set; }

        /// <summary>
        /// Builds the arc stats.
        /// </summary>
        public void BuildStats()
        {
            var averageSpeed = 0d;
            var angles = 0d;

            //We filter noises by getting the smallest speed amongst the neighbor balls. For a line or a parable, this won't change the speed value by a lot,
            //but the noise should totally disappear. Here it uses 2 neighbor balls, but 5 could be used if somehow there's still noise passing through it
            //Balls is indexed by frame key, but Balls.Values is indexed by position in the list
            var ballsIndexed = Balls.Values;
            for (int i = 1; i < Balls.Count - 1; i++)
            {
                averageSpeed += Math.Min(Math.Min(ballsIndexed[i - 1].SpeedSquaredMagnitude, ballsIndexed[i].SpeedSquaredMagnitude), ballsIndexed[i + 1].SpeedSquaredMagnitude);
            }

            for (int i = 0; i < Balls.Count; i++)
            {
                angles += ballsIndexed[i].Angles;
            }

            averageSpeed /= Balls.Count - 2;
            angles /= Balls.Count;

            Stats = new ArcStats(averageSpeed, angles);
        }

        /// <summary>
        /// Gets the combined range: the resulting range of succeeding this arc with other arc
        /// </summary>
        /// <param name="arc1">The arc1.</param>
        /// <param name="arc2">The arc2.</param>
        public int GetCombinedRange(Arc otherArc) => otherArc.Balls.Last().Key - Balls.First().Key;

        /// <summary>
        /// Determines whether [is similar arc] [the specified other arc].
        /// </summary>
        /// <param name="otherArc">The other arc.</param>
        public bool IsSimilarArc(Arc otherArc)
        {
            if (Balls.Count != otherArc.Balls.Count) { return false; }

            if (Range != otherArc.Range) { return false; }

            if ((Balls.First().Value.Position - otherArc.Balls.First().Value.Position).SquaredLength() > _similarPointsDistance.Value
                || (Balls.Last().Value.Position - otherArc.Balls.Last().Value.Position).SquaredLength() > _similarPointsDistance.Value)
            { return false; }

            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"{Stats} ||| Balls: {Balls.Count} |||| First: {Balls.First().Value.GetFramePositionString()} |||| Last: {Balls.Last().Value.GetFramePositionString()}";
    }
}
