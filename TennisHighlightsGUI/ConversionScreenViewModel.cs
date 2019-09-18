using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using TennisHighlights.Utils;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// A screen that performs video conversions
    /// </summary>
    /// <seealso cref="TennisHighlightsGUI.ViewModelBase" />
    public abstract class VideoConversionScreenViewModel : ViewModelBase
    {
        /// <summary>
        /// The update remaining seconds timer
        /// </summary>
        private readonly System.Timers.Timer _updateRemainingSecondsTimer = new System.Timers.Timer
        {
            Interval = 1000,
            // Have the timer fire repeated events (true is the default)
            AutoReset = true,
            // Start the timer
            Enabled = true
        };

        #region Properties
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

        private bool _isConverting;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is converting.
        /// </summary>
        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                if (_isConverting != value)
                {
                    _isConverting = value;

                    OnPropertyChanged();
                }
            }
        }

        private int _progressPercent;
        /// <summary>
        /// Gets or sets the progress.
        /// </summary>
        public int ProgressPercent
        {
            get => _progressPercent;
            private set
            {
                if (_progressPercent != value)
                {
                    _progressPercent = value;

                    OnPropertyChanged();
                }
            }
        }

        private string _progressDetails = "Not started";
        /// <summary>
        /// Gets or sets the progress details.
        /// </summary>
        public string ProgressDetails
        {
            get => _progressDetails;
            private set
            {
                if (_progressDetails != value)
                {
                    _progressDetails = value;

                    OnPropertyChanged();
                }
            }
        }

        private TimeSpan _remainingSeconds;
        /// <summary>
        /// Gets or sets the remaining seconds.
        /// </summary>
        public TimeSpan RemainingSeconds
        {
            get => _remainingSeconds;
            private set
            {
                if (_remainingSeconds != value)
                {
                    _remainingSeconds = value;

                    OnPropertyChanged();
                }
            }
        }

        private TimeSpan _elapsedSeconds;
        /// <summary>
        /// Gets or sets the elapsed seconds.
        /// </summary>
        public TimeSpan ElapsedSeconds
        {
            get => _elapsedSeconds;
            private set
            {
                if (_elapsedSeconds != value)
                {
                    _elapsedSeconds = value;

                    OnPropertyChanged();
                }
            }
        }
        #endregion

        /// <summary>
        /// The requested cancel
        /// </summary>
        public bool RequestedCancel { get; private set; }

        /// <summary>
        /// Gets the open log command.
        /// </summary>
        public Command OpenLogCommand { get; }
        /// <summary>
        /// Gets the convert command.
        /// </summary>
        public Command ConvertCommand { get; }
        /// <summary>
        /// Gets the cancel conversion command.
        /// </summary>
        public Command CancelConversionCommand { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can convert.
        /// </summary>
        public abstract bool CanConvert { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoConversionScreenViewModel"/> class.
        /// </summary>
        public VideoConversionScreenViewModel()
        {
            _updateRemainingSecondsTimer.Elapsed += _updateRemainingSecondsTimer_Elapsed;

            OpenLogCommand = new Command((param) =>
            {
                Process.Start("notepad.exe", Logger.LogPath);
            });

            ConvertCommand = new Command((param) =>
            {              
                try
                {
                    ProgressDetails = "Initializing...";
                    ElapsedSeconds = TimeSpan.Zero;
                    IsConverting = true;

                    ConvertInternal(param);
                }
                catch (Exception e)
                {
                    MessageBox.Show("An error has been encountered in the conversion:\n\n" + e.ToString());
                }
            });

            CancelConversionCommand = new Command((param) =>
            {
                ProgressDetails = "Canceling...";
                RequestedCancel = true;
            });
        }

        /// <summary>
        /// Converts the internal.
        /// </summary>
        /// <param name="param">The parameter.</param>
        protected abstract void ConvertInternal(object param);

        /// <summary>
        /// Cancels the request handled.
        /// </summary>
        protected void CancelRequestHandled() => RequestedCancel = false;

        /// <summary>
        /// Sends the progress information.
        /// </summary>
        /// <param name="details">The details.</param>
        /// <param name="percent">The percent.</param>
        /// <param name="elapsedSeconds">The elapsed seconds.</param>
        protected void SendProgressInfo(string details, int percent, double elapsedSeconds)
        {
            var remainingSeconds = percent > 0 ? (elapsedSeconds / ((double)percent / 100d) - elapsedSeconds)  : 0d;

            SendProgressInfo(new ProgressInfo(null, percent, details, remainingSeconds));
        }

        /// <summary>
        /// Sends the progress information.
        /// </summary>
        protected void SendProgressInfo(ProgressInfo progressInfo)
        {
            ProgressPercent = progressInfo.ProgressPercent;
            ProgressDetails = progressInfo.ProgressDetails;
            RemainingSeconds = TimeSpan.FromSeconds(progressInfo.RemainingSeconds);

            if (progressInfo.PreviewImage != null)
            {
                new Action(() =>
                {
                    PreviewImage = WPFUtils.BitmapToImageSource(progressInfo.PreviewImage);

                    progressInfo.PreviewImage.Dispose();
                }).ExecuteOnUIThread();
            }
        }

        /// <summary>
        /// Handles the Elapsed event of the _updateRemainingSecondsTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        private void _updateRemainingSecondsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsConverting)
            {
                if (RemainingSeconds.TotalSeconds > 1d)
                {
                    RemainingSeconds -= TimeSpan.FromSeconds(1d);
                }

                ElapsedSeconds += TimeSpan.FromSeconds(1d);
            }
        }
    }
}
