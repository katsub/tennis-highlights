using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TennisHighlights.Utils
{
    /// <summary>
    /// The log types
    /// </summary>
    public enum LogType
    {
        Information,
        Warning,
        Error
    }

    /// <summary>
    /// The logger
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The log lock
        /// </summary>
        private static object _logLock = new object();
        /// <summary>
        /// The log path
        /// </summary>
        private static readonly string _logPath;
        /// <summary>
        /// Initializes the <see cref="Logger"/> class.
        /// </summary>
        static Logger()
        {
            _logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";

            TrimLog();
        }

        /// <summary>
        /// Trims the log.
        /// </summary>
        public static void TrimLog()
        {
            var iMaxLogLength = 10000; // Probably should be bigger, say 200,000
            var keepLines = 5000; // minimum of how much of the old log to leave

            try
            {
                var fi = new FileInfo(_logPath);
                if (fi.Length > iMaxLogLength) // if the log file length is already too long
                {
                    var totalLines = 0;
                    var file = File.ReadAllLines(_logPath);
                    var lineArray = file.ToList();
                    var amountToCull = (int)(lineArray.Count - keepLines);
                    var trimmed = lineArray.Skip(amountToCull).ToList();
                    File.WriteAllLines(_logPath, trimmed);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to write to logfile : " + ex.Message);
            }
        }

        /// <summary>
        /// Logs the specified message. Has lock because it should be used sparingly in the first place, so performance isn't an issue
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="LogType">Type of the log.</param>
        public static void Log(LogType type, string message)
        {
            var formattedMessage = $"[{DateTime.Now}][{type}]: {message}";

            lock (_logLock)
            {
                using (var writer = File.AppendText(_logPath))
                {
                    writer.WriteLine(formattedMessage);
                }
            }

            //Useful if using a command-line version
            Console.WriteLine(formattedMessage);
        }
    }
}
