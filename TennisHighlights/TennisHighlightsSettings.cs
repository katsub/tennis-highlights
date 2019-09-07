using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using TennisHighlights.ImageProcessing;

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
        public const string FrameExtractionWorkers = "FrameExtractionWorkers";
    }

    /// <summary>
    /// The general settings
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether [disable image preview].
        /// </summary>
        public bool DisableImagePreview { get; set; }
        /// <summary>
        /// The custom start frame
        /// </summary>
        public int CustomStartMinute { get; set; } = 0;
        /// <summary>
        /// The custom stop frame
        /// </summary>
        public int CustomStopMinute { get; set; } = 0;
        /// <summary>
        /// The use custom start frame
        /// </summary>
        public bool UseCustomStartFrame { get; set; } = false;
        /// <summary>
        /// The use custom stop frame
        /// </summary>
        public bool UseCustomStopFrame { get; set; } = false;
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
        /// Gets a value indicating whether [regenerate frames].
        /// </summary>
        public bool RegenerateFrames { get; }
        /// <summary>
        /// Gets a value indicating whether [automatic join all].
        /// </summary>
        public bool AutoJoinAll { get; set; } = false;
        /// <summary>
        /// The number of frame extraction workers
        /// </summary>
        public static int FrameExtractionWorkers = 10;// = 40;
        /// <summary>
        /// Gets the number of ball extaction workers.
        /// </summary> 
        public int BallExtractionWorkers { get; } = 20;// = 75;
        /// <summary>
        /// Gets the maximum height of the video analysis. All frames will be converted to this height for the analysis
        /// </summary>
        public int VideoAnalysisMaxHeight { get; } = 720;
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
            if (serializedSettings != null)
            {
                var generalSettings = serializedSettings.Element(SettingsKeys.GeneralSettings);

                if (generalSettings != null)
                {
                    AnalysedVideoPath = generalSettings.GetStringElementValue(SettingsKeys.AnalysedVideoPath);

                    if (!File.Exists(AnalysedVideoPath))
                    {
                        AnalysedVideoPath = "";
                    }

                    RegenerateFrames = generalSettings.GetBoolElementValue(SettingsKeys.RegenerateFrames, false);
                    DrawGizmos = generalSettings.GetBoolElementValue(SettingsKeys.DrawGizmos, false);
                    FilterRalliesByDuration = generalSettings.GetBoolElementValue(SettingsKeys.FilterRalliesByDuration, true);
                    CustomStartMinute = generalSettings.GetIntElementValue(SettingsKeys.CustomStartMinute, 0);
                    CustomStopMinute = generalSettings.GetIntElementValue(SettingsKeys.CustomStopMinute, 0);
                    UseCustomStartFrame = generalSettings.GetBoolElementValue(SettingsKeys.UseCustomStartFrame, false);
                    UseCustomStopFrame = generalSettings.GetBoolElementValue(SettingsKeys.UseCustomStopFrame, false);
                    DisableImagePreview = generalSettings.GetBoolElementValue(SettingsKeys.DisableImagePreview, false);
                    AutoJoinAll = generalSettings.GetBoolElementValue(SettingsKeys.AutoJoinAll, false);
                    BallExtractionWorkers = generalSettings.GetIntElementValue(SettingsKeys.BallExtractionWorkers, 20);
                    FrameExtractionWorkers = generalSettings.GetIntElementValue(SettingsKeys.FrameExtractionWorkers, 10);

                    TempDataPath = generalSettings.GetStringElementValue(SettingsKeys.TempDataPath);

                    if (!TempDataPath.EndsWith("\\")) { TempDataPath += "\\"; }

                    FFmpegPath = generalSettings.GetStringElementValue(SettingsKeys.FFmpegPath);

                    if (!File.Exists(FFmpegPath))
                    {
                        FFmpegPath = string.Empty;
                    }
                   
                    FFMPEGCaller.FFmpegPath = FFmpegPath;
                }
            }

            if (!Directory.Exists(TempDataPath))
            {
                TempDataPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Output\\";
            }
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public XElement Serialize()
        {
            var xElement = new XElement(SettingsKeys.GeneralSettings);

            xElement.AddElementWithValue(SettingsKeys.AnalysedVideoPath, AnalysedVideoPath);
            xElement.AddElementWithValue(SettingsKeys.RegenerateFrames, RegenerateFrames);
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
        public int MinBrightness { get; } = 30;
        public ResolutionDependentParameter MinPlayerArea { get; } = new ResolutionDependentParameter(1050, 2d);
    }

    public class BackgroundExtractionSettings
    {
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
        private XDocument _document;
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

                Logger.Instance.Log(LogType.Error, "Problem loading settings: " + e.ToString());
            }
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        public TennisHighlightsSettings Clone() => new TennisHighlightsSettings(Serialize());

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
                Logger.Instance.Log(LogType.Error, "Could not save document: " + e.ToString());
            }
        }
    }
}
