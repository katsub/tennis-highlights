using Accord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        public static ResolutionDependentParameter NoiseSquaredSpeed = new ResolutionDependentParameter(5d, 2d);
        /// <summary>
        /// The maximum acceptable angle error for the next ball, in regards to the angle between the current ball and the previous ball
        /// </summary>
        private const float _nextBallAngleError = 15f;
        /// <summary>
        /// The ball interframe maximum square distance (max squared speed)
        /// </summary>
        private static ResolutionDependentParameter _ballInterframeMaxSquareDistance = new ResolutionDependentParameter(900d, 2d);
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
        private static ResolutionDependentParameter _maxSquaredSpeedRelativeDelta = new ResolutionDependentParameter(10d, 2d);
        /// <summary>
        /// The empty vector
        /// </summary>
        private static Point _emptyVector = new Point(0, 0);
        /// <summary>
        /// The maximum squared speed mag
        /// </summary>
        private static ResolutionDependentParameter _maxSquaredSpeedMag = new ResolutionDependentParameter(30d, 2d);
        /// <summary>
        /// The maximum projection delta squared magnitude
        /// </summary>
        private static ResolutionDependentParameter _maxProjectionDeltaSquaredMagnitude = new ResolutionDependentParameter(1500d, 2d);

        /// <summary>
        /// Gets the longest arc for frame.
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
            var (momentum, maxDirection) = GetMaxMomentum(ballsPerFrame, frameKey, ballIndex);

            if (momentum > _minimumCorrelationForArc)
            {
                var arc = new Arc();

                arc.Balls.Add(frameKey, new ArcBallData(frameKey, ballIndex, ballsPerFrame[frameKey][ballIndex], maxDirection, maxDirection.SquaredLength(), momentum, 0d));

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
                    var nextBallMomentums = new List<(ArcBallData arcBallData, double angles)>();

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
                                nextBallMomentums.Add((new ArcBallData(currentNextFrame, i, ball, backVector, speedSquaredMag, correlation, vecAngles), vecAngles));
                            }
                        }

                        i++;
                    }

                    double angles;
                    (newArcBallData, angles) = nextBallMomentums.OrderBy(b => b.arcBallData.Correlation).FirstOrDefault();

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
        /// Gets the maximum momentum.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="frameKey">The frame key.</param>
        /// <param name="ballIndex">Index of the ball.</param>
        public static (double momentum, Point maxDirection) GetMaxMomentum(Dictionary<int, List<Point>> ballsPerFrame, int frameKey, int ballIndex)
        {
            var ball = ballsPerFrame[frameKey][ballIndex];

            var maxCorrelation = double.NegativeInfinity;
            var maxDirection = _emptyVector;

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
                                        //Precisa levar em conta a orientacao no momento coherence (se um aponta pra cima e o outro pra baixo, tem que somar 180 no ângulo)
                                        var forwardVector = forwardBall - ball;
                                        //TODO: slow (uses vector lengths)
                                        //var momentum = GetDotProduct(backVector, forwardVector, out var vecAngles) / (forwardVector.Length * backVector.Length);
                                        //medida ruim: a medida tem que ser ângulo entre forward e back + mòdulo forward + mòdulo back. Se minimizar essa medida
                                        //vai dar certo 
                                        //TODO: tem bola repetida (mesma posiçao, nao deixar adicionar essas na hora da deteccao)
                                        var momentum = GetVectorCorrelation(forwardVector, backVector, out var angles);

                                        if (maxCorrelation < momentum)
                                        {
                                            maxCorrelation = Math.Max(momentum, maxCorrelation);
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
        /// Gets the arc confidence point.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        public static (int frame, int ballIndex, Point direction, double momentum, double speedMag) GetArcConfidencePoint(Dictionary<int, List<Point>> ballsPerFrame)
        {
            var ballMomentums = new Dictionary<(int frame, int ballIndex), (double momentum, Point direction)>();

            foreach (var frame in ballsPerFrame)
            {
                for (int i = 0; i < frame.Value.Count; i++)
                {
                    var (momentum, speedMag) = GetMaxMomentum(ballsPerFrame, frame.Key, i);

                    ballMomentums.Add((frame.Key, i), (momentum, speedMag));
                }
            }

            if (ballMomentums.Count > 0)
            {
                var mostProbableBall = ballMomentums.OrderByDescending(m => m.Value.momentum).First();

                return (mostProbableBall.Key.frame, mostProbableBall.Key.ballIndex, mostProbableBall.Value.direction, mostProbableBall.Value.momentum,
                        mostProbableBall.Value.direction.SquaredLength());
            }

            return (-1, -1, _emptyVector, double.NaN, double.NaN);
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

            return -(vecAngles + vec1.SquaredLength() + vec2.SquaredLength());
        }
    }
}
