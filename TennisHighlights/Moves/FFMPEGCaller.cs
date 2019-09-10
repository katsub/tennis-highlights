using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TennisHighlights.Utils;

namespace TennisHighlights
{
    /// <summary>
    /// The FFMPEG caller
    /// </summary>
    public static class FFMPEGCaller
    {
        /// <summary>
        /// The FFmpeg path
        /// </summary>
        public static string FFmpegPath { get; set; }

        /// <summary>
        /// Calls FFMPEG with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="askedToStop">The asked to stop.</param>
        public static bool Call(string arguments, out string error, Func<bool> askedToStop = null)
        {
            error = null;

            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = FFmpegPath;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                Task.Run(() => KillProcessIfAskedToStop(proc, askedToStop));

                if (!proc.Start())
                {
                    Logger.Log(LogType.Error, "Could not start FFMPEG.");
                    return false;
                }

                var reader = proc.StandardError;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }

                proc.Close();

                return true;
            }
            catch (Exception e)
            {
                var message = "Could not call FFmpeg: " + e.ToString();

                error = message;

                Logger.Log(LogType.Error, message);

                return false;
            }
        }

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
                    if (askedToStop?.Invoke() == true)
                    {
                        proc.Kill();
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
            var fileName = FileManager.TempDataPath + FileManager.RallyVideosFolder + "//" + rallyIndex + ".mp4";

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

            var arguments = "-i " + originalFile;

            arguments += " -ss " + TimeSpan.FromSeconds(startSeconds);
            arguments += " -t " + TimeSpan.FromSeconds(stopSeconds - startSeconds);
            arguments += " -c:a copy -copyinkf " + fileName;

            return Call(arguments, out error, askedToStop);
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

            foreach (var rally in Directory.GetFiles(rallyFolderPath).Where(f => f.EndsWith(".mp4")))
            {
                ralliesPaths.AppendLine("file '" + rally + "'");
            }

            File.WriteAllText(rallyFilePath, ralliesPaths.ToString());

            var arguments = "-f concat -safe 0 -i " + rallyFilePath + " -c copy " + resultFilePath;

            Call(arguments, out error, askedToStop);
        }
    }
}
