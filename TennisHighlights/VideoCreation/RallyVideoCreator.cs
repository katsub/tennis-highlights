﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using TennisHighlights.ImageProcessing;

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
            error = null;
            FileManager.CleanFolder(FileManager.RallyVideosFolder);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var i = 0;
            foreach (var rally in rallies)
            {
                if (gotCanceled?.Invoke() == true) { return; }

                var percent = 50d * i / rallies.Count;

                updateProgressInfo?.Invoke($"Trimming rallies... ({i}/{rallies.Count})", (int)Math.Round(percent), stopwatch.Elapsed.TotalSeconds);

                //Stop if an error was found
                var success = FFMPEGCaller.TrimRallyFromAnalysedFile(i, rally.Start / videoInfo.FrameRate, rally.Stop / videoInfo.FrameRate, settings.AnalysedVideoPath, out error, gotCanceled);

                if (!success) { return; }

                i++;
            }

            updateProgressInfo?.Invoke("Joining videos...", 50, stopwatch.Elapsed.TotalSeconds);

            if (gotCanceled?.Invoke() == true) { return; }

            var joinedFilePath = FileManager.GetUnusedFilePathInFolderFromFileName(settings.AnalysedVideoPath.Substring(0, settings.AnalysedVideoPath.Length - 4).ToString() + "_rallies.mp4",
                                                                                   FileManager.TempDataPath, ".mp4");

            FFMPEGCaller.JoinAllRallyVideos(joinedFilePath, out error, gotCanceled);

            Process.Start("explorer.exe", FileManager.TempDataPath);

            FileManager.CleanFolder(FileManager.RallyVideosFolder);
            FileManager.DeleteFolder(FileManager.RallyVideosFolder);
        }
    }
}
