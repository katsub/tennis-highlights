using Microsoft.VisualBasic.Devices;
using OpenCvSharp;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;

namespace TennisHighlights
{
    /// <summary>
    /// The settings keys
    /// </summary>
    public class SettingsKeys
    {
        public const string Value = "Value";

        public const string GeneralSettings = "GeneralSettings";
        public const string RallyBuildingSettings = "RallyBuildingSettings";
        public const string BallDetectionSettings = "BallDetectionSettings";
        public const string BackgroundExtractionSettings = "BackgroundExtractionSettings";

        public const string LowMemoryMode = "LowMemoryMode";
        public const string AnalysedVideoPath = "AnalysedVideoPath";
        public const string DrawGizmos = "DrawGizmos";
        public const string RegenerateFrames = "RegenerateFrames";
        public const string TempDataPath = "TempDataPath";
        public const string ExtractedFrames = "ExtractedFrames";
        public const string InitialFrame = "InitialFrame";
        public const string FilterRalliesByDuration = "FilterRalliesByDuration";
        public const string UseCustomStopFrame = "UseCustomStopFrame";
        public const string CustomStopMinute = "CustomStopMinute";
        public const string UseCustomStartFrame = "UseCustomStartFrame";
        public const string CustomStartMinute = "CustomStartMinute";
        public const string DisableImagePreview = "DisableImagePreview";
        public const string AutoJoinAll = "AutoChooseRallies";
        public const string FFmpegPath = "FFmpegPath";
        public const string BallExtractionWorkers = "BallExtractionWorkers";
        public const string FrameMaxHeight = "FrameMaxHeight";
        public const string FrameExtractionWorkers = "FrameExtractionWorkers";
        public const string CopyNonKeyframes = "CopyNonKeyframes";
        public const string MaxVideoBitrate = "MaxVideoBitrate";
        public const string LimitMaxVideoBitrate = "LimitMaxVideoBitrate";
        public const string PreciseTrimming = "PreciseTrimming";
    }

    /// <summary>
    /// The general settings
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether [precise trimming].
        /// </summary>
        public bool PreciseTrimming { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [limit maximum video bitrate].
        /// </summary>
        public bool LimitMaxVideoBitrate { get; set; }
        /// <summary>
        /// Gets or sets the maximum video bitrate.
        /// </summary>
        public int MaxVideoBitrate { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [copy non keyframes].
        /// </summary>
        public bool CopyNonKeyframes { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [disable image preview].
        /// </summary>
        public bool DisableImagePreview { get; set; }
        /// <summary>
        /// The custom start frame
        /// </summary>
        public int CustomStartMinute { get; set; }
        /// <summary>
        /// The custom stop frame
        /// </summary>
        public int CustomStopMinute { get; set; }
        /// <summary>
        /// The use custom start frame
        /// </summary>
        public bool UseCustomStartFrame { get; set; }
        /// <summary>
        /// The use custom stop frame
        /// </summary>
        public bool UseCustomStopFrame { get; set; }
        /// <summary>
        /// Gets the frames per backup.
        /// </summary>
        public int FramesPerBackup { get; } = 3000;
        /// <summary>
        /// Gets a value indicating whether [filter rallies by duration].
        /// </summary>
        public bool FilterRalliesByDuration { get; set; } = true;
        /// <summary>
        /// Gets a value indicating whether [draw gizmos].
        /// </summary>
        public bool DrawGizmos { get; }
        /// <summary>
        /// Gets a value indicating whether [low memory mode].
        /// </summary>
        public bool LowMemoryMode { get; set; }
        /// <summary>
        /// Gets a value indicating whether [automatic join all].
        /// </summary>
        public bool AutoJoinAll { get; set; } = false;
        /// <summary>
        /// The number of frame extraction workers
        /// </summary>
        public static int FrameExtractionWorkers { get; set; }
        /// <summary>
        /// Gets the number of ball extaction workers.
        /// </summary> 
        public int BallExtractionWorkers { get; }
        /// <summary>
        /// Gets the maximum height of frame used in the video analysis. All frames will be resized to this height unless they have equal or 
        /// smaller height.
        /// 720 seems like a good value from personal tests, 480 gives some detection errors and 1080 is too slow.
        /// </summary>
        public int FrameMaxHeight { get; }
        /// <summary>
        /// Gets the analysed video path
        /// </summary>
        public string AnalysedVideoPath { get; set; }
        /// <summary>
        /// Gets the temp data path
        /// </summary>
        public string TempDataPath { get; set; }
        /// <summary>
        /// Gets the ffmpeg path
        /// </summary>
        public string FFmpegPath { get; set; }
        /// <summary>
        /// Gets the seconds before rally.
        /// </summary>
        public double SecondsBeforeRally { get; } = 3d;
        /// <summary>
        /// Gets the seconds after rally.
        /// </summary>
        public double SecondsAfterRally { get; } = 2d;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralSettings"/> class.
        /// </summary>
        /// <param name="xmlElement">The serialized settings.</param>
        public GeneralSettings(XElement serializedSettings = null)
        {
            var generalSettings = serializedSettings?.Element(SettingsKeys.GeneralSettings);

            //We create fake settings just to initialize with the default value
            if (generalSettings == null) { generalSettings = new XElement("dummySettings"); }

            AnalysedVideoPath = generalSettings.GetStringElementValue(SettingsKeys.AnalysedVideoPath);

            if (!File.Exists(AnalysedVideoPath))
            {
                AnalysedVideoPath = "";
            }

            var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 3))) + 0.5);
          
            LowMemoryMode = generalSettings.GetBoolElementValue(SettingsKeys.LowMemoryMode, totalGBRam < 7);
            DrawGizmos = generalSettings.GetBoolElementValue(SettingsKeys.DrawGizmos, false);
            FilterRalliesByDuration = generalSettings.GetBoolElementValue(SettingsKeys.FilterRalliesByDuration, true);
            CustomStartMinute = generalSettings.GetIntElementValue(SettingsKeys.CustomStartMinute, 0);
            CustomStopMinute = generalSettings.GetIntElementValue(SettingsKeys.CustomStopMinute, 5);
            UseCustomStartFrame = generalSettings.GetBoolElementValue(SettingsKeys.UseCustomStartFrame, false);
            UseCustomStopFrame = generalSettings.GetBoolElementValue(SettingsKeys.UseCustomStopFrame, true);
            DisableImagePreview = generalSettings.GetBoolElementValue(SettingsKeys.DisableImagePreview, true);
            AutoJoinAll = generalSettings.GetBoolElementValue(SettingsKeys.AutoJoinAll, true);
            FrameMaxHeight = generalSettings.GetIntElementValue(SettingsKeys.FrameMaxHeight, 720);
            BallExtractionWorkers = generalSettings.GetIntElementValue(SettingsKeys.BallExtractionWorkers, 20);
            FrameExtractionWorkers = generalSettings.GetIntElementValue(SettingsKeys.FrameExtractionWorkers, 10);
            CopyNonKeyframes = generalSettings.GetBoolElementValue(SettingsKeys.CopyNonKeyframes, false);
            TempDataPath = generalSettings.GetStringElementValue(SettingsKeys.TempDataPath);
            LimitMaxVideoBitrate = generalSettings.GetBoolElementValue(SettingsKeys.LimitMaxVideoBitrate);
            MaxVideoBitrate = generalSettings.GetIntElementValue(SettingsKeys.MaxVideoBitrate, 2);
            FFmpegPath = generalSettings.GetStringElementValue(SettingsKeys.FFmpegPath);
            PreciseTrimming = generalSettings.GetBoolElementValue(SettingsKeys.PreciseTrimming, true);

            if (!File.Exists(FFmpegPath))
            {
                FFmpegPath = string.Empty;
            }

            FFmpegCaller.FFmpegPath = FFmpegPath;

            if (!Directory.Exists(TempDataPath))
            {
                TempDataPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Output";
            }

            if (TempDataPath.EndsWith("\\")) { TempDataPath = TempDataPath.Substring(0, TempDataPath.Length - 2); }
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public XElement Serialize()
        {
            var xElement = new XElement(SettingsKeys.GeneralSettings);

            xElement.AddElementWithValue(SettingsKeys.AnalysedVideoPath, AnalysedVideoPath);
            xElement.AddElementWithValue(SettingsKeys.DrawGizmos, DrawGizmos);
            xElement.AddElementWithValue(SettingsKeys.TempDataPath, TempDataPath);
            xElement.AddElementWithValue(SettingsKeys.FilterRalliesByDuration, FilterRalliesByDuration);
            xElement.AddElementWithValue(SettingsKeys.CustomStartMinute, CustomStartMinute);
            xElement.AddElementWithValue(SettingsKeys.CustomStopMinute, CustomStopMinute);
            xElement.AddElementWithValue(SettingsKeys.UseCustomStartFrame, UseCustomStartFrame);
            xElement.AddElementWithValue(SettingsKeys.UseCustomStopFrame, UseCustomStopFrame);
            xElement.AddElementWithValue(SettingsKeys.DisableImagePreview, DisableImagePreview);
            xElement.AddElementWithValue(SettingsKeys.AutoJoinAll, AutoJoinAll);
            xElement.AddElementWithValue(SettingsKeys.FFmpegPath, FFmpegPath);
            xElement.AddElementWithValue(SettingsKeys.BallExtractionWorkers, BallExtractionWorkers);
            xElement.AddElementWithValue(SettingsKeys.FrameExtractionWorkers, FrameExtractionWorkers);
            xElement.AddElementWithValue(SettingsKeys.FrameMaxHeight, FrameMaxHeight);
            xElement.AddElementWithValue(SettingsKeys.CopyNonKeyframes, CopyNonKeyframes);
            xElement.AddElementWithValue(SettingsKeys.MaxVideoBitrate, MaxVideoBitrate);
            xElement.AddElementWithValue(SettingsKeys.LimitMaxVideoBitrate, LimitMaxVideoBitrate);
            xElement.AddElementWithValue(SettingsKeys.PreciseTrimming, PreciseTrimming);
            xElement.AddElementWithValue(SettingsKeys.LowMemoryMode, LowMemoryMode);

            return xElement;
        }

        /// <summary>
        /// Gets the first frame to process.
        /// </summary>
        /// <param name="info">The information.</param>
        public int GetFirstFrameToProcess(VideoInfo info) => UseCustomStartFrame ? (int)(CustomStartMinute * 60 * info.FrameRate)
                                                                                 : 0;

        /// <summary>
        /// Gets the final frame to process.
        /// </summary>
        /// <param name="info">The information.</param>
        public int GetFinalFrameToProcess(VideoInfo info) => UseCustomStopFrame ? Math.Min((int)(CustomStopMinute * 60 * info.FrameRate), info.TotalFrames)
                                                                                : info.TotalFrames;

        /// <summary>
        /// Gets the size of the target.
        /// </summary>
        /// <param name="info">The information.</param>
        public Size GetTargetSize(VideoInfo info)
        {
            var height = (int)Math.Round((double)Math.Min(info.Height, FrameMaxHeight));
            var width = (int)Math.Round(height * info.Width / (double)info.Height);

            return new Size(width, height);
        }
    }

    /// <summary>
    /// The rally building settings
    /// </summary>
    public class RallyBuildingSettings
    {
        /// <summary>
        /// The maximum undetected frames (frames without a ball being added to the rally (an arc beginning with that ball))
        /// </summary>
        public int MaxUndetectedFrames { get; } = 75;
        /// <summary>
        /// Gets the maximum undetected frames for long range (the number of frames without a ball being added) During these frames, arcs can be linked
        /// through a longer range, this allower linking of arcs which are covered by the closer player. This must not be too big or unrelated arcs of
        /// different rallies might get linked.
        /// </summary>
        public int MaxUndetectedFramesForLongRange { get; } = 45;
        /// <summary>
        /// The long range maximum distance. If arcs are separated by a number of frames smaller than max undetected frames for long range, and the last ball
        /// of the first arc and the first ball of the second arc are separated by a distance smaller than max long range square distance, then the arcs
        /// will be linked
        /// </summary>
        public ResolutionDependentParameter MaxLongRangeSquaredDistance { get; } = new ResolutionDependentParameter(70000, 2d);
        /// <summary>
        /// Gets the maximum position delta. Similar to MaxLongRangeSquaredDistance but valid through max undetected frames, which is longer than max 
        /// undetected frames for long range
        /// </summary>
        public ResolutionDependentParameter MaxSquaredDistance { get; } = new ResolutionDependentParameter(8000, 2d);
    }

    /// <summary>
    /// The ball detection settings
    /// </summary>
    public class BallDetectionSettings
    {
        /// <summary>
        /// Gets the minimum brightness for a pixel to appear in the binary frames
        /// </summary>
        public int MinBrightness { get; } = 30;
        /// <summary>
        /// Gets the minimum player area for a blob to be considered a player
        /// </summary>
        public ResolutionDependentParameter MinPlayerArea { get; } = new ResolutionDependentParameter(1050, 2d);
    }

    /// <summary>
    /// The background extraction settings
    /// </summary>
    public class BackgroundExtractionSettings
    {
        //WARNING: the algorithm uses a stackalloc of NumberOfSamples * ClusteringSize * ClusteringSize * 3 of floats, so these values must not
        //be increased without careful consideration
        /// <summary>
        /// Gets the frames skipped per sample.
        /// </summary>
        public int FramesPerSample { get; } = 15;
        /// <summary>
        /// Gets the number of samples used for the background reconstruction.
        /// </summary>
        public int NumberOfSamples { get; } = 10;
        /// <summary>
        /// Gets the size of the cluster.
        /// </summary>
        public int ClusteringSize { get; } = 5;
    }

    /// <summary>
    /// The tennis highlights settings
    /// </summary>
    public class TennisHighlightsSettings
    {
        /// <summary>
        /// The settings file name
        /// </summary>
        private const string _settingsFileName = "settings.xml";

        /// <summary>
        /// The general
        /// </summary>
        public GeneralSettings General { get; set; }
        /// <summary>
        /// The ball detection
        /// </summary>
        public BallDetectionSettings BallDetection = new BallDetectionSettings();
        /// <summary>
        /// The background extraction
        /// </summary>
        public BackgroundExtractionSettings BackgroundExtraction = new BackgroundExtractionSettings();
        /// <summary>
        /// The rally building settings
        /// </summary>
        public RallyBuildingSettings RallyBuildingSettings = new RallyBuildingSettings();

        /// <summary>
        /// Gets the application folder path.
        /// </summary>
        public string AppFolderPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";

        /// <summary>
        /// The document
        /// </summary>
        private readonly XDocument _document;
        /// <summary>
        /// The document path
        /// </summary>
        private string _documentPath => AppFolderPath + _settingsFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TennisHighlightsSettings" /> class.
        /// </summary>
        /// <param name="serializedSettings">The serialized settings.</param>
        public TennisHighlightsSettings(XDocument serializedSettings = null)
        {
            try
            {
                _document = serializedSettings ?? XDocument.Load(_documentPath);

                General = new GeneralSettings(_document.Root);
            }
            catch (Exception e)
            {
                if (_document == null) { _document = new XDocument(); }

                if (General == null) { General = new GeneralSettings(); }

                Logger.Log(LogType.Error, "Problem loading settings: " + e.ToString());
            }
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public XDocument Serialize()
        {
            var serializedSettingsRoot = new XDocument();

            var serializedSettings = new XElement("TennisHighlightsSettings");
            serializedSettingsRoot.Add(serializedSettings);
            serializedSettings.Add(General.Serialize());

            return serializedSettingsRoot;
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            try
            {
                Serialize().Save(_documentPath);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Could not save document: " + e.ToString());
            }
        }
    }
}
