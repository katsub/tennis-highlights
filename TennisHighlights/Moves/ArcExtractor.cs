using Accord;
using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights.ImageProcessing;

namespace TennisHighlights.Moves
{
    /// <summary>
    /// The arc extractor
    /// </summary>
    public static class ArcExtractor
    {
        /// <summary>
        /// Below this speed, values are considered to be noisy and angles unprecise, so the algorithm should be less strict when picking points
        /// </summary>
        public static ResolutionDependentParameter NoiseSquaredSpeed { get; } = new ResolutionDependentParameter(5d, 2d);
        /// <summary>
        /// The maximum acceptable angle error for the next ball, in regards to the angle between the current ball and the previous ball
        /// </summary>
        private const float _nextBallAngleError = 15f;
        /// <summary>
        /// The ball interframe maximum square distance (max squared speed)
        /// </summary>
        private static readonly ResolutionDependentParameter _ballInterframeMaxSquareDistance = new ResolutionDependentParameter(900d, 2d);
        /// <summary>
        /// The minimum correlation for arc
        /// </summary>
        private static double _minimumCorrelationForArc => -(180d + 2d * _ballInterframeMaxSquareDistance.Value);
        /// <summary>
        /// The maximum arc interruption frames
        /// </summary>
        private const int _maxArcInterruptionFrames = 10;
        /// <summary>
        /// The maximum speed relative delta. Must be big because we're dealing with squared speed, so 10 is only about 3.3x times faster (which may
        /// not be a lot when the ball starts falling faster due to gravity, needs testing)
        /// </summary>
        private static readonly ResolutionDependentParameter _maxSquaredSpeedRelativeDelta = new ResolutionDependentParameter(10d, 2d);
        /// <summary>
        /// The empty vector
        /// </summary>
        private static Point _emptyVector = new Point(0, 0);
        /// <summary>
        /// The maximum squared speed mag
        /// </summary>
        private static readonly ResolutionDependentParameter _maxSquaredSpeedMag = new ResolutionDependentParameter(30d, 2d);
        /// <summary>
        /// The maximum projection delta squared magnitude
        /// </summary>
        private static readonly ResolutionDependentParameter _maxProjectionDeltaSquaredMagnitude = new ResolutionDependentParameter(1500d, 2d);

        /// <summary>
        /// Gets the longest arc for frame. Mostly used for debugging purposes to verify that the algorithm is able to build an arc using a ball from
        /// a frame that we suspect to have an arc that should be included in a rally
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="frameKey">The frame key.</param>
        public static Arc GetLongestArcForFrame(Dictionary<int, List<Point>> ballsPerFrame, int frameKey)
        {
            Arc longestArc = null;

            var totalBalls = ballsPerFrame[frameKey].Count;

            for (int i = 0; i < totalBalls; i++)
            {
                var arc = GetArc(ballsPerFrame, frameKey, i);

                if (longestArc == null || (arc != null && longestArc.Balls.Count < arc.Balls.Count))
                {
                    longestArc = arc;
                }
            }

            return longestArc;
        }

        /// <summary>
        /// Gets the arc.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="frameKey">The frame key.</param>
        /// <param name="ballIndex">Index of the ball.</param>
        public static Arc GetArc(Dictionary<int, List<Point>> ballsPerFrame, int frameKey, int ballIndex)
        {
            var (correlation, maxDirection) = GetMaxCorrelation(ballsPerFrame, frameKey, ballIndex);

            if (correlation > _minimumCorrelationForArc)
            {
                var arc = new Arc();

                arc.Balls.Add(frameKey, new ArcBallData(frameKey, ballIndex, ballsPerFrame[frameKey][ballIndex], maxDirection, 
                                                        maxDirection.SquaredLength(), correlation, 0d));

                PropagateArc(arc, ballsPerFrame);

                //Arcs should be either straight lines or parabolas. Ideally the arc points should be fit to a line or parabola and if the error is small enough,
                //then the arc is recognized. But this should be slow, so we use the simplified version below
                //3 is the minimum amount needed to filter distances as seem below
                if (arc.Balls.Count > 3)
                {
                    arc.BuildStats();

                    if (arc.Stats.AverageSpeed > NoiseSquaredSpeed.Value / 2d)
                    {
                        return arc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Propagates the arc.
        /// </summary>
        /// <param name="arc">The arc.</param>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        private static void PropagateArc(Arc arc, Dictionary<int, List<Point>> ballsPerFrame)
        {
            PropagateBall(false, arc, ballsPerFrame);
            PropagateBall(true, arc, ballsPerFrame);
        }

        /// <summary>
        /// Propagates the ball.
        /// </summary>
        /// <param name="propagateForward">if set to <c>true</c> [propagate forward].</param>
        /// <param name="arc">The arc.</param>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        private static void PropagateBall(bool propagateForward, Arc arc, Dictionary<int, List<Point>> ballsPerFrame)
        {
            var foundNewBall = true;
            var currentStartBall = propagateForward ? arc.Balls.Last().Value : arc.Balls.First().Value;

            while (foundNewBall)
            {
                foundNewBall = SeekNewBall(currentStartBall, ballsPerFrame, arc.Balls.Count, out var newArcBallData, propagateForward);

                if (foundNewBall)
                {
                    currentStartBall = newArcBallData;

                    arc.Balls.Add(newArcBallData.FrameIndex, newArcBallData);
                }
            }
        }

        /// <summary>
        /// Seeks the new ball.
        /// </summary>
        /// <param name="arcStartBall">The arc start ball.</param>
        /// <param name="">The .</param>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="newArcBallData">The new arc ball data.</param>
        /// <param name="seekForward">if set to <c>true</c> [seek forward].</param>
        private static bool SeekNewBall(ArcBallData arcStartBall, Dictionary<int, List<Point>> ballsPerFrame, int currentArcSize,
                                        out ArcBallData newArcBallData, bool seekForward = true)
        {
            var ballInterframeMaxSquareDistance = _ballInterframeMaxSquareDistance.Value;
            var noiseSpeed = NoiseSquaredSpeed.Value;
            var maxSquaredSpeedRelativeDelta = _maxSquaredSpeedRelativeDelta.Value;
            var inversedMaxSquaredSpeedRelativeDelta = 1d / _maxSquaredSpeedRelativeDelta.Value;
            var maxSquaredSpeedMag = _maxSquaredSpeedMag.Value;
            var maxProjectionDeltaSquaredMagnitude = _maxProjectionDeltaSquaredMagnitude.Value;

            newArcBallData = null;
            var currentNextFrame = arcStartBall.FrameIndex + (seekForward ? 1 : -1);

            while ((seekForward && currentNextFrame - arcStartBall.FrameIndex < _maxArcInterruptionFrames)
                   || (!seekForward && arcStartBall.FrameIndex - currentNextFrame < _maxArcInterruptionFrames))
            {
                if (ballsPerFrame.TryGetValue(currentNextFrame, out var currentNextBalls))
                {
                    var nextBalls = new List<ArcBallData>();

                    var i = 0;
                    var distanceBetweenFrames = arcStartBall.FrameIndex - currentNextFrame;

                    foreach (var ball in currentNextBalls)
                    {
                        var backVector = seekForward ? (ball - arcStartBall.Position) : (arcStartBall.Position - ball);

                        var speedSquaredMag = backVector.SquaredLength() / Math.Pow(distanceBetweenFrames, 2);

                        //Balls can't be too fast, and if they're above "noise speed" (when the ball is too slow that detection is degraded and so the speed and 
                        //and angles were not very reliable), then we put a tolerance in speed variation, since the ball shouldn't slow down or accelerate too much
                        //during an arc (in any case, not like 50x-100x faster than the previous frame, as can happen because of noise)
                        if (speedSquaredMag < ballInterframeMaxSquareDistance && ((speedSquaredMag < noiseSpeed && arcStartBall.SpeedSquaredMagnitude < noiseSpeed)
                                                                                  || (speedSquaredMag < maxSquaredSpeedRelativeDelta * arcStartBall.SpeedSquaredMagnitude
                                                                                      && speedSquaredMag > inversedMaxSquaredSpeedRelativeDelta * arcStartBall.SpeedSquaredMagnitude)))
                        {
                            var correlation = GetVectorCorrelation(backVector, arcStartBall.SpeedDirection, out var vecAngles);

                            var projectedPosition = arcStartBall.Position + arcStartBall.SpeedDirection * distanceBetweenFrames;

                            if (vecAngles < _nextBallAngleError || speedSquaredMag < noiseSpeed
                                || (vecAngles < 2 * _nextBallAngleError && speedSquaredMag < 2d * noiseSpeed)
                                || (currentArcSize > 5 && speedSquaredMag < _maxSquaredSpeedMag.Value && (projectedPosition - ball).SquaredLength() < maxProjectionDeltaSquaredMagnitude))
                            {
                                nextBalls.Add(new ArcBallData(currentNextFrame, i, ball, backVector, speedSquaredMag, correlation, vecAngles));
                            }
                        }

                        i++;
                    }

                    //Pick the ball most likely to be the successor of the current arc last (or first) ball
                    newArcBallData = nextBalls.OrderBy(b => b.Correlation).FirstOrDefault();

                    if (newArcBallData != null) { break; }
                }

                if (seekForward)
                {
                    currentNextFrame++;
                }
                else
                {
                    currentNextFrame--;
                }
            }

            return newArcBallData != null;
        }

        /// <summary>
        /// Gets the maximum correlation.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="frameKey">The frame key.</param>
        /// <param name="ballIndex">Index of the ball.</param>
        public static (double correlation, Point maxDirection) GetMaxCorrelation(Dictionary<int, List<Point>> ballsPerFrame, int frameKey, int ballIndex)
        {
            var ball = ballsPerFrame[frameKey][ballIndex];

            var maxCorrelation = double.NegativeInfinity;
            var maxDirection = _emptyVector;

            //For the analysed ball, search for one ball before it and one ball after it that maximize its correlation value so we can determine if this
            //ball is likely to be real and wield a real arc
            for (int i = 1; i < _maxArcInterruptionFrames; i++)
            {
                if (ballsPerFrame.TryGetValue(frameKey - i, out var possibleBackBalls))
                {
                    for (int j = 1; j < _maxArcInterruptionFrames; j++)
                    {
                        if (ballsPerFrame.TryGetValue(frameKey + j, out var possibleForwardBalls))
                        {
                            if (possibleBackBalls != null && possibleForwardBalls != null)
                            {
                                foreach (var backBall in possibleBackBalls)
                                {
                                    var backVector = ball - backBall;
                                    var backMag = backVector.SquaredLength();

                                    foreach (var forwardBall in possibleForwardBalls)
                                    {
                                        var forwardVector = forwardBall - ball;
                                        var correlation = GetVectorCorrelation(forwardVector, backVector, out var angles);

                                        if (maxCorrelation < correlation)
                                        {
                                            maxCorrelation = Math.Max(correlation, maxCorrelation);
                                            maxDirection = forwardVector;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
    
            return (maxCorrelation, maxDirection);
        }

        /// <summary>
        /// Gets the vector correlation.
        /// </summary>
        /// <param name="vec1">The vec1.</param>
        /// <param name="vec2">The vec2.</param>
        /// <param name="vecAngles">The vec angles.</param>
        private static double GetVectorCorrelation(Point vec1, Point vec2, out double vecAngles)
        {
            vecAngles = Math.Abs(vec1.AngleBetween(vec2));

            //Negative sign in the beginning so the best value is 0 : vecAngles = 0 means the two vectors are perfectly aligned, and their
            //squared lengths are as small as possible. We want to discard combinations where vecAngles is big (sudden change of direction, which
            //cannot happen in the midle of the trajectory (if a player hits the ball, it is the beginning of a new arc, not part of the current one))
            //We also want to discard combinations of vectors with big lengths: this means the balls are too far and are likely to be unrelated
            return -(vecAngles + vec1.SquaredLength() + vec2.SquaredLength());
        }
    }
}
