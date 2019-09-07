using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using TennisHighlights.Annotation;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;
using TennisHighlights.VideoCreation;

namespace TennisHighlights
{
    class Program
    {
        private static Dictionary<int, List<Accord.Point>> _debugBallsPerFrame;

        static void Main(string[] args)
        {
            Logger.Log(LogType.Information, "Application started.");

            try
            {
                Console.WindowWidth = 150;

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var settings = new TennisHighlightsSettings();

                FileManager.Initialize(settings.General);

                var processedFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(settings.General);
                //This creates an initial settings file that can be modified later if needed
                settings.Save();

                FileManager.Clean();

                var videoInfo = new VideoInfo(settings.General.AnalysedVideoPath);

                var ballsPerFrame = new VideoBallsExtractor(settings, videoInfo, processedFileLog).GetBallsPerFrame().Result;
                
                _debugBallsPerFrame = ballsPerFrame;

                var rallies = TennisHighlightsEngine.GetRalliesFromBalls(settings, ballsPerFrame, processedFileLog);

                Logger.Log(LogType.Information, "Elapsed: " + stopwatch.Elapsed);

                if (settings.General.DrawGizmos)
                {
                    GizmoDrawer.BuildRallyGizmoVideo(ballsPerFrame, rallies, settings, videoInfo);
                }
                else
                {
                    //RallyVideoCreator.BuildVideoWithAllRallies(rallies, videoInfo, settings.General);
                }

                Logger.Log(LogType.Information, "Total time: " + stopwatch.Elapsed);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Errors were encountered: " + e.ToString());
            }

            Console.ReadLine();
        }
    }
}
