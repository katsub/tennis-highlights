using System;
using System.Collections.Generic;
using System.Diagnostics;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

namespace TennisHighlights.VideoCreation
{
    /// <summary>
    /// The rally video creator
    /// </summary>
    public class RallyVideoCreator
    {
        /// <summary>
        /// Builds the video with all rallies.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="error">The error.</param>
        /// <param name="updateProgressInfo">The update progress information.</param>
        /// <param name="gotCanceled">The got canceled.</param>
        public static void BuildVideoWithAllRallies(List<RallyEditData> rallies, VideoInfo videoInfo, GeneralSettings settings, out string error,
                                                    Action<string, int, double> updateProgressInfo = null, Func<bool> gotCanceled = null)
        {
            //Join all rallies that overlap
            for (int j = rallies.Count - 1; j > 0; j--)
            {
                var currentRally = rallies[j];
                var previousRally = rallies[j - 1];

                if (currentRally.Start <= previousRally.Stop)
                {
                    previousRally.Stop = currentRally.Stop;
                    rallies.RemoveAt(j);
                }
            }

            error = null;
            FileManager.CleanFolder(FileManager.RallyVideosFolder);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var i = 0;

            var rotationEnabled = settings.RotationAngles != 0d;
            var trimmingStepPercentage = rotationEnabled ? 33d : 50d;

            foreach (var rally in rallies)
            {
                if (gotCanceled?.Invoke() == true) { return; }

                var percent = trimmingStepPercentage * i / rallies.Count;

                updateProgressInfo?.Invoke($"Trimming rallies... ({i}/{rallies.Count})", (int)Math.Round(percent), stopwatch.Elapsed.TotalSeconds);

                //Stop if an error was found
                var success = FFmpegCaller.TrimRallyFromAnalysedFile(i, rally.Start / videoInfo.FrameRate, rally.Stop / videoInfo.FrameRate, settings.AnalysedVideoPath, out error, gotCanceled);

                if (!success) { return; }

                i++;
            }

            updateProgressInfo?.Invoke("Joining videos...", (int)trimmingStepPercentage, stopwatch.Elapsed.TotalSeconds);

            if (gotCanceled?.Invoke() == true) { return; }

            var joinedFilePath = FileManager.GetUnusedFilePathInFolderFromFileName(settings.AnalysedVideoPath.Substring(0, settings.AnalysedVideoPath.Length - 4).ToString() + "_rallies.mp4",
                                                                                   FileManager.TempDataPath, ".mp4");

            void beganProgressUpdate()
            {
                updateProgressInfo?.Invoke("Rotating video...", (int)(2d * trimmingStepPercentage), stopwatch.Elapsed.TotalSeconds);
            }

            FFmpegCaller.JoinAllRallyVideos(joinedFilePath, out error, gotCanceled, beganProgressUpdate);

            Process.Start("explorer.exe", FileManager.TempDataPath);

            FileManager.CleanFolder(FileManager.RallyVideosFolder);
            FileManager.DeleteFolder(FileManager.RallyVideosFolder);
        }
    }
}
