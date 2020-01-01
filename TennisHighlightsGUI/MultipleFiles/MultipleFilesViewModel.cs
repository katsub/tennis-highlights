using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TennisHighlights;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;
using TennisHighlights.VideoCreation;

namespace TennisHighlightsGUI.MultipleFiles
{
    /// <summary>
    /// The join files view model
    /// </summary>
    public class MultipleFilesViewModel : VideoConversionScreenViewModel
    {
        //The paths separator
        private const string _pathsSeparator = ";";

        /// <summary>
        /// The main view model
        /// </summary>
        public MainViewModel MainVM { get; }

        /// <summary>
        /// The add file command
        /// </summary>
        public Command AddFileCommand { get; }
        /// <summary>
        /// The remove selected file command
        /// </summary>
        public Command RemoveSelectedFileCommand { get; }
        /// <summary>
        /// The add file command
        /// </summary>
        public Command LoadFileIntoEditorCommand { get; }
        /// <summary>
        /// The join files command
        /// </summary>
        public Command JoinFilesCommand { get; }
        /// <summary>
        /// The convert files command
        /// </summary>
        public Command ConvertFilesCommand { get; }
        /// <summary>
        /// The convert and join files command
        /// </summary>
        public Command ConvertAndJoinFilesCommand { get; }

        private string _outputFilePath;
        /// <summary>
        /// The output file path
        /// </summary>
        public string OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                if (value != _outputFilePath)
                {
                    _outputFilePath = value;

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// True if files can be processed
        /// </summary>
        public bool CanProcessFiles => FilesToProcess.Count > 1 && !IsConverting;
        /// <summary>
        /// The selected file
        /// </summary>
        public SingleFileViewModel SelectedFile { get; set; }
        /// <summary>
        /// Gets the files to process
        /// </summary>
        public ObservableCollection<SingleFileViewModel> FilesToProcess { get; } = new ObservableCollection<SingleFileViewModel>();

        public override bool CanConvert => false;

        /// <summary>
        /// Initializes a new instance of the class JoinFilesViewModel
        /// </summary>
        /// <param name="mainVM">The main view model</param>
        public MultipleFilesViewModel(MainViewModel mainVM)
        {
            MainVM = mainVM;

            var selectedFiles = mainVM.Settings.General.MultipleFilesPaths;

            SendProgressInfo("", 0, 0);

            FilesToProcess.CollectionChanged += FilesToJoin_CollectionChanged;

            if (!string.IsNullOrEmpty(selectedFiles))
            {
                foreach (var file in selectedFiles.Split(_pathsSeparator))
                {
                    if (File.Exists(file))
                    {
                        FilesToProcess.Add(new SingleFileViewModel(file, GetFileStatus(file)));
                    }
                }
            }

            RemoveSelectedFileCommand = new Command((param) =>
            {
                if (SelectedFile != null)
                {
                    Remove(SelectedFile);
                }
            });

            LoadFileIntoEditorCommand = new Command((param) =>
            {
                if (SelectedFile != null)
                {
                    MainVM.SetChosenFileAndLoadImage(SelectedFile.FilePath);
                }
            });

            AddFileCommand = new Command((param) =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Video files (*.mp4) | *.mp4"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    FilesToProcess.Add(new SingleFileViewModel(openFileDialog.FileName, GetFileStatus(openFileDialog.FileName)));
                }
            });

            JoinFilesCommand = new Command((param) =>
            {
                OnProcessingStarted();

                var filesToJoin = FilesToProcess.Select(f => f.FilePath).ToList();

                new Task(() =>
                {
                    var expectedSize = filesToJoin.Sum(f => new FileInfo(f).Length);

                    while (IsConverting)
                    {
                        if (File.Exists(OutputFilePath))
                        {
                            var progress = 0;

                            try
                            {
                                progress = (int)Math.Round(Math.Min(100d, 100d * ((double)new FileInfo(OutputFilePath).Length) / expectedSize));
                            }
                            //The file might be deleted by other tasks, to be recreated
                            catch { }

                            SendProgressInfo("Joining files...", progress, ElapsedSeconds.TotalSeconds);
                        }

                        Task.Delay(200);
                    }

                    UpdateProgressOver();
                }).Start();

                new Task(() =>
                {
                    var error = FFmpegCaller.JoinFiles(OutputFilePath, filesToJoin, () => RequestedCancel);

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "Error");
                    }

                    OnProcessingOver();
                }).Start();
            });

            ConvertFilesCommand = new Command((param) =>
            {
                ConvertFiles(false);
            });

            ConvertAndJoinFilesCommand = new Command((param) =>
            {
                ConvertFiles(true);
            });
        }

        /// <summary>
        /// Converts the files
        /// </summary>
        /// <param name="alsoJoin">True if the files should be also joined after conversion</param>
        private void ConvertFiles(bool alsoJoin)
        {
            Task.Run(() =>
            {
                OnProcessingStarted();

                var settings = MainVM.Settings;
                var outputFiles = new List<string>();

                try
                {
                    var percentBeforeJoin = alsoJoin ? 50d : 100d;
                    var i = 0;

                    //TODO: refactor General.AnalysedVideoPath so that only the gui can use to recover it, instead of the
                    //whole program depending on it
                    var oldAnalysedVideoPath = settings.General.AnalysedVideoPath;

                    foreach (var file in FilesToProcess)
                    {
                        settings.General.AnalysedVideoPath = file.FilePath;

                        var videoInfo = new VideoInfo(file.FilePath);
                        var chosenFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(file.FilePath);

                        var stopwatch = new Stopwatch();

                        stopwatch.Start();

                        bool checkIfCancelRequested() => RequestedCancel;

                        try
                        {
                            var framesToProcess = settings.General.GetFinalFrameToProcess(videoInfo);

                            if (framesToProcess > chosenFileLog.LastParsedFrame)
                            {
                                void progressUpdateAction(Bitmap bitmap, int processedFrame)
                                {
                                    var percent = percentBeforeJoin * (double)processedFrame / framesToProcess;

                                    var remainingSeconds = percent > 0 ? (100d * stopwatch.Elapsed.TotalSeconds / percent - stopwatch.Elapsed.TotalSeconds) : 0d;

                                    //The join part is estimated at 50% and progress is not analysed because this requires analysing the FFmpeg
                                    //log. Also, there's a final join if the "join all into one" option is checked which is not accounted for
                                    //but it is fast because there is no reencoding, so it's not very critical
                                    SendProgressInfo("Converting...(" + (i + 1) + "/" + FilesToProcess.Count + ")",
                                                     (int)Math.Round((percent + 100d * i) / FilesToProcess.Count),
                                                     ElapsedSeconds.TotalSeconds);
                                }

                                var ballsPerFrame = new VideoBallsExtractor(settings, videoInfo, chosenFileLog,
                                                                            checkIfCancelRequested, progressUpdateAction).GetBallsPerFrame().Result;

                                chosenFileLog.Save();

                                //We update the file status now that it's been converted
                                file.Status = GetFileStatus(file.FilePath);
                            }

                            if (!RequestedCancel && alsoJoin)
                            {
                                if (chosenFileLog.Rallies.Count == 0)
                                {
                                    var rallies = TennisHighlightsEngine.GetRalliesFromBalls(settings, chosenFileLog.Balls, chosenFileLog, null, checkIfCancelRequested);

                                    chosenFileLog.Rallies.Clear();

                                    var j = 0;
                                    foreach (var rally in rallies)
                                    {
                                        var rallyStart = (int)Math.Max(0, rally.FirstBall.FrameIndex - settings.General.SecondsBeforeRally
                                                                                                       * videoInfo.FrameRate);

                                        var rallyEnd = (int)Math.Min(videoInfo.TotalFrames, rally.LastBall.FrameIndex + settings.General.SecondsAfterRally
                                                                                                                        * videoInfo.FrameRate);

                                        chosenFileLog.Rallies.Add(new RallyEditData(j.ToString()) { Start = rallyStart, Stop = rallyEnd });
                                        j++;
                                    }

                                    chosenFileLog.Save();
                                }

                                if (chosenFileLog.Rallies.Count > 0)
                                {
                                    //If no rally was selected by default, we export them all, otherwise, we wanna preserve
                                    //the user's selection
                                    if (!chosenFileLog.Rallies.Any(r => r.IsSelected))
                                    {
                                        foreach (var rally in chosenFileLog.Rallies)
                                        {
                                            rally.IsSelected = true;
                                        }

                                        //We update file status to show all rallies selected
                                        file.Status = GetFileStatus(file.FilePath);
                                    }

                                    SendProgressInfo("Joining rallies of video #" + i + "...",
                                                     (int)Math.Round(100d * (0.5d + 1d * i) / FilesToProcess.Count),
                                                     ElapsedSeconds.TotalSeconds);

                                    var outputFile = RallyVideoCreator.BuildVideoWithAllRallies(chosenFileLog.Clone().Rallies.Where(r => r.IsSelected).ToList(),
                                                                                                videoInfo, settings.General, chosenFileLog.RotationDegrees, out var error, null,
                                                                                                () => RequestedCancel);

                                    if (!string.IsNullOrEmpty(outputFile))
                                    {
                                        outputFiles.Add(outputFile);
                                    }

                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        throw new Exception(error);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            var finalError = "An error has been encountered in the conversion of file " + file + " :\n\n" + e.ToString();

                            Logger.Log(LogType.Error, finalError);
                            MessageBox.Show(finalError);

                            break;
                        }

                        i++;
                    }

                    settings.General.AnalysedVideoPath = oldAnalysedVideoPath;
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, e.ToString());
                    MessageBox.Show(e.ToString());
                }
                finally
                {
                    if (outputFiles.Count > 1)
                    {
                        try
                        {
                            var error = FFmpegCaller.JoinFiles(OutputFilePath, outputFiles, () => RequestedCancel);

                            if (!string.IsNullOrEmpty(error))
                            {
                                Logger.Log(LogType.Error, error);
                                MessageBox.Show(error, "Error");
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log(LogType.Error, e.ToString());
                            MessageBox.Show(e.ToString(), "Error");
                        }
                    }

                    OnProcessingOver();
                }
            });
        }

        /// <summary>
        /// Gets the file status.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private string GetFileStatus(string filePath)
        {
            var status = "";
            var videoInfo = new VideoInfo(filePath);
            var chosenFileLog = ProcessedFileLog.GetOrCreateProcessedFileLog(filePath);

            try
            {
                var framesToProcess = MainVM.Settings.General.GetFinalFrameToProcess(videoInfo);

                if (framesToProcess <= chosenFileLog.LastParsedFrame)
                {
                    status = "Converted, " + chosenFileLog.Rallies.Count(r => r.IsSelected) + "/" + chosenFileLog.Rallies.Count + " rallies";
                }
                else
                {
                    status = "Not converted (" + chosenFileLog.LastParsedFrame + "/" + framesToProcess + ")";
                }
            }
            catch { }

            return status;
        }

        /// <summary>
        /// Called when processing is started
        /// </summary>
        private void OnProcessingStarted()
        {
            ConvertCommand.Execute(null);

            OnPropertyChanged(nameof(CanProcessFiles));
        }

        /// <summary>
        /// Updates the progress after processing is over
        /// </summary>
        private void UpdateProgressOver()
        {
            var message = RequestedCancel ? "Cancelled" : "Completed";

            SendProgressInfo(message,
                             RequestedCancel ? 0 : 100,
                             ElapsedSeconds.TotalSeconds);
        }

        /// <summary>
        /// Called when processing is over
        /// </summary>
        private void OnProcessingOver()
        {
            IsConverting = false;
            CancelRequestHandled();

            UpdateProgressOver();

            OnPropertyChanged(nameof(CanProcessFiles));

            UpdateOutputFilePath();

            if (MainVM.Settings.General.BeepWhenFinished)
            {
                PlayConversionOverSound();
            }
        }

        /// <summary>
        /// Handles the CollectionChanged event of the FilesToJoin control
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void FilesToJoin_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainVM.Settings.General.MultipleFilesPaths = string.Join(_pathsSeparator, FilesToProcess.Select(f => f.FilePath));
            MainVM.Settings.Save();

            OnPropertyChanged(nameof(CanProcessFiles));

            UpdateOutputFilePath();
        }

        /// <summary>
        /// Updates the output file path
        /// </summary>
        private void UpdateOutputFilePath()
        {
            if (FilesToProcess.Count > 0)
            {
                var outputFolder = Path.GetDirectoryName(FilesToProcess.First().FilePath) + "\\";

                OutputFilePath = FileManager.GetUnusedFilePathInFolderFromFileName("output.mp4", outputFolder, ".mp4");
            }
            else
            {
                OutputFilePath = string.Empty;
            }
        }

        /// <summary>
        /// Removes a file from the files to join
        /// </summary>
        /// <param name="fileToRemove">The file to remove</param>
        public void Remove(SingleFileViewModel fileToRemove) => FilesToProcess.Remove(fileToRemove);

        /// <summary>
        /// The internal convert.
        /// </summary>
        /// <param name="param"></param>
        protected override void ConvertInternal(object param) { }
    }
}
