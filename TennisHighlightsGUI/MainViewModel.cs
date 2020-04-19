using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using TennisHighlights;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Rallies;
using TennisHighlights.Utils;
using TennisHighlights.VideoCreation;
using TennisHighlightsGUI.MultipleFiles;

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
        /// The color correction window
        /// </summary>
        private ColorCorrectionWindow _colorCorrectionWindow;

        /// <summary>
        /// The multiple files window
        /// </summary>
        private MultipleFilesWindow _multipleFilesWindow;

        /// <summary>
        /// The rally selection view model
        /// </summary>
        private RallySelectionViewModel _rallySelectionViewModel;
        /// <summary>
        /// The settings
        /// </summary>
        public TennisHighlightsSettings Settings { get; private set; }

        #region Commands
        public Command EstimatePoseCommand { get; }
        /// <summary>
        /// Gets the choose file command.
        /// </summary>
        public Command ChooseFileCommand { get; }
        /// <summary>
        /// Gets the multiple files command.
        /// </summary>
        public Command MultipleFilesCommand { get; }
        /// <summary>
        /// Gets the color correction command.
        /// </summary>
        public Command ColorCorrectionCommand { get; }
        /// <summary>
        /// Gets the choose output folder command.
        /// </summary>
        public Command ChooseOutputFolderCommand { get; }
        /// <summary>
        /// Gets the open output folder command.
        /// </summary>
        public Command OpenOutputFolderCommand { get; }
        /// <summary>
        /// Gets the open chosen file command.
        /// </summary>
        public Command OpenChosenFileCommand { get; }
        /// <summary>
        /// Gets the open rally graph command.
        /// </summary>
        public Command OpenRallyGraphCommand { get; }
        /// <summary>
        /// Gets the regenerate rallies command.
        /// </summary>
        public Command RegenerateRalliesCommand { get; }
        #endregion

        #region Properties
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

                    Settings.General.TempDataPath = OutputFolder;

                    FileManager.Initialize(Settings.General);

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

                    var targetSize = Settings.General.GetTargetSize(VideoInfo);

                    ResolutionDependentParameter.SetTargetResolutionHeight(targetSize.Height);

                    FFmpegCaller.VideoInfo = VideoInfo;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanConvert));
                }
            }
        }

        /// <summary>
        /// Gets the image angle.
        /// </summary>
        public int RotationAngle
        {
            get => ChosenFileLog?.RotationDegrees ?? 0;
            set
            {
                if (ChosenFileLog != null && ChosenFileLog.RotationDegrees != value)
                {
                    ChosenFileLog.RotationDegrees = value;

                    //Needs to be saved immediately because the multiple file window might use it
                    ChosenFileLog.SaveRotation();
                    OnPropertyChanged();
                }
            }
        }

        private bool _beepWhenFinished;
        /// <summary>
        /// True if the program should beep when finished
        /// </summary>
        public bool BeepWhenFinished
        {
            get => _beepWhenFinished;
            set
            {
                if (_beepWhenFinished != value)
                {
                    _beepWhenFinished = value;

                    OnPropertyChanged();
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
                    OnPropertyChanged(nameof(RotationAngle));
                }
            }
        }
        #endregion

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
        /// Gets a value indicating whether this instance is in debug mode.
        /// </summary>
        public bool IsInDebugMode => ConditionalCompilation.Debug;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            InitializeSettings();

            FileManager.Initialize(Settings.General);

            LoadInitialPreviewImage();

            Settings.General.PropertyChanged += General_PropertyChanged;

            #region Commands
            MultipleFilesCommand = new Command((param) =>
            {
                if (_multipleFilesWindow == null || !_multipleFilesWindow.IsLoaded)
                {
                    _multipleFilesWindow = new MultipleFilesWindow(this);
                }

                _multipleFilesWindow.WindowState = WindowState.Minimized;
                _multipleFilesWindow.Show();
                _multipleFilesWindow.WindowState = WindowState.Normal;
            });

            EstimatePoseCommand = new Command((param) =>
            {
                PoseEstimationBuilder.ClassifyLogFile(ChosenFileLog, VideoInfo);
            });

            ColorCorrectionCommand = new Command((param) =>
            {
                if (_colorCorrectionWindow == null || !_colorCorrectionWindow.IsLoaded)
                {
                    _colorCorrectionWindow = new ColorCorrectionWindow(this);
                }

                _colorCorrectionWindow.WindowState = WindowState.Minimized;
                _colorCorrectionWindow.Show();
                _colorCorrectionWindow.WindowState = WindowState.Normal;
            });

            RegenerateRalliesCommand = new Command((param) =>
            {
                var result = MessageBox.Show("This will restore all rallies to their original start-stop points from when they were extracted. Are you sure?", "Warning", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            IsConverting = true;

                            RegenerateRallies();

                            OnRalliesReady();
                        }
                        finally
                        {
                            IsConverting = false;
                            CancelRequestHandled();
                        }
                    });
                }
            });

            /*
            OpenRallyGraphCommand = new Command((param) =>
            {
                var rallyClassificationsFile = FileManager.ReadPersistentFile(_rallyClassificationsFileName);

                var rallies = TennisHighlightsEngine.GetRalliesFromBalls(Settings, ChosenFileLog);

                rallies = RallyFilter.FilterRalliesByDuration(rallies);

                if (rallies.Count == ChosenFileLog.Rallies.Count)
                {
                    var rallyData = new RallyClassificationData(rallies.Select(r => (r, ChosenFileLog.Rallies[rallies.IndexOf(r)].IsSelected))
                                                                       .ToList());

                    var rallyGraphViewModel = new RallyGraphViewModel(rallyData);
                    var rallyGraphWindow = new RallyGraphWindow(rallyGraphViewModel);

                    rallyGraphWindow.Show();
                }
            });*/

            ChooseFileCommand = new Command((param) =>
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

            OpenChosenFileCommand = new Command((param) =>
            {
                if (!string.IsNullOrEmpty(ChosenFile) && File.Exists(ChosenFile))
                {
                    Process.Start("explorer.exe", ChosenFile);
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
        /// Handles the PropertyChanged event of the General control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void General_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == (nameof(Settings.General.TrackPlayerMoves)) && Settings.General.TrackPlayerMoves)
            {
                var caffeeModelPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\pose_iter_160000.caffemodel";

                if (!File.Exists(caffeeModelPath))
                {
                    var result = MessageBox.Show("Model needed for player move tracking not found. Download it now?", "Model not found", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        var uri = "https://drive.google.com/uc?export=download&confirm=YNtK&id=1xhVSPUTBS33BIQJQzx_SVrpls0LTtPas";

                        var webClient = new WebClient();

                        var caffeeModel = webClient.DownloadData(uri);

                        File.WriteAllBytes(caffeeModelPath, caffeeModel);
                    }
                    else
                    {
                        Settings.General.TrackPlayerMoves = false;
                    }
                }
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

            //Update the log, it might have been modified by other screens (i.e. multiple files screen converting videos)
            ChosenFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(Settings.General);

            //We're gonna do heavy operations, could lead to out of memory or C++ crash, better save everything before doing so
            Settings.Save();

            //Begin conversion
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            Task.Run(() =>
            {
                try
                {
                    var framesToProcess = Settings.General.GetFinalFrameToProcess(VideoInfo);

                    if (Settings.General.GetFinalFrameToProcess(VideoInfo) <= ChosenFileLog.LastParsedFrame)
                    {
                        if (!ChosenFileLog.Rallies.Any())
                        {
                            RegenerateRallies();
                        }
                        else
                        {
                            SendProgressInfo("Parsed from cache.", 100, 0d);
                        }
                    }
                    else
                    {
                        void progressUpdateAction(Bitmap bitmap, int processedFrame)
                        {
                            var percent = 100d * (double)processedFrame / framesToProcess;

                            var remainingSeconds = percent > 0 ? (100d * stopwatch.Elapsed.TotalSeconds / percent - stopwatch.Elapsed.TotalSeconds) : 0d;

                            SendProgressInfo(new ProgressInfo(bitmap, (int)Math.Round(percent), "Reading frames (" + processedFrame + "/" + framesToProcess + ")", remainingSeconds));
                        }

                        bool checkIfCancelRequested() => RequestedCancel;

                        var ballsPerFrame = VideoBallsExtractor.ExtractVideoData(Settings, VideoInfo, ChosenFileLog,
                                                                                 checkIfCancelRequested, progressUpdateAction);

                        ChosenFileLog.Save();

                        if (!RequestedCancel)
                        {                            
                            RegenerateRallies(stopwatch.Elapsed.TotalSeconds);
                        }
                    }

                    if (!RequestedCancel)
                    {
                        if (ChosenFileLog.Rallies.Count > 0)
                        {
                            OnRalliesReady();
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
                    try
                    {
                        if (Settings.General.BeepWhenFinished)
                        {
                            PlayConversionOverSound();
                        }
                    }
                    catch { }

                    IsConverting = false;
                    CancelRequestHandled();
                    OnPropertyChanged(nameof(CanRegenerateRallies));
                }
            });
        }

        /// <summary>
        /// Called when [rallies ready].
        /// </summary>
        private void OnRalliesReady()
        {
            if (Settings.General.AutoJoinAll)
            {
                foreach (var rally in ChosenFileLog.Rallies)
                {
                    rally.IsSelected = true;
                }

                ColorCorrectionSettings ccSettings = null;

                if (ChosenFileLog.UseColorCorrection) { ccSettings = ChosenFileLog.CCSettings; }

                RallyVideoCreator.BuildVideoWithAllRallies(ChosenFileLog.Clone().Rallies,
                                                           VideoInfo, Settings.General, ChosenFileLog.RotationDegrees,
                                                           ccSettings, out var error, SendProgressInfo, () => RequestedCancel);
            }
            else
            {
                //Switches to the rally selection view
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
        }

        /// <summary>
        /// Initializes the settings.
        /// </summary>
        private void InitializeSettings()
        {
            Settings = new TennisHighlightsSettings();
            //This creates an initial settings file that can be modified later if needed
            Settings.Save();

            FFmpegCaller.Settings = Settings.General;

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

                        if (!TryToFindFFmpeg())
                        {
                            System.Windows.Application.Current.Shutdown();
                        }
                    }
                    else
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }
            }

            FFmpegCaller.FFmpegPath = Settings.General.FFmpegPath;
        }

        /// <summary>
        /// Regenerates the rallies.
        /// </summary>
        /// <param name="elapsedTime">The elapsed time.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void RegenerateRallies(double elapsedTime = 0d)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            void rallyProgressUpdateAction(int currentRally, int currentBallFrame)
            {
                var percent = 100d * (double)currentBallFrame / ChosenFileLog.Balls.Count;

                SendProgressInfo(new ProgressInfo(null, (int)Math.Round(percent), "Built rally " + currentRally + "...", 0d));
            }

            bool cancelRequested() => RequestedCancel;

            ChosenFileLog.ParseRallies(Settings, VideoInfo, rallyProgressUpdateAction, cancelRequested);

            SendProgressInfo("Built rally " + ChosenFileLog.Rallies.Count, 100, elapsedTime + stopwatch.Elapsed.TotalSeconds);
        }

        /// <summary>
        /// Called when [closing].
        /// </summary>
        public void OnClosing()
        {
            Settings.Save();
            ChosenFileLog?.Save();

            FFmpegCaller.KillAllInstances();
        }

        /// <summary>
        /// Sets the chosen file and load image.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void SetChosenFileAndLoadImage(string fileName)
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
        internal static Bitmap GetVideoPreviewImage(string filePath)
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
