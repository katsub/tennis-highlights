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
    public class Logger
    {
        /// <summary>
        /// The log path
        /// </summary>
        private readonly string _logPath; 

        /// <summary>
        /// The instance
        /// </summary>
        public static Logger Instance { get; private set; }

        /// <summary>
        /// Initializes the <see cref="Logger"/> class.
        /// </summary>
        static Logger() => Instance = new Logger();

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        private Logger() => _logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt";

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="LogType">Type of the log.</param>
        public void Log(LogType type, string message)
        {
            var formattedMessage = $"[{DateTime.Now}][{type}]: {message}";

            using (var writer = File.AppendText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log.txt"))
            {
                writer.WriteLine(formattedMessage);
            }

            Console.WriteLine(formattedMessage);
        }
    }
}
