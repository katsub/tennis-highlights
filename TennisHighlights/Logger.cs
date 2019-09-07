using System;
using System.IO;
using System.Reflection;

namespace TennisHighlights
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
        /// The log path
        /// </summary>
        private static readonly string _logPath; 
        /// <summary>
        /// Initializes the <see cref="Logger"/> class.
        /// </summary>
        static Logger() => _logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="LogType">Type of the log.</param>
        public static void Log(LogType type, string message)
        {
            var formattedMessage = $"[{DateTime.Now}][{type}]: {message}";

            using (var writer = File.AppendText(_logPath))
            {
                writer.WriteLine(formattedMessage);
            }

            //Useful if using a command-line version
            Console.WriteLine(formattedMessage);
        }
    }
}
