using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Moves;

namespace TennisHighlights.Rallies
{
    /// <summary>
    /// The rally builder
    /// </summary>
    public class RallyBuilder
    {
        /// <summary>
        /// The minimum travel distance. 
        /// </summary>
        private static ResolutionDependentParameter _minTravelDistance = new ResolutionDependentParameter(15000d, 1d);
        /// <summary>
        /// The outside the camera tolerance
        /// </summary>
        private static ResolutionDependentParameter _outsideTheCameraTolerance = new ResolutionDependentParameter(20d, 1d);

        /// <summary>
        /// The minimum rally frames
        /// </summary>
        private const int _minRallyFrames = 30;

        /// <summary>
        /// The rally settings
        /// </summary>
        private readonly RallyBuildingSettings _rallySettings;
        /// <summary>
        /// The video information
        /// </summary>
        private readonly System.Drawing.Size _targetSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyBuilder" /> class.
        /// </summary>
        /// <param name="rallySettings">The rally settings.</param>
        /// <param name="targetSize">Size of the target.</param>
        public RallyBuilder(RallyBuildingSettings rallySettings, System.Drawing.Size targetSize)
        {
            _rallySettings = rallySettings;
            _targetSize = targetSize;
        }

        /// <summary>
        /// Builds the rallies.
        /// </summary>
        /// <param name="arcsPerFrame">The arcs per frame.</param>
        public List<Rally> BuildRallies(Dictionary<int, Dictionary<int, Arc>> arcsPerFrame)
        {
            var cameraTol = _outsideTheCameraTolerance.Value;
            var minTravelDistance = _minTravelDistance.Value;
            var noiseSquaredSpeed = ArcExtractor.NoiseSquaredSpeed.Value;

            var rallies = new List<Rally>();

            var endOfLastRallyAdded = 0;

            foreach (var thisframeArcs in arcsPerFrame)
            {
                foreach (var arc in thisframeArcs.Value)
                {
                    var firstBall = arc.Value.Balls.First();

                    if (firstBall.Key < endOfLastRallyAdded) { continue; }


                    //If ball comes from outside the camera, it's probably part of another rally. Skip it.
                    if (firstBall.Value.Position.X - cameraTol < 0d
                        || firstBall.Value.Position.Y - cameraTol < 0d
                        || firstBall.Value.Position.X + cameraTol > _targetSize.Width
                        || firstBall.Value.Position.Y + cameraTol > _targetSize.Height)
                    {
                        continue;
                    }

                    var rally = new Rally();
                    rally.Arcs.Add(arc.Value);

                    var newArcAdded = true;

                    while (newArcAdded)
                    {
                        newArcAdded = ExtendRally(rally, arcsPerFrame);
                    }

                    if (rally.Arcs.Count > 1 && rally.DurationInFrames > _minRallyFrames && rally.GetBallTotalTravelDistance() > minTravelDistance)
                    {
                        endOfLastRallyAdded = rally.Arcs.Last().Balls.Last().Key;

                        //If nobody plays for a long time, noise can form random slow rallies
                        if (rally.Arcs.Any(a => a.Stats.AverageSpeed > 2d * noiseSquaredSpeed))
                        {
                            rallies.Add(rally);
                        }

                        break;
                    }
                }
            }

            return rallies;
        }

        /// <summary>
        /// Extends the rally.
        /// </summary>
        /// <param name="rally">The rally.</param>
        /// <param name="arcsPerFrame">The arcs per frame.</param>
        private bool ExtendRally(Rally rally, Dictionary<int, Dictionary<int, Arc>> arcsPerFrame)
        {
            var lastArc = rally.Arcs.Last();

            var candidateConnection = GetNextArcInRally(lastArc, arcsPerFrame);

            if (candidateConnection != null)
            {
                for (int s = 0; s < candidateConnection.DiscardFollowingBalls; s++)
                {
                    candidateConnection.CandidateFollowingArc.Balls.RemoveAt(0);
                }

                for (int s = 0; s < candidateConnection.DiscardedPrecedingBalls; s++)
                {
                    lastArc.Balls.RemoveAt(lastArc.Balls.Count - 1);
                }

                rally.Arcs.Add(candidateConnection.CandidateFollowingArc);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the next arc in rally.
        /// </summary>
        /// <param name="lastArc">The last arc.</param>
        /// <param name="arcsPerFrame">The arcs per frame.</param>
        public CandidateArcConnection GetNextArcInRally(Arc lastArc, Dictionary<int, Dictionary<int, Arc>> arcsPerFrame)
        {
            //If too slow, limiter detected arcs at 10 per discarded ball config
            var lastArcRange = lastArc.Range;
            var lastArcBalls = lastArc.Balls;
            var possibleArcs = new List<CandidateArcConnection>();

            for (int j = 0; j < Math.Min(lastArcBalls.Count, 100); j++)
            {
                var discardedOriginalBalls = j;

                var index = lastArcBalls.Count - discardedOriginalBalls - 1;
                var rallyLastBall = lastArcBalls.Values[index];
                var rallyLastFrame = rallyLastBall.FrameIndex;

                var frameLimitForLongRange = rallyLastFrame + _rallySettings.MaxUndetectedFramesForLongRange;

                for (int i = rallyLastFrame - 100; i < rallyLastFrame + _rallySettings.MaxUndetectedFrames; i++)
                {
                    if (arcsPerFrame.TryGetValue(i, out var thisFrameArcs))
                    {
                        //TODO (perf): needs a way to filter quickly all combinations of first arc discarded balls + second arcs discarded balls per arc
                        //all combinations need to be tried, as discarding more balls from the first arc might allow the second ones to include much more balls
                        //than were discarded
                        foreach (var arc in thisFrameArcs.Where(a => a.Value.Id != lastArc.Id))
                        {
                            if (lastArc.GetCombinedRange(arc.Value) > lastArcRange
                                && TestArcConnection(lastArc, arc.Value, discardedOriginalBalls, out var discardedBalls))
                            {
                                possibleArcs.Add(new CandidateArcConnection(lastArc, arc.Value, discardedOriginalBalls, discardedBalls));
                            }
                        }
                    }
                }
            }

            var candidatesAdded = true;

            while (candidatesAdded)
            {
                candidatesAdded = CombineCandidateArcConnections(possibleArcs);
            }

            if (possibleArcs.Any())
            {
                //TODO: probably faster if we manually check the sort and return manually the best arc
                //Sort by number of undetected frames between the two arcs
                //returns null if no arc adds new balls to the rally
                return possibleArcs.Where(a => a.NewBallsInRally > 0).OrderByDescending(o => o.NewBallsInRally)
                                                                     .ThenBy(o => o.NumberOfUndetectedFramesBetweenOriginalAndCandidate)
                                                                     .FirstOrDefault();

            }

            return null;
        }

        /// <summary>
        /// Combines the candidate arc connections.
        /// </summary>
        /// <param name="candidateArcConnections">The candidate arc connections.</param>
        private bool CombineCandidateArcConnections(List<CandidateArcConnection> candidateArcConnections)
        {
            //TODO: could be faster, contains() would be really useful, but test first to see if it's slow
            var candidatesToRemove = new List<CandidateArcConnection>();
            var candidatesToAdd = new List<CandidateArcConnection>();

            foreach (var candidate1 in candidateArcConnections)
            {
                var firstArc = candidate1.CandidateFollowingArc;
                var firstArcRange = firstArc.Range;

                foreach (var candidate2 in candidateArcConnections)
                {
                    var secondArc = candidate2.CandidateFollowingArc;

                    if (firstArc.GetCombinedRange(secondArc) > firstArcRange)
                    {
                        for (int i = 0; i < candidate1.CandidateFollowingArc.Balls.Count - 1; i++)
                        {
                            if (TestArcConnection(firstArc, secondArc, i, out var candidate2DiscardedBalls))
                            {
                                var combinedArc = new Arc();

                                foreach (var ball in firstArc.Balls.Take(firstArc.Balls.Count - i))
                                {
                                    combinedArc.Balls.Add(ball.Key, ball.Value);
                                }

                                foreach (var ball in secondArc.Balls.Skip(candidate2DiscardedBalls))
                                {
                                    combinedArc.Balls.Add(ball.Key, ball.Value);
                                }

                                if (!candidateArcConnections.Any(a => a.CandidateFollowingArc.IsSimilarArc(combinedArc))
                                    && !candidatesToAdd.Any(a => a.CandidateFollowingArc.IsSimilarArc(combinedArc)))
                                {
                                    combinedArc.BuildStats();

                                    candidatesToAdd.Add(new CandidateArcConnection(candidate1.PrecedingArc, combinedArc, candidate1.DiscardedPrecedingBalls, 0));

                                    candidatesToRemove.Add(candidate1);
                                    candidatesToRemove.Add(candidate2);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            foreach (var candidate in candidatesToRemove)
            {
                candidateArcConnections.Remove(candidate);
            }

            foreach (var candidate in candidatesToAdd)
            {
                candidateArcConnections.Add(candidate);
            }

            return candidatesToAdd.Count > 0;
        }

        /// <summary>
        /// Determines whether [is ball close to player] [the specified player].
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="ball">The ball.</param>
        private bool IsBallCloseToPlayer(Boundary player, Accord.Point ball, double distanceTolerance)
        {
            return ball.X < player.maxX + 20d && ball.X > player.minX - 20d && ball.Y < player.maxY + 20d && ball.Y > player.minY - 20d;
        }

        /// <summary>
        /// Returns true if arcs can be connected using the following settings, and the amount of balls that need to be discarded from the second arc for the connection to make sense.
        /// </summary>
        /// <param name="firstArc">The first arc.</param>
        /// <param name="secondArc">The second arc.</param>
        /// <param name="firstArcDiscardBalls">The first arc discard balls.</param>
        /// <param name="secondArcDiscardedBalls">The second arc discarded balls.</param>
        public bool TestArcConnection(Arc firstArc, Arc secondArc, int firstArcDiscardBalls, out int secondArcDiscardedBalls)
        {
            var maxSquaredDistance = _rallySettings.MaxSquaredDistance.Value;
            var maxLongRangeSquareDistance = _rallySettings.MaxLongRangeSquaredDistance.Value;

            secondArcDiscardedBalls = 0;

            var lastArcBalls = firstArc.Balls;
            var index = lastArcBalls.Count - firstArcDiscardBalls - 1;
            var rallyLastBall = lastArcBalls.Values[index];
            var rallyLastBallPosition = rallyLastBall.Position;
            var rallyLastFrame = rallyLastBall.FrameIndex;

            var frameLimitForLongRange = rallyLastFrame + _rallySettings.MaxUndetectedFramesForLongRange;

            foreach (var ball in secondArc.Balls)
            {
                //If we discard more balls than the amount of balls being added, there's no point in doing the connection
                if (firstArcDiscardBalls + secondArcDiscardedBalls >= Math.Min(firstArc.Balls.Count, secondArc.Balls.Count))
                {
                    return false;
                }

                var arcsConnectionDistance = (ball.Value.Position - rallyLastBallPosition).SquaredLength();

                if (ball.Key > rallyLastFrame)
                {
                    if (arcsConnectionDistance < maxSquaredDistance
                        || (arcsConnectionDistance < maxLongRangeSquareDistance
                            && ball.Key < frameLimitForLongRange))
                    {
                        return true;
                    }
                }

                secondArcDiscardedBalls++;
            }

            return false;
        }
    }
}
