using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Moves;
using TennisHighlights.Rallies;

namespace TennisHighlights.Annotation
{
    /// <summary>
    /// The gizmo drawer
    /// </summary>
    public static class GizmoDrawer
    {
        /// <summary>
        /// Draws the gizmos and save image.
        /// </summary>
        /// <param name="drawFrame">The draw frame.</param>
        /// <param name="i">The i.</param>
        /// <param name="balls">The balls.</param>
        /// <param name="candidateBalls">The candidate balls.</param>
        /// <param name="players">The players.</param>
        public static void DrawGizmosAndSaveImage(Bitmap drawFrame, int i, List<Accord.Point> balls)
        {
            ImageUtils.DrawRectangles(drawFrame, new List<Boundary>() { new Boundary(0, 60, 0, 20) }, null, Brushes.White);
            ImageUtils.DrawText(drawFrame, i.ToString(), new Accord.Point(0, 0), 15, Brushes.Red);

            if (balls != null)
            {
                ImageUtils.DrawCircles(drawFrame, balls, 4, Brushes.Red);
            }

            FileManager.WriteTempFile(i.ToString("D6") + ".jpg", drawFrame, FileManager.FrameFolder);

            drawFrame.Dispose();
        }

        /// <summary>
        /// Draws the rallies.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        private static void DrawRallies(List<Rally> rallies)
        {
            Logger.Instance.Log(LogType.Information, "Drawing rallies...");

            FillRallies(rallies);

            var i = 0;

            foreach (var rally in rallies)
            {
                foreach (var arc in rally.Arcs)
                {
                    foreach (var ball in arc.Balls.Values)
                    {
                        var frame = FileManager.ReadTempBitmapFile(ball.FrameIndex.ToString("D6") + ".jpg", FileManager.FrameFolder);

                        if (frame != null)
                        {
                            var frameCopy = new Bitmap(frame);

                            frame.Dispose();

                            ImageUtils.DrawCircles(frameCopy, new List<Accord.Point>() { ball.Position }, 20, Brushes.Purple);

                            ImageUtils.DrawText(frameCopy, "Rally: " + i, new Accord.Point(100, 0), 30, Brushes.Red);

                            FileManager.WriteTempFile(ball.FrameIndex.ToString("D6") + ".jpg", frameCopy, FileManager.RallyFolder);

                            frameCopy.Dispose();
                        }
                    }
                }

                i++;
            }

            Logger.Instance.Log(LogType.Information, "Drawed rallies.");
        }

        /// <summary>
        /// Builds the rally video.
        /// </summary>
        /// <param name="ballsPerFrame">The balls per frame.</param>
        /// <param name="rallies">The rallies.</param>
        public static void BuildRallyGizmoVideo(Dictionary<int, List<Accord.Point>> ballsPerFrame, List<Rally> rallies, TennisHighlightsSettings settings, VideoInfo videoInfo)
        {
            var stopwatch = new Stopwatch();
            Logger.Instance.Log(LogType.Information, "Building output video...");

            DrawRallies(rallies);

//            var videoWriter = new VideoFileWriter();
            var framerate = (int)Math.Round(videoInfo.FrameRate * 1.3d);
            var path = Path.GetFullPath(FileManager.TempDataPath + "output.avi");
            int videoBitRate = 1200 * 300;
            var lastFrame = ballsPerFrame.Last().Key;
            var firstBitmapRead = true;

            for (int i = 0; i < lastFrame; i++)
            {
                if (settings.General.UseCustomStartFrame && i < settings.General.CustomStartMinute) { continue; }

                var frame = FileManager.ReadTempBitmapFile(i.ToString("D6") + ".jpg", FileManager.RallyFolder);

                if (frame == null) { frame = FileManager.ReadTempBitmapFile(i.ToString("D6") + ".jpg", FileManager.FrameFolder); }

                if (frame != null)
                {
                    if (firstBitmapRead)
                    {
  //                      videoWriter.Open(path, frame.Width, frame.Height, framerate, VideoCodec.MPEG4, videoBitRate);

                        firstBitmapRead = false;
                    }

    //                videoWriter.WriteVideoFrame(frame);

                    frame.Dispose();
                }
            }

      //      videoWriter.Close();

            Logger.Instance.Log(LogType.Information, "Done. " + stopwatch.Elapsed);
        }

        /// <summary>
        /// Fills the rallies.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        private static void FillRallies(List<Rally> rallies)
        {
            foreach (var rally in rallies)
            {
                foreach (var arc in rally.Arcs)
                {
                    var newBalls = new SortedList<int, ArcBallData>();
                    var previousBall = arc.Balls.First();

                    foreach (var ball in arc.Balls)
                    {
                        if (ball.Value == arc.Balls.First().Value)
                        {
                            if (arc != rally.Arcs.First())
                            {
                                previousBall = rally.Arcs[rally.Arcs.IndexOf(arc) - 1].Balls.Last();
                            }
                        }

                        var missingFrames = ball.Key - previousBall.Key - 1;

                        for (int i = 1; i <= missingFrames; i++)
                        {
                            var missingPosition = previousBall.Value.Position.Multiply((double)(missingFrames - i) / missingFrames)
                                                  + ball.Value.Position.Multiply((double)((double)i / missingFrames));

                            var frameKey = previousBall.Key + i;

                            newBalls.Add(frameKey, new ArcBallData(frameKey, -1, missingPosition, ball.Value.SpeedDirection, ball.Value.SpeedSquaredMagnitude, ball.Value.Correlation, ball.Value.Angles));
                        }

                        newBalls.Add(ball.Key, ball.Value);

                        previousBall = ball;
                    }

                    arc.Balls.Clear();

                    foreach (var ball in newBalls)
                    {
                        arc.Balls.Add(ball.Key, ball.Value);
                    }
                }
            }
        }
    }
}
