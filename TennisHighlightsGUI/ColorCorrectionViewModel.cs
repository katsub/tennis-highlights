using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;
using TennisHighlights;
using TennisHighlights.Utils;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The color correction view model
    /// </summary>
    public class ColorCorrectionViewModel : ViewModelBase
    {
        /// <summary>
        /// The main view model
        /// </summary>
        public MainViewModel MainVM { get; }
        private BitmapImage _previewImage;
        /// <summary>
        /// Gets the preview image.
        /// </summary>
        public BitmapImage PreviewImage
        {
            get => _previewImage;
            set
            {
                if (_previewImage != value)
                {
                    _previewImage = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// The sample frame, before color correction
        /// </summary>
        private string _sampleFrame;
        /// <summary>
        /// The frame index of the frame currently displayed in the preview
        /// </summary>
        private int _previewFrameIndex;
        /// <summary>
        /// The frame index of the frame currently displayed in the preview and color corrected
        /// </summary>
        private int _previewFrameColorCorrectedIndex = int.MinValue;
        /// <summary>
        /// The color correction settings of the frame currently displayed in the preview
        /// </summary>
        private ColorCorrectionSettings _previewCCSettings = new ColorCorrectionSettings();
        private string _statusText;
        /// <summary>
        /// The status text
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;

                    OnPropertyChanged();
                }
            }
        }
        private int _sampleFrameIndex;
        /// <summary>
        /// The frame serving as sample
        /// </summary>
        public int SampleFrameIndex
        {
            get => _sampleFrameIndex;
            set
            {
                if (_sampleFrameIndex != value)
                {
                    _sampleFrameIndex = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets the brightness
        /// </summary>
        public int Brightness
        {
            get => MainVM.ChosenFileLog.CCSettings.Brightness;
            set
            {
                if (MainVM.ChosenFileLog.CCSettings.Brightness != value)
                {
                    MainVM.ChosenFileLog.CCSettings.Brightness = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets the saturation
        /// </summary>
        public int Saturation
        {
            get => MainVM.ChosenFileLog.CCSettings.Saturation;
            set
            {
                if (MainVM.ChosenFileLog.CCSettings.Saturation != value)
                {
                    MainVM.ChosenFileLog.CCSettings.Saturation = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets the contrast
        /// </summary>
        public int Contrast
        {
            get => MainVM.ChosenFileLog.CCSettings.Contrast;
            set
            {
                if (MainVM.ChosenFileLog.CCSettings.Contrast != value)
                {
                    MainVM.ChosenFileLog.CCSettings.Contrast = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// True if color correction should be used when exporting rallies, false otherwise
        /// </summary>
        public bool UseColorCorrection
        {
            get => MainVM.ChosenFileLog.UseColorCorrection;
            set
            {
                if (MainVM.ChosenFileLog.UseColorCorrection != value)
                {
                    MainVM.ChosenFileLog.UseColorCorrection = value;

                    MainVM.ChosenFileLog.SaveColorSettings();

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets the tone color
        /// </summary>
        public int ToneColor
        {
            get => MainVM.ChosenFileLog.CCSettings.ToneColor;
            set
            {
                if (MainVM.ChosenFileLog.CCSettings.ToneColor != value)
                {
                    MainVM.ChosenFileLog.CCSettings.ToneColor = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets the warm color
        /// </summary>
        public int WarmColor
        {
            get => MainVM.ChosenFileLog.CCSettings.WarmColor;
            set
            {
                if (MainVM.ChosenFileLog.CCSettings.WarmColor != value)
                {
                    MainVM.ChosenFileLog.CCSettings.WarmColor = value;

                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// True if preview image color correction is being updated
        /// </summary>  
        private bool _isUpdatingColorCorrection;
        /// <summary>
        /// True if preview image sample frame is being updated
        /// </summary>  
        private bool _isUpdatingSampleFrame;
        /// <summary>
        /// The preview update timer
        /// </summary>
        private readonly Timer _previewUpdateTimer = new System.Timers.Timer
        {
            Interval = 200,
            // Have the timer fire repeated events (true is the default)
            AutoReset = true,
            // Start the timer
            Enabled = true
        };

        /// <summary>
        /// Initializes a new instance of the class ColorCorrectionViewModel
        /// </summary>
        /// <param name="mainVM">The main view model</param>
        public ColorCorrectionViewModel(MainViewModel mainVM)
        {
            MainVM = mainVM;

            UpdateSampleFrame(true);

            _previewUpdateTimer.Elapsed += PreviewUpdateTimer_Elapsed;
        }

        private bool _isSavingSettings;

        /// <summary>
        /// Handles the elapsed event of the timer control
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void PreviewUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //TODO: implement event-oriented approach, instead of periodical updates
            UpdatePreviewImage();
            UpdateSampleFrame();

            if (!MainVM.ChosenFileLog.CCSettings.Equals(_previewCCSettings))
            {
                if (!_isSavingSettings)
                {
                    _isSavingSettings = true;

                    try
                    {
                        MainVM.ChosenFileLog.SaveColorSettings();
                    }
                    finally
                    {
                        _isSavingSettings = false;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the sample frame
        /// </summary>
        /// <param name="firstCall">True if it's the first call (sample frame null)</param>
        private void UpdateSampleFrame(bool firstCall = false)
        {
            if (!_isUpdatingSampleFrame && (_sampleFrameIndex != _previewFrameIndex || firstCall))
            {
                _previewFrameIndex = _sampleFrameIndex;

                _isUpdatingSampleFrame = true;
                StatusText = "Loading...";

                var sampleTime = MainVM.VideoInfo.TotalFrames * ((double)SampleFrameIndex / 100d)
                                 / MainVM.VideoInfo.FrameRate;

                new Task(() =>
                {
                    try
                    {
                        _sampleFrame = FFmpegCaller.ExtractSingleFrameAndReturnPath(MainVM.ChosenFile, TimeSpan.FromSeconds(sampleTime));

                        if (firstCall)
                        {
                            UpdatePreviewImage(true);
                        }                       
                    }
                    finally
                    {
                        StatusText = "";
                        _isUpdatingSampleFrame = false;
                    }
                }).Start();
            }
        }
       
        /// <summary>
        /// Updates the preview image
        /// </summary>
        /// <param name="forceRefresh">Forces the refresh</param>
        public void UpdatePreviewImage(bool forceRefresh = false)
        {
            if (!_isUpdatingColorCorrection 
                && (_previewFrameColorCorrectedIndex != _previewFrameIndex
                    || !MainVM.ChosenFileLog.CCSettings.Equals(_previewCCSettings))
                    || forceRefresh)
            {
                _isUpdatingColorCorrection = true;
                _previewFrameColorCorrectedIndex = _previewFrameIndex;
                _previewCCSettings = new ColorCorrectionSettings(MainVM.ChosenFileLog.CCSettings);

                new Task(() =>
                {
                    try
                    {
                        if (_sampleFrame != null)
                        {
                            var colorCorrectedBitmap = FFmpegCaller.ColorCorrect(_sampleFrame, MainVM.ChosenFileLog.CCSettings, 5);

                            new Action(() => PreviewImage = WPFUtils.BitmapToImageSource(colorCorrectedBitmap)).ExecuteOnUIThread();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, e.ToString());
                    }

                    _isUpdatingColorCorrection = false;
                }).Start();
            }
        }
    }
}