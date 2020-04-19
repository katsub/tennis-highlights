using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights.Utils;
using TennisHighlights.Utils.PoseEstimation;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The tennis move detector
    /// </summary>
    public class TennisMoveDetector
    {
        /// <summary>
        /// The maximum distance between balls
        /// </summary>
        private static readonly ResolutionDependentParameter _maxInterBallDistance = new ResolutionDependentParameter(900, 2);
        /// <summary>
        /// The minimum wrist speed
        /// </summary>
        private const float _minWristSpeed = 30;
        /// <summary>
        /// The zero point
        /// </summary>
        private static readonly Accord.Point _zeroPoint = new Accord.Point(0, 0);

        /// <summary>
        /// The max ball player distance
        /// </summary>
        private readonly ResolutionDependentParameter _maxBallPlayerDistance = new ResolutionDependentParameter(400, 1);

        /// <summary>
        /// The video information
        /// </summary>
        private readonly VideoInfo _videoInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="TennisMoveDetector"/> class.
        /// </summary>
        /// <param name="videoInfo">The video information.</param>
        public TennisMoveDetector(VideoInfo videoInfo) => _videoInfo = videoInfo;

        /// <summary>
        /// Gets the player moves per frame.
        /// </summary>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="log">The log.</param>
        public static Dictionary<int, MoveData> GetForegroundPlayerMovesPerFrame(VideoInfo videoInfo, ProcessedFileLog log)
        {
            var detector = new TennisMoveDetector(videoInfo);

            var foregroundKeypointsArray = new PlayerFrameData[videoInfo.TotalFrames];

            foreach (var kvp in log.ForegroundPlayerKeypoints)
            {
                foregroundKeypointsArray[kvp.Key] = kvp.Value;
            }

            return detector.DetectMovesOnPlayerAddedFrames(foregroundKeypointsArray, log.Balls);
        }

        /// <summary>
        /// Gets the body parts.
        /// </summary>
        /// <param name="keypoints">The keypoints.</param>      
        private static BodyParts GetBodyParts(float[] keypoints)
        {
            var bodyX = keypoints[28];
            var bodyY = keypoints[29];

            var leftWrist = new Accord.Point(keypoints[14] - bodyX, keypoints[15] - bodyY);
            var rightWrist = new Accord.Point(keypoints[8] - bodyX, keypoints[9] - bodyY);
            var leftKnee = new Accord.Point(keypoints[24] - bodyX, keypoints[25] - bodyY);
            var rightKnee = new Accord.Point(keypoints[18] - bodyX, keypoints[19] - bodyY);
            var leftShoulder = new Accord.Point(keypoints[10] - bodyX, keypoints[11] - bodyY);
            var rightShoulder = new Accord.Point(keypoints[4] - bodyX, keypoints[5] - bodyY);
            var torso = new Accord.Point(bodyX, bodyY);

            return new BodyParts(leftWrist, rightWrist, leftKnee, rightKnee, leftShoulder, rightShoulder, torso);
        }

        /// <summary>
        /// Returns true if a wrist crosses the center of the body (helps identifying if it's a forehand or a backhand
        /// </summary>
        /// <param name="wristSpeed">The wrist speed.</param>
        /// <param name="isLeftHanded">if set to <c>true</c> [is left handed].</param>
        /// <param name="moveFrame">The move frame.</param>
        /// <param name="detectedBodyparts">The detected body parts.</param>
        private bool DominantHandCrossedSide(float wristSpeed, bool isLeftHanded, int moveFrame, BodyParts[] detectedBodyParts)
        {
            //If the wrist is going left but never crosses to the left side, then it's a "weird forehand" (if it crosses the body while going left, it's a backhand)
            //Since bodyX is subtracted from wrist, crossing the body = changing sign
            var wristGoingLeft = wristSpeed < 0;

            if (isLeftHanded)
            {
                for (int s = moveFrame + 1; s > moveFrame - 5; s--)
                {
                    if (detectedBodyParts[s] != null)
                    {
                        //Found a wrist going left that was originally on the right side: it's probably crossing it
                        //The right knee must also be on the right side of the body (otherwise this means the body is facing the camera and depending on the angle, the hand
                        //did not cross sides
                        if ((wristGoingLeft && detectedBodyParts[s].LeftWrist.X > 0)
                            //Found a wrist going right that was originally on the left side, it's probably crossing it
                            || (!wristGoingLeft && detectedBodyParts[s].LeftWrist.X < 0))
                        {
                            if ((wristGoingLeft && detectedBodyParts[s].LeftWrist.X > 0 && detectedBodyParts[s].LeftShoulder.X > 0)
                                //Found a wrist going right that was originally on the left side, it's probably crossing it
                                || (!wristGoingLeft && detectedBodyParts[s].LeftWrist.X < 0 && detectedBodyParts[s].RightShoulder.X < 0))
                            {
                                continue;
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                for (int s = moveFrame + 1; s > moveFrame - 5; s--)
                {
                    if (detectedBodyParts[s] != null)
                    {
                        if ((wristGoingLeft && detectedBodyParts[s].RightWrist.X > 0)
                             || (!wristGoingLeft && detectedBodyParts[s].RightWrist.X < 0))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Erases the close moves that are slower.
        /// </summary>
        /// <param name="playerFrameData">The player frame data.</param>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        /// <param name="minSampleDelayBetweenMoves">The minimum sample delay between moves.</param>
        private void EraseCloseMovesThatAreSlower(PlayerFrameData[] playerFrameData, WristSpeedData[] detectedWristSpeeds, int minSampleDelayBetweenMoves)
        {
            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                var wristSpeed = detectedWristSpeeds[i];

                if (wristSpeed != null)
                {
                    var maxIndex = Math.Min(playerFrameData.Length, i + minSampleDelayBetweenMoves);

                    for (int j = i + 1; j < maxIndex; j++)
                    {
                        var futureWristSpeed = detectedWristSpeeds[j];

                        if (futureWristSpeed != null)
                        {
                            if (futureWristSpeed.SquaredAbs > wristSpeed.SquaredAbs)
                            {
                                detectedWristSpeeds[i] = null;
                            }
                            else
                            {
                                detectedWristSpeeds[j] = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Erases the slow moves.
        /// </summary>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        private void EraseSlowMoves(WristSpeedData[] detectedWristSpeeds)
        {
            //This speed does not vary with the actual video dimension, but with the target size
            var squaredWristSpeed = Math.Pow(_minWristSpeed, 2);

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                if (detectedWristSpeeds[i] != null && detectedWristSpeeds[i].SquaredAbs < squaredWristSpeed)
                {
                    //Logger.Log(LogType.Information, "Deleteted move at sample " + i + " with speed " + detectedWristSpeeds[i].SquaredAbs);

                    detectedWristSpeeds[i] = null;
                }
            }
        }

        /// <summary>
        /// Determines whether [is left handed] [the specified detected wrist speeds].
        /// </summary>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        private bool IsLeftHanded(WristSpeedData[] leftWristSpeeds, WristSpeedData[] rightWristSpeeds)
        {
            var leftWrists = 0;
            var rightWrists = 0;

            for (int i = 0; i < leftWristSpeeds.Length; i++)
            {
                var leftSpeed = leftWristSpeeds[i]?.SquaredAbs ?? 0;
                var rightSpeed = rightWristSpeeds[i]?.SquaredAbs ?? 0;

                if (leftSpeed > rightSpeed)
                {
                    leftWrists++;
                }
                else if (rightSpeed > leftSpeed)
                {
                    rightWrists++;
                }
            }

            return leftWrists > rightWrists;
        }

        /// <summary>
        /// Fills the wrist speeds and body parts.
        /// </summary>
        /// <param name="playerFrameData">The player frame data.</param>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        /// <param name="detectedBodyParts">The detected body parts.</param>
        private void FillWristSpeedsAndBodyParts(PlayerFrameData[] playerFrameData, out WristSpeedData[] leftWristSpeeds, out WristSpeedData[] rightWristSpeeds, out BodyParts[] detectedBodyParts)
        {
            leftWristSpeeds = new WristSpeedData[playerFrameData.Length];
            rightWristSpeeds = new WristSpeedData[playerFrameData.Length];
            detectedBodyParts = new BodyParts[playerFrameData.Length];

            for (int i = 0; i < playerFrameData.Length; i++)
            {
                var currentFrame = playerFrameData[i];
                var previousFrame = i > 0 ? playerFrameData[i - 1] : null;
                var previousPreviousFrame = i > 1 ? playerFrameData[i - 2] : null;

                if (currentFrame != null && previousFrame != null)
                {
                    if (PlayerMovementAnalyser.NoErrorKeypoint(currentFrame.Keypoints))
                    {
                        var bodyParts = GetBodyParts(currentFrame.Keypoints);

                        detectedBodyParts[i] = bodyParts;

                        if (previousFrame != null && PlayerMovementAnalyser.NoErrorKeypoint(previousFrame.Keypoints))
                        {
                            if (detectedBodyParts[i - 1] == null)
                            {
                                detectedBodyParts[i - 1] = GetBodyParts(previousFrame.Keypoints);
                            }

                            var previousBodyParts = detectedBodyParts[i - 1];

                            leftWristSpeeds[i] = new WristSpeedData(bodyParts.LeftWrist, previousBodyParts.LeftWrist, false);
                            rightWristSpeeds[i] = new WristSpeedData(bodyParts.RightWrist, previousBodyParts.RightWrist, false);
                        }

                        if (previousPreviousFrame != null && PlayerMovementAnalyser.NoErrorKeypoint(previousPreviousFrame.Keypoints))
                        {
                            if (detectedBodyParts[i - 2] == null)
                            {
                                detectedBodyParts[i - 2] = GetBodyParts(previousPreviousFrame.Keypoints);
                            }

                            var ppBodyParts = detectedBodyParts[i - 2];

                            var previousPreviousLeftSpeed = new WristSpeedData(bodyParts.LeftWrist, ppBodyParts.LeftWrist, true);
                            var previousPreviousRightSpeed = new WristSpeedData(bodyParts.RightWrist, ppBodyParts.RightWrist, true);

                            if (leftWristSpeeds[i] == null || leftWristSpeeds[i].SquaredAbs < previousPreviousLeftSpeed.SquaredAbs)
                            {
                                leftWristSpeeds[i] = previousPreviousLeftSpeed;
                            }

                            if (rightWristSpeeds[i] == null || rightWristSpeeds[i].SquaredAbs < previousPreviousRightSpeed.SquaredAbs)
                            {
                                rightWristSpeeds[i] = previousPreviousRightSpeed;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Erases the moves without ball going away.
        /// </summary>
        /// <param name="playerFrameData">The player frame data.</param>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        /// <param name="detectedBodyParts">The detected body parts.</param>
        /// <param name="balls">The balls.</param>
        private void EraseMovesWithoutBallGoingAway(PlayerFrameData[] playerFrameData, WristSpeedData[] detectedWristSpeeds, BodyParts[] detectedBodyParts, Dictionary<int, List<Accord.Point>> balls)
        {
            static bool GoesAwayFromPlayer(Accord.Point ball, Accord.Point previousBall, Accord.Point playerTorso)
            {
                return (playerTorso.X > ball.X && (ball.X < previousBall.X)) || (playerTorso.X < ball.X && (ball.X > previousBall.X));
            }

            var framesPerSample = PlayerMovementAnalyser.GetFramesPerSample(_videoInfo.FrameRate);

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                var wristSpeed = detectedWristSpeeds[i];

                //Needs some frames so that we can look at the previous ball and the previous previous one and measure speed
                if (wristSpeed != null && i > 1)
                {
                    var foundBallAwayGoingFromPlayer = false;

                    var squaredMaxDistance = Math.Pow(_maxBallPlayerDistance.Value, 2);

                    var realFrame = i * framesPerSample;

                    var playerTorso = playerFrameData[i].TopLeftCorner + detectedBodyParts[i].Torso.Multiply(playerFrameData[i].Scale);

                    //+10 so the ball has some time to distance itself from the player
                    for (int j = realFrame + 10; j < realFrame + 40; j++)
                    {
                        var playerSampleFrame = (int)(j / framesPerSample);

                        if (detectedBodyParts[playerSampleFrame] != null)
                        {
                            playerTorso = playerFrameData[playerSampleFrame].TopLeftCorner
                                          + detectedBodyParts[playerSampleFrame].Torso.Multiply(playerFrameData[playerSampleFrame].Scale);
                        }

                        var canApproximateTorso = detectedBodyParts[playerSampleFrame] != null && (playerSampleFrame + 1 < playerFrameData.Length)
                                                  && detectedBodyParts[playerSampleFrame + 1] != null;

                        if (j % framesPerSample == 0 || canApproximateTorso)
                        {
                            if (j % framesPerSample != 0 && canApproximateTorso)
                            {
                                //Approximate torso, if it reduces false positives and detects 1485, keep it, otherwise change it or only let this detection happen in j % framesPerSample
                                var nextTorso = playerFrameData[playerSampleFrame + 1].TopLeftCorner
                                                + detectedBodyParts[playerSampleFrame + 1].Torso.Multiply(playerFrameData[playerSampleFrame + 1].Scale);

                                var t = ((float)(j % framesPerSample)) / framesPerSample;

                                playerTorso = playerTorso * (1 - t) + nextTorso * t;
                            }

                            if (balls.TryGetValue(j, out var allCurrentBalls))
                            {
                                var currentBalls = allCurrentBalls.Where(b => b.SquaredDistanceTo(playerTorso) < squaredMaxDistance);

                                balls.TryGetValue(j - 1, out var allPreviousBalls);
                                var previousBalls = allPreviousBalls?.Where(b => b.SquaredDistanceTo(playerTorso) < squaredMaxDistance);
                                balls.TryGetValue(j - 2, out var allPPBalls);
                                var previousPreviousBalls = allPPBalls?.Where(b => b.SquaredDistanceTo(playerTorso) < squaredMaxDistance);

                                var previousBallAway = previousBalls?.FirstOrDefault(pb => currentBalls.Any(b => GoesAwayFromPlayer(b, pb, playerTorso) && b.SquaredDistanceTo(pb) < _maxInterBallDistance.Value));
                                var ppBallAway = previousPreviousBalls?.FirstOrDefault(pb => currentBalls.Any(b => GoesAwayFromPlayer(b, pb, playerTorso) && b.SquaredDistanceTo(pb) < _maxInterBallDistance.Value));

                                //To eliminate false positives, we look for balls going away from the player: if we can't find it, it's probably a false positive
                                if ((previousBallAway != _zeroPoint && previousBallAway != null) || (ppBallAway != _zeroPoint && ppBallAway != null))
                                {
                                    foundBallAwayGoingFromPlayer = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!foundBallAwayGoingFromPlayer)
                    {
                        detectedWristSpeeds[i] = null;

                        //Logger.Log(LogType.Information, "Deleted move from frame " + realFrame + " because no ball going away was found.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the detected moves.
        /// </summary>
        /// <param name="detectedWristSpeeds">The detected wrist speeds.</param>
        /// <param name="isLeftHanded">if set to <c>true</c> [is left handed].</param>
        /// <param name="detectedBodyParts">The detected body parts.</param>
        private Dictionary<int, MoveData> GetDetectedMoves(WristSpeedData[] detectedWristSpeeds, bool isLeftHanded, BodyParts[] detectedBodyParts)
        {
            var moveData = new Dictionary<int, MoveData>();

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                var wristSpeed = detectedWristSpeeds[i];

                if (wristSpeed != null)
                {
                    MoveLabel move;

                    var moveHandCrossedToOtherSide = DominantHandCrossedSide(wristSpeed.Speed.X, isLeftHanded, i, detectedBodyParts);

                    if ((isLeftHanded && wristSpeed.Speed.X > 0) || (!isLeftHanded && wristSpeed.Speed.X < 0))
                    {
                        move = moveHandCrossedToOtherSide ? MoveLabel.Forehand : MoveLabel.Backhand;
                    }
                    else
                    {
                        move = moveHandCrossedToOtherSide ? MoveLabel.Backhand : MoveLabel.Forehand;
                    }

                    //Check if the player is still visible after the move: if he disappears shortly after, it might be a misinterpreted move of the playes running away from the camera
                    if (i < detectedWristSpeeds.Length - 2 && detectedBodyParts[i + 1] != null && detectedBodyParts[i + 2] != null)
                    {
                        moveData.Add(i, new MoveData(move, i, detectedWristSpeeds[i].Speed));
                    }
                }
            }

            return moveData;
        }

        /// <summary>
        /// Detects the moves on player added frames.
        /// </summary>
        /// <param name="playerFrameData">The player frame data.</param>
        /// <param name="balls">The balls</param>
        private Dictionary<int, MoveData> DetectMovesOnPlayerAddedFrames(PlayerFrameData[] playerFrameData, Dictionary<int, List<Accord.Point>> balls)
        {
            FillWristSpeedsAndBodyParts(playerFrameData, out var leftWristSpeeds, out var rightWristSpeeds, out var detectedBodyParts);

            //We suppose the wrist that is speediest most often is the one of the main hand
            var isLeftHanded = IsLeftHanded(leftWristSpeeds, rightWristSpeeds);

            var candidateWristSpeeds = isLeftHanded ? leftWristSpeeds : rightWristSpeeds;

            EraseMovesWithoutBallGoingAway(playerFrameData, candidateWristSpeeds, detectedBodyParts, balls);

            EraseCloseMovesThatAreSlower(playerFrameData, candidateWristSpeeds, 10);

            EraseSlowMoves(candidateWristSpeeds);

            return GetDetectedMoves(candidateWristSpeeds, isLeftHanded, detectedBodyParts);
        }
    }
}
