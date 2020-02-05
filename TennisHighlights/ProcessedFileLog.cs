using Accord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

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
        public const string RotationDegrees = "RotationDegrees";
        public const string Saturation = "Saturation";
        public const string Brightness = "Brightness";
        public const string Constrast = "Constrast";
        public const string WarmColor = "WarmColor";
        public const string ToneColor = "ToneColor";
        public const string UseColorCorrection = "UseColorCorrection";
        public const string BallLog = "BallLog";
        public const string RallyLog = "RallyLog";
        public const string Rally = "Rally";
        public const string Signature = "Signature";
        public const string LastParsedFrame = "LastParsedFrame";
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
        /// Gets the rotation degrees.
        /// </summary>
        public int RotationDegrees { get; set; }
        /// <summary>
        /// The color correction settings
        /// </summary>
        public ColorCorrectionSettings CCSettings { get; } = new ColorCorrectionSettings();
        /// <summary>
        /// True if color correction should be used, false otherwise.
        /// </summary>
        public bool UseColorCorrection { get; set; }
        /// <summary>
        /// Gets or sets the last parsed frame.
        /// </summary>
        public int LastParsedFrame { get; set; }

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

                    RotationDegrees = fileInfo.GetIntElementValue(LogKeys.RotationDegrees);
                    CCSettings.Saturation = fileInfo.GetIntElementValue(LogKeys.Saturation);
                    CCSettings.Brightness = fileInfo.GetIntElementValue(LogKeys.Brightness);
                    CCSettings.Contrast = fileInfo.GetIntElementValue(LogKeys.Constrast);
                    CCSettings.WarmColor = fileInfo.GetIntElementValue(LogKeys.WarmColor);
                    CCSettings.ToneColor = fileInfo.GetIntElementValue(LogKeys.ToneColor);
                    UseColorCorrection = fileInfo.GetBoolElementValue(LogKeys.UseColorCorrection);
                }

                var ballLog = document.Root.Element(LogKeys.BallLog);

                if (ballLog != null)
                {
                    LastParsedFrame = ballLog.GetIntAttribute(LogKeys.LastParsedFrame, 0);

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
        /// Parses the rallies.
        /// </summary>
        /// <param name="settings">The settings</param>
        /// <param name="videoInfo">The video info</param>
        /// <param name="rallyProgressUpdateAction">The rally progress update action</param>
        /// <param name="cancelRequested">True if cancelling was requested</param>
        public void ParseRallies(TennisHighlightsSettings settings, VideoInfo videoInfo,
                                 Action<int, int> rallyProgressUpdateAction = null, Func<bool> cancelRequested = null)
        {
            var rallies = TennisHighlightsEngine.GetRalliesFromBalls(settings, this, rallyProgressUpdateAction, cancelRequested);

            if (settings.General.FilterRalliesByDuration)
            {
                rallies = TennisHighlights.Rallies.RallyFilter.FilterRalliesByDuration(rallies);
            }

            Rallies.Clear();

            var i = 0;
            foreach (var rally in rallies)
            {
                var rallyStart = (int)Math.Max(0, rally.FirstBall.FrameIndex - settings.General.SecondsBeforeRally
                                                                               * videoInfo.FrameRate);

                var rallyEnd = (int)Math.Min(videoInfo.TotalFrames, rally.LastBall.FrameIndex + settings.General.SecondsAfterRally
                                                                                                * videoInfo.FrameRate);

                Rallies.Add(new RallyEditData(i.ToString()) { Start = rallyStart, Stop = rallyEnd });
                i++;
            }

            Save();
        }

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
        /// Gets the or create processed file log
        /// </summary>
        /// <param name="generalSettings">The general settings</param>
        public static ProcessedFileLog GetOrCreateProcessedFileLog(GeneralSettings settings)
        {
            return GetOrCreateProcessedFileLog(settings.AnalysedVideoPath);
        }

        /// <summary>
        /// Gets the or create processed file log
        /// </summary>
        /// <param name="generalSettings">The general settings</param>
        /// <param name="analysedVideoPath">The analysed video path</param>
        public static ProcessedFileLog GetOrCreateProcessedFileLog(string analysedVideoPath)
        {
            if (File.Exists(analysedVideoPath))
            {
                var currentFileSignature = GetFileSignatureFromFile(analysedVideoPath);

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
                    var logPath = FileManager.GetUnusedFilePathInFolderFromFileName(analysedVideoPath,
                                                                                    FileManager.PersistentDataPath + _processedFileLogCacheFolder, ".xml");

                    return new ProcessedFileLog(analysedVideoPath, logPath);
                }

                return new ProcessedFileLog(processedFileLogPath);
            }

            return null;
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            //Since multiple windows modify that file, we need to check if the file existing on disk is more or less advanced than the one we're
            //saving. i.e. main window had an empty log but wants to save it since it's changing the selected file, but multiple window had just
            //saved a fully converted log, which will be erased if we don't do that check
            if (File.Exists(_logPath))
            {
                var existingFileLastParsedFrame = new ProcessedFileLog(_logPath).LastParsedFrame;

                //If existing file has more converted frames, keep it
                if (LastParsedFrame < existingFileLastParsedFrame)
                {
                    return;
                }
            }

            try
            {
                File.WriteAllText(_logPath, Serialize());
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Could not save processed file log: " + e.ToString());
            }
        }

        /// <summary>
        /// Saves the rotation.
        /// </summary>
        public void SaveRotation()
        {
            var log = new ProcessedFileLog(_logPath);

            log.RotationDegrees = RotationDegrees;

            log.Save();
        }

        /// <summary>
        /// Saves the color settings
        /// </summary>
        public void SaveColorSettings()
        {
            var log = new ProcessedFileLog(_logPath);

            log.CCSettings.Brightness = CCSettings.Brightness;
            log.CCSettings.Saturation = CCSettings.Saturation;
            log.CCSettings.Contrast = CCSettings.Contrast;
            log.CCSettings.WarmColor = CCSettings.WarmColor;
            log.CCSettings.ToneColor = CCSettings.ToneColor;
            log.UseColorCorrection = UseColorCorrection;

            log.Save();
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
            xFileInfo.AddElementWithValue(LogKeys.RotationDegrees, RotationDegrees);
            xFileInfo.AddElementWithValue(LogKeys.Brightness, CCSettings.Brightness);
            xFileInfo.AddElementWithValue(LogKeys.Constrast, CCSettings.Contrast);
            xFileInfo.AddElementWithValue(LogKeys.Saturation, CCSettings.Saturation);
            xFileInfo.AddElementWithValue(LogKeys.WarmColor, CCSettings.WarmColor);
            xFileInfo.AddElementWithValue(LogKeys.ToneColor, CCSettings.ToneColor);
            xFileInfo.AddElementWithValue(LogKeys.UseColorCorrection, UseColorCorrection);

            var xBallLog = new XElement(LogKeys.BallLog, FrameDataSerializer.SerializeBallsPerFrameIntoString(Balls), 
                                                         new XAttribute(LogKeys.LastParsedFrame, LastParsedFrame));
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
                Logger.Log(LogType.Error, "Could not parse file signature: " + e.ToString());
            }

            return null;
        }
    }
}
