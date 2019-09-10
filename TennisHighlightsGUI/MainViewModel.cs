using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TennisHighlights;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Rallies;
using TennisHighlights.VideoCreation;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The main view model
    /// </summary>
    public class MainViewModel : VideoConversionScreenViewModel
    {
        /// <summary>
        /// The rally classifications file name
        /// </summary>
        private const string _rallyClassificationsFileName = "classifiedRallies.txt";

        /// <summary>
        /// The rally selection view model
        /// </summary>
        private RallySelectionViewModel _rallySelectionViewModel;
        /// <summary>
        /// The settings
        /// </summary>
        public TennisHighlightsSettings Settings { get; private set; }

        /// <summary>
        /// Gets the open file command.
        /// </summary>
        public Command OpenFileCommand { get; }
        /// <summary>
        /// Gets the choose output folder command.
        /// </summary>
        public Command ChooseOutputFolderCommand { get; }
        /// <summary>
        /// Gets the open output folder command.
        /// </summary>
        public Command OpenOutputFolderCommand { get; }
        /// <summary>
        /// Gets the open rally graph command.
        /// </summary>
        public Command OpenRallyGraphCommand { get; }
        /// <summary>
        /// Gets the regenerate rallies command.
        /// </summary>
        public Command RegenerateRalliesCommand { get; }

        private string _outputFolder;
        /// <summary>
        /// Gets or sets the output folder.
        /// </summary>
        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (_outputFolder != value)
                {
                    _outputFolder = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanConvert));
                }
            }
        }

        private string _chosenFile;
        /// <summary>
        /// Gets the chosen file.
        /// </summary>
        public string ChosenFile
        {
            get => _chosenFile;
            set
            {
                if (_chosenFile != value)
                {
                    _chosenFile = value;

                    VideoInfo = new VideoInfo(ChosenFile);

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanConvert));
                }
            }
        }

        private ProcessedFileLog _chosenFileLog;
        /// <summary>
        /// Gets or sets the chosen file log.
        /// </summary>
        public ProcessedFileLog ChosenFileLog
        {
            get => _chosenFileLog;
            set
            {
                if (_chosenFileLog != value)
                {
                    _chosenFileLog = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRegenerateRallies));
                    OnPropertyChanged(nameof(CanOpenRallyGraph));
                }
            }
        }

        /// <summary>
        /// Gets the video information.
        /// </summary>
        public VideoInfo VideoInfo { get; private set; }
        /// <summary>
        /// Gets a value indicating whether this instance can convert.
        /// </summary>
        public override bool CanConvert => !string.IsNullOrEmpty(ChosenFile) && !string.IsNullOrEmpty(OutputFolder);

        /// <summary>
        /// Gets a value indicating whether this instance can regenerate rallies.
        /// </summary>
        public bool CanRegenerateRallies => CanConvert && ChosenFileLog?.Balls?.Count > 500;

        /// <summary>
        /// Gets a value indicating whether this instance can open rally graph.
        /// </summary>
        public bool CanOpenRallyGraph => !string.IsNullOrEmpty(FileManager.ReadPersistentFile(_rallyClassificationsFileName)) && ChosenFileLog.Balls.Count > 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            InitializeSettings();

            FileManager.Initialize(Settings.General);

            LoadInitialPreviewImage();

            #region Commands
            RegenerateRalliesCommand = new Command((param) =>
            {
                RegenerateRallies();

                SwitchToRallySelectionView();
            });

            OpenRallyGraphCommand = new Command((param) =>
            {
                var rallyClassificationsFile = FileManager.ReadPersistentFile(_rallyClassificationsFileName);

                var rallies = TennisHighlightsEngine.GetRalliesFromBalls(Settings, ChosenFileLog.Balls, ChosenFileLog);

                var rallyData = new RallyClassificationData(rallyClassificationsFile, rallies);

                /*  var rallyGraphViewModel = new RallyGraphViewModel(rallyData);
                  var rallyGraphWindow = new RallyGraphWindow(rallyGraphViewModel);

                  rallyGraphWindow.Show();*/
            });

            OpenFileCommand = new Command((param) =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Video files (*.mp4) | *.mp4"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SetChosenFileAndLoadImage(openFileDialog.FileName);
                }
            });

            OpenOutputFolderCommand = new Command((param) =>
            {
                if (!string.IsNullOrEmpty(OutputFolder) && Directory.Exists(OutputFolder))
                {
                    Process.Start("explorer.exe", OutputFolder);
                }
            });

            ChooseOutputFolderCommand = new Command((param) =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Folder | *.folder",

                    // Set validate names and check file exists to false otherwise windows will
                    // not let you select "Folder Selection."
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,

                    // Always default to Folder Selection.
                    FileName = "Folder Selection."
                };

                if (dialog.ShowDialog() == true)
                {
                    OutputFolder = Path.GetDirectoryName(dialog.FileName);
                    Settings.General.TempDataPath = OutputFolder;

                    ChosenFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(Settings.General);
                }
            });
            #endregion

            if (Directory.Exists(Settings.General.TempDataPath))
            {
                OutputFolder = Settings.General.TempDataPath;
            }

            if (File.Exists(Settings.General.AnalysedVideoPath))
            {
                SetChosenFileAndLoadImage(Settings.General.AnalysedVideoPath);
            }
        }

        /// <summary>
        /// Converts the command.
        /// </summary>
        /// <param name="param">The parameter.</param>
        protected override void ConvertInternal(object param)
        {
            if (string.IsNullOrEmpty(ChosenFile) || string.IsNullOrEmpty(OutputFolder))
            {
                MessageBox.Show("Pick a video and an output folder before converting.", "Error");

                return;
            }

            Settings.Save();

            if (Settings.General.RegenerateFrames)
            {
                ChosenFileLog.Balls.Clear();
                ChosenFileLog.Rallies.Clear();
            }

            var framesToProcess = Settings.General.GetFinalFrameToProcess(VideoInfo);

            //Indices start at 0, so we must subtract 1 from the frames count to have the correct index
            if (ChosenFileLog.Balls.LastOrDefault().Key >= framesToProcess - 1)
            {
                if (!ChosenFileLog.Rallies.Any())
                {
                    RegenerateRallies();
                }

                SendProgressInfo("Parsed from cache", 100, 0d);
                SwitchToRallySelectionView();

                IsConverting = false;
                CancelRequestHandled();
            }
            else
            {
                //Begin conversion
                var stopwatch = new Stopwatch();

                stopwatch.Start();

                Task.Run(() =>
                {
                    try
                    {
                        void progressUpdateAction(Bitmap bitmap, int processedFrame)
                        {
                            var percent = 100d * (double)processedFrame / framesToProcess;

                            var remainingSeconds = percent > 0 ? (100d * stopwatch.Elapsed.TotalSeconds / percent - stopwatch.Elapsed.TotalSeconds) : 0d;

                            SendProgressInfo(new ProgressInfo(bitmap, (int)Math.Round(percent), "Reading frames (" + processedFrame + "/" + framesToProcess + ")", remainingSeconds));
                        }

                        bool checkIfCancelRequested() => RequestedCancel;

                        var ballsPerFrame = new VideoBallsExtractor(Settings.Clone(), VideoInfo, ChosenFileLog,
                                                                    checkIfCancelRequested, progressUpdateAction).GetBallsPerFrame().Result;

                        ChosenFileLog.Save();

                        if (!RequestedCancel)
                        {
                            RegenerateRallies();
                        }

                        if (!RequestedCancel)
                        {
                            if (ChosenFileLog.Rallies.Count > 0)
                            {
                                if (Settings.General.AutoJoinAll)
                                {
                                    foreach (var rally in ChosenFileLog.Rallies)
                                    {
                                        rally.IsSelected = true;
                                    }

                                    RallyVideoCreator.BuildVideoWithAllRallies(ChosenFileLog.Clone().Rallies,
                                                                               VideoInfo, Settings.General, out var error, SendProgressInfo, () => RequestedCancel);
                                }
                                else
                                {
                                    SwitchToRallySelectionView();
                                }
                            }
                            else
                            {
                                MessageBox.Show("No rallies were found in video.", "Error");
                            }
                        }

                        SendProgressInfo(new ProgressInfo(null, RequestedCancel ? 0 : 100, RequestedCancel ? "Canceled" : "Done", 0d));
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("An error has been encountered in the conversion:\n\n" + e.ToString());
                    }
                    finally
                    {
                        IsConverting = false;
                        CancelRequestHandled();
                        OnPropertyChanged(nameof(CanRegenerateRallies));
                    }
                });
            }
        }

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        private void InitializeSettings()
        {
            Settings = new TennisHighlightsSettings();
            //This creates an initial settings file that can be modified later if needed
            Settings.Save();

            if (string.IsNullOrEmpty(Settings.General.FFmpegPath))
            {
                Settings.General.FFmpegPath = Directory.GetFiles(Settings.AppFolderPath, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();

                if (string.IsNullOrEmpty(Settings.General.FFmpegPath))
                {
                    var result = MessageBox.Show("Could not find ffmpeg.exe path. Install FFmpeg then choose the executable file using the button below. You can also install it inside this program's folder so it'll be detected automatically. \n\n Would you like to choose the FFmpeg.exe file now?", "Error", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dialog = new OpenFileDialog
                        {
                            Filter = "FFmpeg executable | *.exe",
                        };

                        bool TryToFindFFmpeg()
                        {
                            if (dialog.ShowDialog() == true)
                            {
                                if (!dialog.FileName.ToLower().EndsWith("ffmpeg.exe"))
                                {
                                    var result2 = MessageBox.Show("This is not the ffmpeg.exe file. Would you like to pick it again?", "Error", MessageBoxButton.YesNo);

                                    return result2 == MessageBoxResult.Yes ? TryToFindFFmpeg() : false;
                                }
                                else
                                {
                                    Settings.General.FFmpegPath = dialog.FileName;

                                    return true;
                                }
                            }

                            return false;
                        }

                        TryToFindFFmpeg();
                    }
                }
            }

            FFMPEGCaller.FFmpegPath = Settings.General.FFmpegPath;
        }

        /// <summary>
        /// Regenerates the rallies.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void RegenerateRallies()
        {
            var clusterSize = 5;
            var rallies = TennisHighlightsEngine.GetRalliesFromBalls(Settings, ChosenFileLog.Balls, ChosenFileLog);

            if (rallies.Count > clusterSize)
            {
                if (Settings.General.FilterRalliesByDuration)
                {
                    var filter = RallyFilter.ScoreByDuration(rallies.ToDictionary(r => r, r => new RallyFilterInfo(rallies.IndexOf(r))), clusterSize);

                    var shortDurationClusters = new HashSet<int>(filter.clusters.Clusters.OrderBy(c => c.Centroid[0]).Take(2).Select(c => c.Index));

                    rallies = new List<Rally>(filter.rallies.Where(r => !shortDurationClusters.Contains(r.Value.DurationCluster)).Select(r => r.Key));
                }
            }

            ChosenFileLog.Rallies.Clear();

            var i = 0;
            foreach (var rally in rallies)
            {
                var rallyStart = (int)Math.Max(0, rally.FirstBall.FrameIndex - Settings.General.SecondsBeforeRally
                                                                               * VideoInfo.FrameRate);

                var rallyEnd = (int)Math.Min(VideoInfo.TotalFrames, rally.LastBall.FrameIndex + Settings.General.SecondsAfterRally
                                                                                                * VideoInfo.FrameRate);

                ChosenFileLog.Rallies.Add(new RallyEditData(i.ToString()) { Start = rallyStart, Stop = rallyEnd });
                i++;
            }
        }

        /// <summary>
        /// Switches to rally selection view.
        /// </summary>
        private void SwitchToRallySelectionView()
        {
            new Action(() =>
            {
                if (_rallySelectionViewModel == null)
                {
                    _rallySelectionViewModel = new RallySelectionViewModel(this);
                }

                _rallySelectionViewModel.InitializeVideoParameters();

                Switcher.Switch(new RallySelectionView(_rallySelectionViewModel));
            }).ExecuteOnUIThread();
        }

        /// <summary>
        /// Called when [closing].
        /// </summary>
        public void OnClosing()
        {
            Settings.Save();
            ChosenFileLog?.Save();
        }

        /// <summary>
        /// Sets the chosen file and load image.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void SetChosenFileAndLoadImage(string fileName)
        {
            //If there's a previous file loaded, we save its edit data before loading a new one
            if (ChosenFileLog != null)
            {
                ChosenFileLog.Save();
            }

            ChosenFile = fileName;
            Settings.General.AnalysedVideoPath = fileName;
            ChosenFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(Settings.General);

            SendProgressInfo("Not started", 0, 0d);

            try
            {
                using (var bitmap = GetVideoPreviewImage(ChosenFile))
                {
                    PreviewImage = WPFUtils.BitmapToImageSource(bitmap);
                }
            }
            catch (Exception e)
            {
                ChosenFile = string.Empty;

                LoadInitialPreviewImage();

                MessageBox.Show("Could not open video file. Error: " + e.ToString(), "Error");

                return;
            }
            finally
            {

                Settings.General.AnalysedVideoPath = ChosenFile;

                Settings.Save();
            }
        }

        /// <summary>
        /// Gets the video preview image.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private static Bitmap GetVideoPreviewImage(string filePath)
        {
            using (var video = new VideoCapture(filePath))
            using (var frame = new Mat())
            {
                video.Read(frame);

                return BitmapConverter.ToBitmap(frame);
            }
        }

        /// <summary>
        /// Loads the initial preview image.
        /// </summary>
        private void LoadInitialPreviewImage()
        {
            var initialPreviewImage = new Bitmap(1280, 720);

            using (Graphics grp = Graphics.FromImage(initialPreviewImage))
            {
                grp.FillRectangle(Brushes.Black, 0, 0, 1280, 720);
            }

            PreviewImage = WPFUtils.BitmapToImageSource(initialPreviewImage);
        }
    }
}
