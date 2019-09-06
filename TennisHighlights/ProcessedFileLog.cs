using Accord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace TennisHighlights
{
    /// <summary>
    /// The log serialization keys
    /// </summary>
    public class LogKeys
    {
        public const string FileInfo = "FileInfo";
        public const string Name = "Name";
        public const string Size = "Size";
        public const string ModifiedDate = "ModifiedDate";
        public const string BallLog = "BallLog";
        public const string RallyLog = "RallyLog";
        public const string Rally = "Rally";
        public const string Signature = "Signature";
    }

    /// <summary>
    /// The processed file log (balls, rallies, etc)
    /// </summary>
    public class ProcessedFileLog
    {
        /// <summary>
        /// The processed file log cache folder
        /// </summary>
        private const string _processedFileLogCacheFolder = "ProcessedFileLogCache\\";

        /// <summary>
        /// The file name
        /// </summary>
        private readonly string _name;
        /// <summary>
        /// The size
        /// </summary>
        private readonly long? _size;
        /// <summary>
        /// The modified date
        /// </summary>
        private readonly DateTime? _modifiedDate;
        /// <summary>
        /// The log path
        /// </summary>
        private readonly string _logPath;

        /// <summary>
        /// Gets the signature.
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Gets the balls.
        /// </summary>
        public Dictionary<int, List<Point>> Balls { get; } = new Dictionary<int, List<Point>>();

        /// <summary>
        /// Gets the rallies.
        /// </summary>
        public List<RallyEditData> Rallies { get; } = new List<RallyEditData>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessedFileLog" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="logPath">The log path.</param>
        /// <param name="save">if set to <c>true</c> [save].</param>
        private ProcessedFileLog(string filePath, string logPath, bool save = true)
        {
            var file = new FileInfo(filePath);

            _name = file.Name;
            _size = file.Length;
            _modifiedDate = file.LastWriteTime;

            Signature = GetFileSignature(_name.ToString(), _size.ToString(), _modifiedDate.ToString());

            _logPath = logPath;

            if (save)
            {
                Save();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessedFileLog"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        private ProcessedFileLog(XDocument document)
        {
            if (document != null)
            {
                var fileInfo = document.Root.Element(LogKeys.FileInfo);

                if (fileInfo != null)
                {
                    if (long.TryParse(fileInfo.GetStringElementValue(LogKeys.Size), out var size))
                    {
                        _size = size;
                    }

                    _name = fileInfo.GetStringElementValue(LogKeys.Name);

                    if (DateTime.TryParse(fileInfo.GetStringElementValue(LogKeys.ModifiedDate), out var modifiedDate))
                    {
                        _modifiedDate = modifiedDate;
                    }

                    Signature = fileInfo.GetStringElementValue(LogKeys.Signature);
                }

                var ballLog = document.Root.Element(LogKeys.BallLog);

                if (ballLog != null)
                {
                    Balls = FrameDataSerializer.ParseBallLog(ballLog.Value);
                }

                var rallyLog = document.Root.Element(LogKeys.RallyLog);

                Rallies = new List<RallyEditData>();

                if (rallyLog != null)
                {
                    foreach (var rally in rallyLog.Elements(LogKeys.Rally))
                    {
                        Rallies.Add(new RallyEditData(rally));
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessedFileLog" /> class.
        /// </summary>
        /// <param name="logPath">The file path.</param>
        private ProcessedFileLog(string logPath) : this(XDocument.Load(logPath)) => _logPath = logPath;

        /// <summary>
        /// Clones this instance.
        /// </summary>
        public ProcessedFileLog Clone() => new ProcessedFileLog(XDocument.Parse(Serialize()));

        /// <summary>
        /// Reloads the balls from serialization.
        /// </summary>
        public void ReloadBallsFromSerialization()
        {
            Balls.Clear();

            var deserializedBalls = new ProcessedFileLog(_logPath).Balls;

            foreach (var ball in deserializedBalls)
            {
                Balls.Add(ball.Key, ball.Value);
            }
        }

        /// <summary>
        /// Gets the or create processed file log.
        /// </summary>
        /// <param name="generalSettings">The general settings.</param>
        public static ProcessedFileLog GetOrCreateProcessedFileLog(GeneralSettings generalSettings)
        {
            var currentFileSignature = GetFileSignatureFromFile(generalSettings.AnalysedVideoPath);

            string processedFileLogPath = null;

            var logFolder = FileManager.PersistentDataPath + _processedFileLogCacheFolder;

            if (!Directory.Exists(logFolder)) { Directory.CreateDirectory(logFolder); }

            foreach (var file in Directory.GetFiles(logFolder))
            {
                if (GetFileSignatureFromLogFile(file) == currentFileSignature)
                {
                    processedFileLogPath = file;
                }
            }

            if (processedFileLogPath == null)
            {
                var logPath = FileManager.GetUnusedFilePathInFolderFromFileName(generalSettings.AnalysedVideoPath,
                                                                                FileManager.PersistentDataPath + _processedFileLogCacheFolder, ".xml");

                return new ProcessedFileLog(generalSettings.AnalysedVideoPath, logPath);
            }

            return new ProcessedFileLog(processedFileLogPath);
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            try
            {
                File.WriteAllText(_logPath, Serialize());
            }
            catch (Exception e)
            {
                Logger.Instance.Log(LogType.Error, "Could not save processed file log: " + e.ToString());
            }
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        private string Serialize()
        {
            var document = new XDocument();

            var rootElement = new XElement("ProcessedFileLog");

            var xFileInfo = new XElement(LogKeys.FileInfo);

            xFileInfo.AddElementWithValue(LogKeys.Name, _name);
            xFileInfo.AddElementWithValue(LogKeys.Size, _size);
            xFileInfo.AddElementWithValue(LogKeys.ModifiedDate, _modifiedDate);
            xFileInfo.AddElementWithValue(LogKeys.Signature, Signature);

            var xBallLog = new XElement(LogKeys.BallLog, FrameDataSerializer.SerializeBallsPerFrameIntoString(Balls));
            var xRallyLog = new XElement(LogKeys.RallyLog);

            foreach (var rally in Rallies)
            {
                xRallyLog.Add(rally.Serialize());
            }

            rootElement.Add(xFileInfo);
            rootElement.Add(xBallLog);
            rootElement.Add(xRallyLog);

            document.Add(rootElement);

            return document.ToString();
        }

        /// <summary>
        /// Gets the file signature.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="size">The size.</param>
        /// <param name="modifiedDate">The modified date.</param>
        private static string GetFileSignature(string name, string size, string modifiedDate)
        {
            if (name != null && size != null && modifiedDate != null)
            {
                return name + "_" + size + "_" + modifiedDate;
            }

            return null;
        }

        /// <summary>
        /// Gets the file signature from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public static string GetFileSignatureFromFile(string filePath) => new ProcessedFileLog(filePath, "", false).Signature;

        /// <summary>
        /// Gets the file signature from log file.
        /// </summary>
        /// <param name="logPath">The log path.</param>
        public static string GetFileSignatureFromLogFile(string logPath)
        {
            try
            {
                var document = XDocument.Load(logPath);

                if (document != null)
                {
                    var fileInfo = document.Root.Element(LogKeys.FileInfo);

                    if (fileInfo != null)
                    {
                        return fileInfo.GetStringElementValue(LogKeys.Signature);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Instance.Log(LogType.Error, "Could not parse file signature: " + e.ToString());
            }

            return null;
        }
    }
}
