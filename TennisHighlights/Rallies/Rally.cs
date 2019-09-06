using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Moves;

namespace TennisHighlights.Rallies
{
    public class Rally
    {
        /// <summary>
        /// Gets the arcs.
        /// </summary>
        public List<Arc> Arcs { get; } = new List<Arc>();

        /// <summary>
        /// Gets the duration in frames.
        /// </summary>
        public int DurationInFrames => LastBall.FrameIndex - FirstBall.FrameIndex;
        /// <summary>
        /// Gets the detected frames percentage.
        /// </summary>
        public double DetectedFramesPercentage => 100d * Arcs.Sum(a => a.Balls.Count) / (double)DurationInFrames;
        /// <summary>
        /// Gets the boundaries.
        /// </summary>
        public Boundary Boundaries => new Boundary(Arcs.Min(a => a.Balls.Min(b => b.Value.Position.X)),
                                                   Arcs.Max(a => a.Balls.Max(b => b.Value.Position.X)),
                                                   Arcs.Min(a => a.Balls.Min(b => b.Value.Position.Y)),
                                                   Arcs.Max(a => a.Balls.Max(b => b.Value.Position.Y)));

        /// <summary>
        /// Gets the active area.
        /// </summary>
        public double ActiveArea
        {
            get
            {
                var boundaries = Boundaries;

                return (boundaries.maxX - boundaries.minX) * (boundaries.maxY - boundaries.minY);
            }
        }

        /// <summary>
        /// Gets the active area roundness.
        /// </summary>
        public double ActiveAreaRoundness
        {
            get
            {
                var boundaries = Boundaries;

                return (boundaries.maxX - boundaries.minX) / (boundaries.maxY - boundaries.minY);
            }
        }

        /// <summary>
        /// Gets the last ball.
        /// </summary>
        public ArcBallData LastBall => Arcs.Last().Balls.Last().Value;

        /// <summary>
        /// Gets the first ball.
        /// </summary>
        public ArcBallData FirstBall => Arcs.First().Balls.First().Value;

        /// <summary>
        /// Gets the ball total travel distance.
        /// </summary>
        public double GetBallTotalTravelDistance()
        {
            var distance = 0d;
            var currentBall = Arcs.First().Balls.First().Value.Position;

            foreach (var arc in Arcs)
            {
                foreach (var ball in arc.Balls.Values)
                {
                    //Using length squared instead of length is faster but it favors longer arcs, this shouldn't be critical, but will need testing
                    distance += (ball.Position - currentBall).SquaredLength();

                    currentBall = ball.Position;
                }
            }

            return distance;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"TravelDistance: {Math.Round(GetBallTotalTravelDistance(),1)} ||| Arcs: {Arcs.Count} |||| First: {Arcs.First().Balls.First().Value.GetFramePositionString()} |||| Last: {Arcs.Last().Balls.Last().Value.GetFramePositionString()}";
    }
}
