using System;
using System.Collections.Generic;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The tennis move detector
    /// </summary>
    public class TennisMoveDetector
    {
        /// <summary>
        /// Gets the player moves per frame.
        /// </summary>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="log">The log.</param>
        public static Dictionary<int, MoveLabel> GetForegroundPlayerMovesPerFrame(VideoInfo videoInfo, ProcessedFileLog log)
        {
            var detector = new TennisMoveDetector();

            var foregroundKeypointsArray = new PlayerFrameData[videoInfo.TotalFrames];

            foreach (var kvp in log.ForegroundPlayerKeypoints)
            {
                foregroundKeypointsArray[kvp.Key] = kvp.Value;
            }

            return detector.DetectMovesOnPlayerAddedFramesManual(foregroundKeypointsArray, log.Balls);
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
            var leftKnee = new Accord.Point(keypoints[14] - bodyX, keypoints[15] - bodyY);
            var rightKnee = new Accord.Point(keypoints[8] - bodyX, keypoints[9] - bodyY);

            return new BodyParts(leftWrist, rightWrist, leftKnee, rightKnee);
        }

        /// <summary>
        /// Detects the moves on player added frames.
        /// </summary>
        /// <param name="playerFrameData">The player frame data.</param>
        /// <param name="balls">The balls</param>
        private Dictionary<int, MoveLabel> DetectMovesOnPlayerAddedFramesManual(PlayerFrameData[] playerFrameData, Dictionary<int, List<Accord.Point>> balls)
        {
            var detectedWristSpeeds = new WristSpeedData[playerFrameData.Length];
            var detectedBodyParts = new BodyParts[playerFrameData.Length];

            //This speed does not vary with the actual video dimension, but with the target size
            var minRacketSpeed = 30;
            var squaredRacketSpeed = Math.Pow(minRacketSpeed, 2);

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

                            detectedWristSpeeds[i] = new WristSpeedData(bodyParts.LeftWrist, bodyParts.RightWrist, previousBodyParts.LeftWrist, previousBodyParts.RightWrist, false);
                        }

                        if (previousPreviousFrame != null && PlayerMovementAnalyser.NoErrorKeypoint(previousPreviousFrame.Keypoints))
                        {
                            if (detectedBodyParts[i - 2] == null)
                            {
                                detectedBodyParts[i - 2] = GetBodyParts(previousPreviousFrame.Keypoints);
                            }

                            var ppBodyParts = detectedBodyParts[i - 2];

                            var previousPreviousSpeed = new WristSpeedData(bodyParts.LeftWrist, bodyParts.RightWrist, ppBodyParts.LeftWrist, ppBodyParts.RightWrist, true);

                            if (detectedWristSpeeds[i] == null || detectedWristSpeeds[i].SquaredAbs < previousPreviousSpeed.SquaredAbs)
                            {
                                detectedWristSpeeds[i] = previousPreviousSpeed;
                            }
                        }
                    }
                }
            }

            var minDelayBetweenMoves = 10;

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                var wristSpeed = detectedWristSpeeds[i];

                if (wristSpeed != null)
                {
                    var maxIndex = Math.Min(playerFrameData.Length, i + minDelayBetweenMoves);

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

            var leftWrists = 0;
            var rightWrists = 0;

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                if (detectedWristSpeeds[i] != null)
                {
                    if (detectedWristSpeeds[i].SquaredAbs > squaredRacketSpeed)
                    {
                        if (detectedWristSpeeds[i].Wrist == Wrist.Left)
                        {
                            leftWrists++;
                        }
                        else
                        {
                            rightWrists++;
                        }
                    }
                    else
                    {
                        detectedWristSpeeds[i] = null;
                    }
                }
            }

            //We suppose the speediest wrist is the one of the main hand
            var isLeftHanded = leftWrists > rightWrists;
            var moveData = new Dictionary<int, MoveLabel>();

            for (int i = 0; i < detectedWristSpeeds.Length; i++)
            {
                //If the wrist is going left but never crosses to the left side, then it's a "weird forehand" (if it crosses the body while going left, it's a backhand)
                //Since bodyX is subtracted from wrist, crossing the body = changing sign
                bool handCrossedSide(float wristSpeed)
                { 
                    var wristGoingLeft = wristSpeed < 0;

                    if (isLeftHanded)
                    {
                        for (int s = i + 1; s > i - 5; s--)
                        {
                            //Found a wrist going left that was originally on the right side: it's probably crossing it
                            //The right knee must also be on the right side of the body (otherwise this means the body is facing the camera and depending on the angle, the hand
                            //did not cross sides
                            if ((detectedBodyParts[s]?.RightKnee.X > 0 || detectedBodyParts[s]?.LeftKnee.X < 0)
                                && ((wristGoingLeft && detectedBodyParts[s].LeftWrist.X > 0)
                                    //Found a wrist going right that was originally on the left side, it's probably crossing it
                                    || (!wristGoingLeft && detectedBodyParts[s].LeftWrist.X < 0 && detectedBodyParts[s].LeftKnee.X < 0)))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                    else
                    {
                        for (int s = i + 1; s > i - 5; s--)
                        {
                            if ((detectedBodyParts[s]?.RightKnee.X > 0 || detectedBodyParts[s]?.LeftKnee.X < 0) 
                                && ((wristGoingLeft && detectedBodyParts[s].RightWrist.X > 0) 
                                    || (!wristGoingLeft && detectedBodyParts[s].RightWrist.X < 0)))
                            { 
                                return true;
                            }
                        }

                        return false;
                    }
                }

                var wristSpeed = detectedWristSpeeds[i];

                if (wristSpeed != null)
                {
                    MoveLabel move;

                    var handCrossedToOtherSide = handCrossedSide(wristSpeed.Speed.X);

                    if ((isLeftHanded && wristSpeed.Speed.X > 0) || (!isLeftHanded && wristSpeed.Speed.X < 0))
                    {
                        move = handCrossedToOtherSide ? MoveLabel.Forehand : MoveLabel.Backhand;
                    }
                    else
                    {
                        move = handCrossedToOtherSide ? MoveLabel.Backhand : MoveLabel.Forehand;
                    }

                    moveData.Add(i, move);
                }
            }

            return moveData;
        }
    }
}
