using Accord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TennisHighlights
{
    /// <summary>
    /// The frame data serializer
    /// </summary>
    public static class FrameDataSerializer
    {
        /// <summary>
        /// The ball log file name
        /// </summary>
        public const string BallLogFileName = "ballLog.txt";

        /// <summary>
        /// Parses the double array log.
        /// </summary>
        private static Dictionary<int, List<T>> ParseDoubleArrayLog<T>(Func<string[], T> parseDataFromStringArray, string logToParse = null)
        {
            var dataPerFrame = new Dictionary<int, List<T>>();

            var log = logToParse ?? FileManager.ReadPersistentFile(BallLogFileName);

            if (!string.IsNullOrEmpty(log))
            {
                foreach (var frameData in log.Split('\n'))
                {
                    if (string.IsNullOrEmpty(frameData)) { continue; }

                    var splitFrameData = frameData.Replace("\r", "").Split(':');
                    var splitPlayersData = splitFrameData[1].Split(';');

                    var frameId = int.Parse(splitFrameData[0]);

                    void addSingleBall(string singleBallData)
                    {
                        var coordinates = singleBallData.Replace("(", "").Replace(")", "").Split('|');

                        if (!dataPerFrame.TryGetValue(frameId, out var thisFrameData))
                        {
                            thisFrameData = new List<T>();

                            dataPerFrame.Add(frameId, thisFrameData);
                        }

                        var dataToAdd = parseDataFromStringArray(coordinates);
                        thisFrameData.Add(dataToAdd);
                    }

                    if (splitPlayersData.Length == 0)
                    {
                        addSingleBall(splitFrameData[1]);
                    }
                    else
                    {
                        foreach (var singleBallData in splitPlayersData)
                        {
                            if (!string.IsNullOrEmpty(singleBallData.Replace(" ", "")))
                            {
                                addSingleBall(singleBallData);
                            }
                        }
                    }
                }
            }

            return dataPerFrame;
        }

        /// <summary>
        /// Parses the ball log and returns the last parsed index.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="logToParse">The log to parse.</param>
        public static Dictionary<int, List<Point>> ParseBallLog(string logToParse = null)
        {
            return ParseDoubleArrayLog((coordinates) => new Point(float.Parse(coordinates[0]), float.Parse(coordinates[1])), logToParse);
        }

        /// <summary>
        /// Serializes the balls per frame.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        public static string SerializeBallsPerFrameIntoString(Dictionary<int, List<Point>> ballsPerFrame)
        {
            var ballData = new StringBuilder();
            foreach (var frame in ballsPerFrame)
            {
                ballData.AppendLine(frame.Key.ToString("D6") + ": " + string.Join("; ", frame.Value.Select(b => "(" + b.X + "|" + b.Y + ")")));
            }

            return ballData.ToString();
        }
    }
}
