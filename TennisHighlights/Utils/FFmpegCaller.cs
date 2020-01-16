﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TennisHighlights.ImageProcessing;

namespace TennisHighlights.Utils
{
    /// <summary>
    /// The FFmpeg caller
    /// </summary>
    public static class FFmpegCaller
    {
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        public static GeneralSettings Settings { get; set; }
        /// <summary>
        /// Gets or sets the video info.
        /// </summary>
        public static VideoInfo VideoInfo { get; set; }
        /// <summary>
        /// The FFmpeg path
        /// </summary>
        public static string FFmpegPath { get; set; }

        /// <summary>
        /// The instances
        /// </summary>
        private static readonly List<Process> _instances = new List<Process>();

        /// <summary>
        /// Kills all instances.
        /// </summary>
        public static void KillAllInstances()
        {
            foreach (var instance in _instances)
            {
                try
                {
                    instance.Kill();
                }
                catch { }
            }
        }

        /// <summary>
        /// Calls FFmpeg with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="error">The error.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static async Task<(bool success, string error)> Call(string arguments, Func<bool> askedToStop = null)
        {
            string error = null;

            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = FFmpegPath;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                if (!proc.Start())
                {
                    Logger.Log(LogType.Error, "Could not start FFmpeg.");
                    return (false, error);
                }

                _instances.Add(proc);

                await Task.Delay(1000);

                try
                {
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    proc.OutputDataReceived += (sender, e) => {
                        Logger.Log(LogType.Information, e.Data);
                    };

                    proc.ErrorDataReceived += (sender, e) =>
                    {
                        Logger.Log(LogType.Error, e.Data);
                    };

                    while (!proc.HasExited)
                    {
                        await Task.Delay(1000);

                        if (askedToStop?.Invoke() == true)
                        {
                            proc.Kill();
                            break;
                        }
                    }
                }
                catch { }

                await Task.Delay(1000);
                
                proc.Close();

                _instances.Remove(proc);

                return (true, error);
            }
            catch (Exception e)
            {
                var message = "Could not call FFmpeg: " + e.ToString();

                error = message;

                Logger.Log(LogType.Error, message);

                return (false, error);
            }
        }

        /*
        /// <summary>
        /// Kills the process if asked to stop.
        /// </summary>
        /// <param name="askedToStop">The asked to stop.</param>
        private static async void KillProcessIfAskedToStop(Process proc, Func<bool> askedToStop = null)
        {
            try
            {
                //wait a little for the process to start
                await Task.Delay(1000);

                var hasExited = false;

                while (!hasExited)
                {
                    Task.Run(() =>
                    {
                    });

                    if (askedToStop?.Invoke() == true)
                    {
                        try
                        {
                            proc.Kill();
                        }
                        catch { }

                        break;
                    }

                    await Task.Delay(1000);

                    hasExited = proc.HasExited;
                }
            }
            //Process is over, checking has existed will throw "process is not associated with any running process"
            catch (InvalidOperationException) { }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Kill process failed: " + e.ToString());
            }
        }
        */

        /// <summary>
        /// Trims the rally from analysed file.
        /// </summary>
        /// <param name="rallyIndex">Index of the rally.</param>
        /// <param name="startSeconds">The start seconds.</param>
        /// <param name="stopSeconds">The stop seconds.</param>
        /// <param name="originalFile">The original file.</param>
        /// <param name="error">The error.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static bool TrimRallyFromAnalysedFile(int rallyIndex, double startSeconds, double stopSeconds, string originalFile, out string error, Func<bool> askedToStop = null)
        {
            var fileName = FileManager.TempDataPath + FileManager.RallyVideosFolder + "//" + rallyIndex.ToString("D3") + ".mp4";

            return TrimRallyFromAnalysedFile(fileName, startSeconds, stopSeconds, originalFile, out error, askedToStop);
        }

        /// <summary>
        /// Exports the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="startSeconds">The start seconds.</param>
        /// <param name="stopSeconds">The stop seconds.</param>
        /// <param name="originalFile">The original file.</param>
        /// <param name="error">The error.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        /// <param name="rotationDegrees">The rotation in degrees.</param>
        public static bool ExportRally(string fileName, double startSeconds, double stopSeconds, string originalFile, double rotationDegrees, out string error, Func<bool> askedToStop = null)
        {
            if (TrimRallyFromAnalysedFile(fileName, startSeconds, stopSeconds, originalFile, out error, askedToStop))
            {
                if (rotationDegrees > 0)
                {
                    RotateSingleVideo(fileName, rotationDegrees, out var error2);

                    File.Delete(fileName);

                    error += error2;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Trims the rally from analysed file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="startSeconds">The start seconds.</param>
        /// <param name="stopSeconds">The stop seconds.</param>
        /// <param name="originalFile">The original file.</param>
        /// <param name="error">The error.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static bool TrimRallyFromAnalysedFile(string fileName, double startSeconds, double stopSeconds, string originalFile, out string error, Func<bool> askedToStop = null)
        {
            Directory.CreateDirectory(FileManager.TempDataPath + FileManager.RallyVideosFolder);
            var arguments = "";
            var startPoint = " -ss " + TimeSpan.FromSeconds(startSeconds); ;
            var inputFile = " -i " + "\"" + originalFile + "\"";

            //FFmpeg interprets arguments differently depending on their position: setting the -ss option before the input is fast while setting it
            //before the output ensures precision
            if (Settings.PreciseTrimming)
            {
                arguments += inputFile;
                arguments += startPoint;
            }
            else
            {
                arguments += startPoint;
                arguments += inputFile;
            }

            arguments += " -t " + TimeSpan.FromSeconds(stopSeconds - startSeconds);

            arguments += " -c:a copy ";
            arguments += Settings.CopyNonKeyframes ? " -copyinkf " : " -c:v copy ";
            arguments += " \"" + fileName + "\" ";

            var taskResult = Call(arguments, askedToStop).Result;

            error = taskResult.error;

            return taskResult.success;
        }

        /// <summary>
        /// Joins the files
        /// </summary>
        /// <param name="resultFilePath">The result file path</param>
        /// <param name="filePaths">The file paths</param>
        /// <param name="cancelRequested">Returns true if the join has been cancelled</param>
        public static string JoinFiles(string resultFilePath, ICollection<string> filePaths, Func<bool> cancelRequested)
        {
            var joinFilesPath = Path.GetDirectoryName(filePaths.First()) + "\\filesToJoin.txt";

            var joinFilesPaths = new StringBuilder();

            foreach (var fileToJoin in filePaths)
            {
                joinFilesPaths.AppendLine("file '" + fileToJoin + "'");
            }

            File.WriteAllText(joinFilesPath, joinFilesPaths.ToString());

            File.Delete(resultFilePath);

            var arguments = "-f concat -safe 0 -i \"" + joinFilesPath + "\" -c:v copy -c:a copy " + "\"" + resultFilePath + "\"";

            var error = Call(arguments, cancelRequested).Result.error;

            if (!string.IsNullOrEmpty(error))
            {
                Logger.Log(LogType.Error, error);

                return error;
            }

            File.Delete(joinFilesPath);

            return string.Empty;
        }

        /// <summary>
        /// Joins all rally videos.
        /// </summary>
        /// <param name="rotationDegrees">The rotation in degrees.</param>
        /// <param name="resultFilePath">The result file path.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static void JoinAllRallyVideos(string resultFilePath, double rotationDegrees, out string error, Func<bool> askedToStop = null)
        {
            var rallyFolderPath = FileManager.TempDataPath + FileManager.RallyVideosFolder + "\\";
            var rallyFilePath = rallyFolderPath + "rallies.txt";

            var ralliesPaths = new StringBuilder();

            foreach (var rally in Directory.GetFiles(rallyFolderPath).Where(f => f.EndsWith(".mp4")).OrderBy(b => b))
            {
                ralliesPaths.AppendLine("file '" + rally + "'");
            }

            var needsRotation = rotationDegrees != 0;

            //If only the rotation is done, video quality is reduced (why? just saw a stackoverflow post saying it happens)
            //In that case, high bitrate must be forced (ideally we'd get the original bitrate and force it, but 10 is a pretty high
            //number. TODO: check if it gets the maximum number availabl
            var reencodeArguments = Settings.LimitMaxVideoBitrate && Settings.MaxVideoBitrate > 0
                                    ? " -b:v " + Settings.MaxVideoBitrate + "M "
                                    : needsRotation ? "-b:v 10M "
                                                    : "";

            var videoCodec = !string.IsNullOrEmpty(reencodeArguments) ? (reencodeArguments 
                                                                         + (needsRotation ? GetRotationArguments(rotationDegrees, true) : ""))
                                                                      : " -c:v copy ";

            File.WriteAllText(rallyFilePath, ralliesPaths.ToString());

            //The videos generated by the program can't be used as input files unless the option "max_muxing_queue_size 9999 is added"
            //why?
            var arguments = "-f concat -safe 0 -i \"" + rallyFilePath + "\"" + videoCodec + " -c:a copy -max_muxing_queue_size 9999 " + "\"" + resultFilePath + "\"";

            error = Call(arguments, askedToStop).Result.error;
        }

        /// <summary>
        /// Gets the rotation arguments.
        /// </summary>
        /// <param name="rotationDegrees">The rotation in degrees.</param>
        /// <param name="calledFromJoin">True if called from Join, false otherwise</param>
        public static string GetRotationArguments(double rotationDegrees, bool calledFromJoin = false)
        {
            //TODO: Accept angles in double: see how to write culture invariant numbers so FFmpeg won't have a problem
            //depending on the number being written with a dot or a comma
            var rotationDegreesRounded = (int)Math.Round(rotationDegrees);

            var cropRect = CropRotationHelper.GetCropCoordinates(rotationDegreesRounded, new OpenCvSharp.Rect(0, 0, VideoInfo.Width, VideoInfo.Height));

            //For some reason FFmpeg won't rotate more than 90 degrees, vflip is needed to it actually turns upside down
            var vflip = rotationDegreesRounded > 90 ? "vflip," : string.Empty;
            var angleSign = rotationDegreesRounded > 90 ? "-" : string.Empty;

            //For reasons I have yet to understand, when called from join, it doesn't need vlip or angleSign
            if (calledFromJoin)
            {
                vflip = string.Empty;
                angleSign = string.Empty;
            }

            return " -max_muxing_queue_size 99999 -vf \"" + vflip + "rotate=" + angleSign + rotationDegreesRounded + "*PI/180, crop=" + cropRect.Width + ":" + cropRect.Height + ":" + cropRect.X + ":" + cropRect.Y + "\" ";
        }

        /// <summary>
        /// Rotates the single video.
        /// </summary>
        /// <param name="fileToRotate">The file to rotate.</param>
        /// <param name="error">The error.</param>
        /// <param name="rotationDegrees">The rotation in degrees.</param>
        /// <param name="calledFromJoin">if set to <c>true</c> [called from join].</param>
        public static void RotateSingleVideo(string fileToRotate, double rotationDegrees, out string error, bool calledFromJoin = false)
        {
            var inputFileArguments = " -i " + "\"" + fileToRotate + "\"";

            var inputFileName = fileToRotate.Substring(0, fileToRotate.Length - 4);

            //When rotating, a high bitrate needs to be forced, otherwise the rotation will reduce the video quality (unknown reason but 
            //according to stackoverflow it's a ffmpeg problem
            var videoCodec = "-b:v 10M " + GetRotationArguments(rotationDegrees, calledFromJoin);

            var outputFile = FileManager.GetUnusedFilePathInFolderFromFileName(inputFileName + "_rotated.mp4",
                                                                               FileManager.TempDataPath, ".mp4");

            var arguments = inputFileArguments + videoCodec + "\"" + outputFile + "\"";

            error = Call(arguments).Result.error;
        }
    }
}
