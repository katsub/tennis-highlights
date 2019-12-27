using InWit.WPF.MultiRangeSlider;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using TennisHighlights;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;
using TennisHighlights.VideoCreation;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The rally selection view model
    /// </summary>
    public class RallySelectionViewModel : VideoConversionScreenViewModel
    {
        /// <summary>
        /// The timer
        /// </summary>
        private readonly Timer _timer = new Timer
        {
            Interval = 50,
            // Have the timer fire repeated events (true is the default)
            AutoReset = true,
            // Start the timer
            Enabled = true
        };

        /// <summary>
        /// The main view model
        /// </summary>
        public MainViewModel MainVM { get; }
        /// <summary>
        /// The video information
        /// </summary>
        private VideoInfo _videoInfo;
        /// <summary>
        /// The player
        /// </summary>
        private MediaElement _player;
        /// <summary>
        /// The slider
        /// </summary>
        private WitMultiRangeSlider _multiSlider;

        #region Commands
        /// <summary>
        /// Gets the increase speed command.
        /// </summary>
        public Command IncreaseSpeedCommand { get; }
        /// <summary>
        /// Gets the decrease speed command.
        /// </summary>
        public Command DecreaseSpeedCommand { get; }
        /// <summary>
        /// Gets the export command.
        /// </summary>
        public Command ExportCommand { get; }
        /// <summary>
        /// Gets the select all command.
        /// </summary>
        public Command SelectAllCommand { get; }
        /// <summary>
        /// Gets the select none command.
        /// </summary>
        public Command SelectNoneCommand { get; }
        /// <summary>
        /// Gets the play command.
        /// </summary>
        public Command PlayCommand { get; }
        /// <summary>
        /// Gets the pause command.
        /// </summary>
        public Command PauseCommand { get; }
        /// <summary>
        /// Gets the join next command.
        /// </summary>
        public Command JoinNextCommand { get; }
        /// <summary>
        /// Gets the split command.
        /// </summary>
        public Command SplitCommand { get; }
        /// <summary>
        /// Gets the back to main screen command.
        /// </summary>
        public Command BackToMainCommand { get; }
        #endregion

        #region Properties
        private double _playSpeed = 1d;
        /// <summary>
        /// Gets or sets the play speed.
        /// </summary>
        public double PlaySpeed
        {
            get => _playSpeed;
            set
            {
                if (_playSpeed != value)
                {
                    _playSpeed = value;

                    _player.SpeedRatio = _playSpeed;

                    MainVM.Settings.General.RallyPlaySpeed = _playSpeed;

                    OnPropertyChanged();
                }
            }
        }

        private bool _isPlaying;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is playing.
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;

                    OnPropertyChanged();
                }
            }
        }

        private int _minStart;
        /// <summary>
        /// Gets the minimum start.
        /// </summary>
        public int MinStart
        {
            get => _minStart;
            set
            {
                if (_minStart != value)
                {
                    _minStart = value;

                    OnPropertyChanged();
                }
            }
        }

        private int _maxStop;
        /// <summary>
        /// Gets the maximum stop.
        /// </summary>
        public int MaxStop
        {
            get => _maxStop;
            set
            {
                if (_maxStop != value)
                {
                    _maxStop = value;

                    OnPropertyChanged();
                }
            }
        }

        private int _sliderStart;
        /// <summary>
        /// Gets or sets the slider start.
        /// </summary>
        public int SliderStart
        {
            get => _sliderStart;
            set
            {
                if (_sliderStart != value)
                {
                    _sliderStart = value;

                    if (StartSliderBeingDragged)
                    {
                        SelectedRally.Start = value;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPositionSeconds));
                }
            }
        }

        private int _sliderStop;
        /// <summary>
        /// Gets or sets the slider stop.
        /// </summary>
        public int SliderStop
        {
            get => _sliderStop;
            set
            {
                if (_sliderStop != value)
                {
                    _sliderStop = value;

                    if (StopSliderBeingDragged && !StartSliderBeingDragged && !CurrentSliderBeingDragged)
                    {
                        SelectedRally.Stop = value;
                    }

                    OnPropertyChanged();
                }
            }
        }

        private RallyEditViewModel _selectedRally;
        /// <summary>
        /// Gets or sets the selected rally.
        /// </summary>
        public RallyEditViewModel SelectedRally
        {
            get => _selectedRally;
            set
            {
                if (_selectedRally != value)
                {
                    _selectedRally = value;

                    //If it's null we're gonna set it again soon to a non-null value: wait till it comes back
                    if (value != null)
                    {
                        SetSelectedRallyParameters();

                        OnPropertyChanged();
                    }
                }
            }
        }

        private int _currentPosition;
        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPositionSeconds));
                }
            }
        }

        private bool _startSliderBeingDragged;
        /// <summary>
        /// Gets or sets a value indicating whether [start slider being dragged].
        /// </summary>
        public bool StartSliderBeingDragged
        {
            get => _startSliderBeingDragged;
            set
            {
                if (_startSliderBeingDragged != value)
                {
                    _startSliderBeingDragged = value;

                    OnPropertyChanged(nameof(TotalDuration));
                    OnPropertyChanged();
                }
            }
        }

        private bool _stopSliderBeingDragged;
        /// <summary>
        /// Gets or sets a value indicating whether [stop slider being dragged].
        /// </summary>
        public bool StopSliderBeingDragged
        {
            get => _stopSliderBeingDragged;
            set
            {
                if (_stopSliderBeingDragged != value)
                {
                    _stopSliderBeingDragged = value;

                    OnPropertyChanged(nameof(TotalDuration));
                    OnPropertyChanged();
                }
            }
        }

        private bool _currentSliderBeingDragged;
        /// <summary>
        /// Gets or sets a value indicating whether [current slider being dragged].
        /// </summary>
        public bool CurrentSliderBeingDragged
        {
            get => _currentSliderBeingDragged;
            set
            {
                if (_currentSliderBeingDragged != value)
                {
                    _currentSliderBeingDragged = value;

                    OnPropertyChanged();
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets the rallies.
        /// </summary>
        public ObservableCollection<RallyEditViewModel> Rallies { get; } = new ObservableCollection<RallyEditViewModel>();
        /// <summary>
        /// Gets the chosen file URI.
        /// </summary>
        public Uri ChosenFileUri { get; set; }
        /// <summary>
        /// Gets the delta frames.
        /// </summary>
        public int DeltaFrames => (int)(5 * _videoInfo.FrameRate);
        /// <summary>
        /// Gets a value indicating whether this instance can convert.
        /// </summary>
        public override bool CanConvert => Rallies.Any(r => r.IsSelected);
        /// <summary>
        /// Gets the selected rallies count.
        /// </summary>
        public int SelectedRalliesCount => Rallies.Count(r => r.IsSelected);
        /// <summary>
        /// Gets the selected rallies count.
        /// </summary>
        public int TotalRalliesCount => Rallies.Count;
        /// <summary>
        /// Gets the total duration.
        /// </summary>
        public TimeSpan TotalDuration => TimeSpan.FromSeconds(Rallies.Where(r => r.IsSelected).Sum(r => r.DurationSeconds.TotalSeconds));

        /// <summary>
        /// Gets the current position seconds.
        /// </summary>
        public TimeSpan CurrentPositionSeconds
        {
            get
            {
                if (SelectedRally != null)
                {
                    return TimeSpan.FromSeconds((CurrentPosition - SelectedRally.Start) / _videoInfo.FrameRate);
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallySelectionViewModel"/> class.
        /// </summary>
        /// <param name="mainVM">The main vm.</param>
        public RallySelectionViewModel(MainViewModel mainVM)
        {
            MainVM = mainVM;

            Rallies.CollectionChanged += Rallies_CollectionChanged;

            #region Commands
            ExportCommand = new Command((param) =>
            {
                var rally = SelectedRally;
                var settings = mainVM.Settings.General;
                var videoInfo = mainVM.VideoInfo;

                var rallyPath = FileManager.GetUnusedFilePathInFolderFromFileName(settings.AnalysedVideoPath.Substring(0, settings.AnalysedVideoPath.Length - 4).ToString() + "_" + rally.OriginalIndex + ".mp4",
                                                                            FileManager.TempDataPath, ".mp4");

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "MP4 file (*.mp4) | *.mp4",
                    FileName = new FileInfo(rallyPath).Name,
                    InitialDirectory = FileManager.TempDataPath
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    rallyPath = saveFileDialog.FileName;

                    Task.Run(() =>
                    { 
                        FFmpegCaller.ExportRally(rallyPath, rally.Start / videoInfo.FrameRate,
                                                 rally.Stop / videoInfo.FrameRate, settings.AnalysedVideoPath, out var error, () => RequestedCancel);

                        Process.Start("explorer.exe", FileManager.TempDataPath);
                    });
                }
            });

            IncreaseSpeedCommand = new Command((param) =>
            {
                //If the speed is set to over 2x, then after dragging the stop slider, the player plays at regular speed even though
                //its speed ratio is at 2x+. Why? (not high priority since I don't think playing at 2x+ is very useful)
                if (PlaySpeed < 2d)
                {
                    PlaySpeed += 0.25d;
                }
            });

            DecreaseSpeedCommand = new Command((param) =>
            {
                if (PlaySpeed > 0.25d)
                {
                    PlaySpeed -= 0.25d;
                }
            });

            SelectAllCommand = new Command((param) =>
            {
                foreach (var rally in Rallies)
                {
                    rally.IsSelected = true;
                }
            });

            SelectNoneCommand = new Command((param) =>
            {
                foreach (var rally in Rallies)
                {
                    rally.IsSelected = false;
                }
            });

            PlayCommand = new Command((param) => { Play(); });

            PauseCommand = new Command((param) => { Pause(); });

            JoinNextCommand = new Command((param) =>
            {
                var result = MessageBox.Show("This will permanently join those two rallies. Are you sure?", "Warning", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes) { return; }

                var selectedRallyIndex = Rallies.IndexOf(SelectedRally);

                var nextRally = Rallies.ElementAt(selectedRallyIndex + 1);

                var distance = nextRally.Start - SelectedRally.Stop;

                if (Math.Abs(distance) > 12d * _videoInfo.FrameRate)
                {
                    MessageBox.Show("Can only join rallies when selected rally ends close to where the next rally starts, such as those generated by the \'Split\' button.", "Warning");
                    return;
                }

                var joinedRally = new RallyEditViewModel(new RallyEditData(SelectedRally.OriginalIndex + "_" + nextRally.OriginalIndex)
                {
                    Start = (int)Math.Min(SelectedRally.Start, nextRally.Start),
                    Stop = (int)Math.Max(SelectedRally.Stop, nextRally.Stop)
                },
                                                         DeltaFrames, _videoInfo.TotalFrames, _videoInfo.FrameRate);

                Rallies.Insert(selectedRallyIndex, joinedRally);
                MainVM.ChosenFileLog.Rallies.Insert(selectedRallyIndex, joinedRally.Data);

                var rallyToRemove = SelectedRally;
                SelectedRally = joinedRally;

                Rallies.Remove(rallyToRemove);
                Rallies.Remove(nextRally);
                MainVM.ChosenFileLog.Rallies.Remove(rallyToRemove.Data);
                MainVM.ChosenFileLog.Rallies.Remove(nextRally.Data);
            });

            SplitCommand = new Command((param) =>
            {
                var result = MessageBox.Show("This will permanently split this rally. Are you sure?", "Warning", MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes) { return; }

                var firstRally = new RallyEditViewModel(new RallyEditData(SelectedRally.OriginalIndex + ".1")
                {
                    Start = SelectedRally.Start,
                    Stop = CurrentPosition
                },
                DeltaFrames, _videoInfo.TotalFrames, _videoInfo.FrameRate);

                var secondRally = new RallyEditViewModel(new RallyEditData(SelectedRally.OriginalIndex + ".2")
                {
                    Start = CurrentPosition,
                    Stop = SelectedRally.Stop
                },
                DeltaFrames, _videoInfo.TotalFrames, _videoInfo.FrameRate);

                var selectedRallyIndex = Rallies.IndexOf(SelectedRally);

                Rallies.Insert(selectedRallyIndex, secondRally);
                Rallies.Insert(selectedRallyIndex, firstRally);
                MainVM.ChosenFileLog.Rallies.Insert(selectedRallyIndex, secondRally.Data);
                MainVM.ChosenFileLog.Rallies.Insert(selectedRallyIndex, firstRally.Data);

                var rallyToRemove = SelectedRally;
                SelectedRally = firstRally;

                Rallies.Remove(rallyToRemove);
                MainVM.ChosenFileLog.Rallies.Remove(rallyToRemove.Data);
            });

            BackToMainCommand = new Command((param) => { SwitchToMainView(); });
            #endregion

            _timer.Elapsed += Timer_Elapsed; ;
            _timer.Start();
        }

        /// <summary>
        /// Handles the CollectionChanged event of the Rallies control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void Rallies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var rally in e.OldItems.OfType<RallyEditViewModel>())
                {
                    rally.PropertyChanged -= Rally_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var rally in e.NewItems.OfType<RallyEditViewModel>())
                {
                    rally.PropertyChanged += Rally_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(TotalRalliesCount));
        }

        /// <summary>
        /// Handles the PropertyChanged event of the Rally control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void Rally_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(RallyEditViewModel.IsSelected):
                    OnPropertyChanged(nameof(TotalDuration));
                    OnPropertyChanged(nameof(SelectedRalliesCount));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Converts the internal.
        /// </summary>
        /// <param name="param">The parameter.</param>
        protected override void ConvertInternal(object param)
        {
            if (!Rallies.Any(r => r.IsSelected))
            {
                MessageBox.Show("Select at least one rally before converting.", "Error");

                IsConverting = false;
                CancelRequestHandled();
                return;
            }

            Pause();

            //We're gonna do heavy operations, could lead to out of memory or C++ crash, better save everything before doing so
            MainVM.Settings.Save();
            MainVM.ChosenFileLog.Save();

            //Begin conversion
            Task.Run(() =>
            {
                try
                {
                    RallyVideoCreator.BuildVideoWithAllRallies(MainVM.ChosenFileLog.Clone().Rallies.Where(r => r.IsSelected).ToList(),
                                                               MainVM.VideoInfo, MainVM.Settings.General, out var error, SendProgressInfo, () => RequestedCancel);

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
                        if (MainVM.Settings.General.BeepWhenFinished)
                        {
                            PlayConversionOverSound();
                        }
                    }
                    catch { }

                    IsConverting = false;
                    CancelRequestHandled();
                }
            });
        }

        /// <summary>
        /// Initializes the video parameters.
        /// </summary>
        public void InitializeVideoParameters()
        {
            ChosenFileUri = new Uri(MainVM.ChosenFile);

            _videoInfo = new VideoInfo(MainVM.ChosenFile);

            Rallies.Clear();

            foreach (var rallyData in MainVM.ChosenFileLog.Rallies)
            {
                Rallies.Add(new RallyEditViewModel(rallyData, DeltaFrames, _videoInfo.TotalFrames - 1, _videoInfo.FrameRate));
            }
        }

        /// <summary>
        /// Switches to main view.
        /// </summary>
        private void SwitchToMainView()
        {
            new Action(() =>
            {
                Switcher.Switch(new MainWindow(MainVM));
            }).ExecuteOnUIThread();
        }

        /// <summary>
        /// Gets the player position from frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        public TimeSpan GetPlayerPositionFromFrame(int frame) => TimeSpan.FromSeconds((double)frame / _videoInfo.FrameRate);

        /// <summary>
        /// Gets the frame from player position.
        /// </summary>
        /// <param name="position">The position.</param>
        public int GetFrameFromPlayerPosition(TimeSpan position) => (int)(position.TotalSeconds * _videoInfo.FrameRate);

        /// <summary>
        /// Called when user begins dragging start slider.
        /// </summary>
        public bool BeganDraggingStartSlider() => StartSliderBeingDragged = true;
        /// <summary>
        /// Called when user stops dragging start slider.
        /// </summary>
        public void StoppedDraggingStartSlider()
        {
            StartSliderBeingDragged = false;

            CurrentPosition = SelectedRally.Start;

            Play();
        }
        /// <summary>
        /// Called when user begins dragging stop slider.
        /// </summary>
        public bool BeganDraggingStopSlider() => StopSliderBeingDragged = true;
        /// <summary>
        /// Called when user stops dragging stop slider.
        /// </summary>
        public void StoppedDraggingStopSlider()
        {
            StopSliderBeingDragged = false;

            Play();
        }
        /// <summary>
        /// Called when user begins dragging current slider.
        /// </summary>
        public bool BeganDraggingCurrentSlider() => CurrentSliderBeingDragged = true;
        /// <summary>
        /// Called when user stops dragging current slider.
        /// </summary>
        public void StoppedDraggingCurrentSlider() => CurrentSliderBeingDragged = false;

        /// <summary>
        /// Handles the Elapsed event of the _timer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            new Action(() =>
            {
                if ((_player.Source != null) && (_player.NaturalDuration.HasTimeSpan))
                {
                    if (StartSliderBeingDragged || StopSliderBeingDragged || CurrentSliderBeingDragged)
                    {
                        Pause();
                    }

                    //StopSlider is activated whenever start and current are too (and on its own if it's actually being clicked. This seems to be a limitation of the multislider control
                    //Must be careful when handling it
                    if (StartSliderBeingDragged)
                    {
                        _player.Position = GetPlayerPositionFromFrame(SelectedRally.Start);

                        CurrentPosition = (int)Math.Min(SelectedRally.Stop - 1, SelectedRally.Start + (int)_videoInfo.FrameRate);
                    }
                    else if (CurrentSliderBeingDragged)
                    {
                        _player.Position = GetPlayerPositionFromFrame(CurrentPosition);
                    }
                    else if (StopSliderBeingDragged)
                    {
                        _player.Position = GetPlayerPositionFromFrame(SelectedRally.Stop);

                        //We place the current position 1s before the stop so we can quickly
                        //visualize the new endpoint
                        CurrentPosition = (int)Math.Max(SelectedRally.Start, SelectedRally.Stop - (int)_videoInfo.FrameRate);
                    }

                    if (IsPlaying)
                    {
                        CurrentPosition = GetFrameFromPlayerPosition(_player.Position);

                        if (CurrentPosition > SelectedRally.Stop)
                        {
                            CurrentPosition = SelectedRally.Start;

                            _player.Position = GetPlayerPositionFromFrame(CurrentPosition);
                        }
                    }
                }
            }).ExecuteOnUIThread();
        }

        /// <summary>
        /// Plays this instance.
        /// </summary>
        private void Play()
        {
            _player.Position = GetPlayerPositionFromFrame(CurrentPosition);
            _player.Play();

            //Speed must be reassigned after each play
            OnPropertyChanged(nameof(PlaySpeed));

            IsPlaying = true;
        }

        /// <summary>
        /// Pauses this instance.
        /// </summary>
        private void Pause()
        {
            _player.Pause();

            IsPlaying = false;
        }

        /// <summary>
        /// Sets the selected rally parameters.
        /// </summary>
        public void SetSelectedRallyParameters()
        {
            MinStart = 0;
            SliderStart = 1;
            CurrentPosition = 2;
            SliderStop = 3;
            MaxStop = 4;

            MaxStop = SelectedRally.MaxStop;
            MinStart = SelectedRally.MinStart;

            _multiSlider.ResetItems();

            SliderStop = SelectedRally.Stop;
            CurrentPosition = SelectedRally.Start;
            SliderStart = SelectedRally.Start;

            Play();
        }

        /// <summary>
        /// Sets the player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="multiSlider">The multi slider.</param>
        public void SetPlayer(MediaElement player, WitMultiRangeSlider multiSlider)
        {
            _player = player;

            PlaySpeed = MainVM.Settings.General.RallyPlaySpeed;

            //If the play speed was already at the settings value, we have to manually set the player speed, because
            //the code from play speed set {} won't be called.
            _player.SpeedRatio = PlaySpeed;

            _multiSlider = multiSlider;

            if (Rallies.Any())
            {
                SelectedRally = Rallies.First();
            }
        }
    }
}
