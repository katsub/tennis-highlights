using Accord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Moves;
using TennisHighlights.Rallies;

namespace TennisHighlights
{
    /// <summary>
    /// The tennis highlights engine
    /// </summary>
    public class TennisHighlightsEngine
    {
        /// <summary>
        /// Gets the rallies from balls.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="processedFileLog">The processed file log.</param>
        /// <param name="rallyProgressUpdateInfo">The rally progress update information.</param>
        /// <param name="wasCancelRequested">The was cancel requested.</param>
        public static List<Rally> GetRalliesFromBalls(TennisHighlightsSettings settings, Dictionary<int, List<Point>> ballsPerFrame,
                                                      ProcessedFileLog processedFileLog, Action<int, int> rallyProgressUpdateInfo = null,
                                                      Func<bool> wasCancelRequested = null)
        {
            var arcsPerFrame = GetArcsPerFrame(ballsPerFrame);

            //We assume the video aspect ratio to be 1.78 and the analysed height to be the same from when the balls were calculated
            return BuildRallies(settings.RallyBuildingSettings, arcsPerFrame,
                                new System.Drawing.Size((int)1.78d * settings.General.FrameMaxHeight,
                                                        settings.General.FrameMaxHeight),
                                rallyProgressUpdateInfo, wasCancelRequested);

        }

        /// <summary>
        /// Builds the rallies.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="arcsPerFrame">The arcs per frame.</param>
        /// <param name="targetSize">Size of the target.</param>
        /// <param name="rallyProgressUpdateInfo">The rally progress update information.</param>
        /// <param name="wasCancelRequested">The was cancel requested.</param>
        private static List<Rally> BuildRallies(RallyBuildingSettings settings, Dictionary<int, Dictionary<int, Arc>> arcsPerFrame, 
                                                System.Drawing.Size targetSize, Action<int, int> rallyProgressUpdateInfo = null,
                                                Func<bool> wasCancelRequested = null)
        {
            var rallyBuilder = new RallyBuilder(settings, targetSize);

            var rallies = rallyBuilder.BuildRallies(arcsPerFrame, rallyProgressUpdateInfo, wasCancelRequested);

            return rallies;
        }

        /// <summary>
        /// Converts the balls to dico.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        public static Dictionary<int, List<Point>> ConvertBallsToDico(List<Point>[] ballsPerFrame)
        {
            var dico = new Dictionary<int, List<Point>>();

            for (int index = 0; index < ballsPerFrame.Length; index++)
            {
                var frame = ballsPerFrame[index];

                if (frame != null)
                {
                    dico.Add(index, frame);
                }
            }

            return dico;
        }

        /// <summary>
        /// Checks the arc consistency.
        /// </summary>
        /// <param name="arc">The arc.</param>
        private static bool CheckArcConsistency(Arc arc)
        {
            //Validate the arc if it or one of its subsets satisfies the criteria
            bool isSubArcConsistant(IEnumerable<ArcBallData> balls)
            {
                var numberOfBalls = balls.Count();

                if (numberOfBalls <= 3) { return false; }

                var firstBall = balls.First();
                var lastBall = balls.Last();

                return (firstBall.Position - lastBall.Position).SquaredLength() > 1000
                       && (((double)numberOfBalls) / (lastBall.FrameIndex - firstBall.FrameIndex)) > 0.5d;
            }

            for (int i = 0; i < arc.Balls.Count; i++)
            {
                for (int j = 0; j < arc.Balls.Count; j++)
                {
                    var analysedBalls = arc.Balls.Values.Skip(i).Take(j);

                    if (isSubArcConsistant(analysedBalls)) { return true; }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the arcs per frame.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        private static Dictionary<int, Dictionary<int, Arc>> GetArcsPerFrame(Dictionary<int, List<Point>> ballsPerFrame)
        {
            var arcsPerBall = new Dictionary<int, Dictionary<int, Arc>>();
            var lastBallFrame = ballsPerFrame.Last().Key;

            for (int s = 0; s < lastBallFrame; s++)
            {
                if (ballsPerFrame.TryGetValue(s, out var thisFrameBalls))
                {
                    for (int t = 0; t < thisFrameBalls.Count; t++)
                    {
                        var candidateArc = ArcExtractor.GetArc(ballsPerFrame, s, t);

                        if (candidateArc != null)
                        {
                            var a = false;
                        }

                        if (candidateArc != null && CheckArcConsistency(candidateArc))
                        {
                            var isNewArc = true;

                            foreach (var ball in candidateArc.Balls)
                            {
                                //Check if the arc doesn't coincide with one built having another ball as a starting point
                                if (arcsPerBall.TryGetValue(ball.Key, out var thisFrameDict)
                                    && thisFrameDict.TryGetValue(ball.Value.BallIndex, out var existingBallArc)
                                    && existingBallArc.Balls.Count == candidateArc.Balls.Count
                                    && existingBallArc.IsSimilarArc(candidateArc))
                                {
                                    isNewArc = false;
                                    break;
                                }
                            }

                            if (isNewArc)
                            {
                                if (!arcsPerBall.ContainsKey(s))
                                {
                                    arcsPerBall[s] = new Dictionary<int, Arc>();
                                }

                                arcsPerBall[s][t] = candidateArc;
                            }
                        }
                    }
                }
            }

            return arcsPerBall;
        }
    }
}
