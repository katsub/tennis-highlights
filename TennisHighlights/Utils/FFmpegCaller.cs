using System;
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

            if (!Settings.CopyNonKeyframes && Settings.RotationAngles == 0)
            {
                arguments += " -c:v copy ";
            }
            else
            {
                if (Settings.CopyNonKeyframes)
                {
                    //copyinkf is needed so the trimmed video won't be stuck because it was trimmed in section where there was no keyframe
                    arguments += " -copyinkf ";
                }
                if (Settings.RotationAngles != 0)
                {
                    var cropRect = CropRotationHelper.GetCropCoordinates(Settings.RotationAngles, new OpenCvSharp.Rect(0, 0, VideoInfo.Width, VideoInfo.Height));

                    var vflip = Settings.RotationAngles > 90 ? "vflip," : string.Empty;

                    arguments += " -vf \"" + vflip + "rotate=-" + Settings.RotationAngles + "*PI/180, crop=" + cropRect.Width + ":" + cropRect.Height + ":" + cropRect.X + ":" + cropRect.Y + "\" ";
                }
            }

            arguments += " " + fileName + " ";

            var taskResult = Call(arguments, askedToStop).Result;

            error = taskResult.error;

            return taskResult.success;
        }

        /// <summary>
        /// Joins all rally videos.
        /// </summary>
        /// <param name="resultFilePath">The result file path.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static void JoinAllRallyVideos(string resultFilePath, out string error, Func<bool> askedToStop = null)
        {
            var rallyFolderPath = FileManager.TempDataPath + FileManager.RallyVideosFolder + "\\";
            var rallyFilePath = rallyFolderPath + "rallies.txt";

            var ralliesPaths = new StringBuilder();

            foreach (var rally in Directory.GetFiles(rallyFolderPath).Where(f => f.EndsWith(".mp4")).OrderBy(b => b))
            {
                ralliesPaths.AppendLine("file '" + rally + "'");
            }

            var limitBitrateConfig = Settings.LimitMaxVideoBitrate && Settings.MaxVideoBitrate > 0 ? " -b:v " + Settings.MaxVideoBitrate + "M"
                                                                                                   : string.Empty;

            var videoCodec = string.IsNullOrEmpty(limitBitrateConfig) ? " -c:v copy " : limitBitrateConfig;

            File.WriteAllText(rallyFilePath, ralliesPaths.ToString());

            var arguments = "-f concat -safe 0 -i " + rallyFilePath + videoCodec + " -c:a copy " + "\"" + resultFilePath + "\"";

            error = Call(arguments, askedToStop).Result.error;
        }
    }
}
