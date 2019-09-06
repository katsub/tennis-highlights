using System;
using System.Windows;
using System.Windows.Controls;

namespace TennisHighlightsGUI.WPF
{
    /// <summary>
    /// Logical interaction for ProgressBar.xam
    /// </summary>
    public partial class ProgressBar : UserControl
    {
        /// <summary>
        /// The progress details property
        /// </summary>
        public static readonly DependencyProperty ProgressDetailsProperty = DependencyProperty.Register(nameof(ProgressDetails), typeof(string), typeof(ProgressBar), new PropertyMetadata(String.Empty, ProgressDetailsPropertyChanged));
        /// <summary>
        /// The progress percent property
        /// </summary>
        public static readonly DependencyProperty ProgressPercentProperty = DependencyProperty.Register(nameof(ProgressPercent), typeof(int), typeof(ProgressBar), new PropertyMetadata(0, ProgressPercentPropertyChanged));
        /// <summary>
        /// The remaining seconds property
        /// </summary>
        public static readonly DependencyProperty RemainingSecondsProperty = DependencyProperty.Register(nameof(RemainingSeconds), typeof(TimeSpan), typeof(ProgressBar), new PropertyMetadata(TimeSpan.FromSeconds(0d), RemainingSecondsPropertyChanged));
        /// <summary>
        /// The elapsed seconds property
        /// </summary>
        public static readonly DependencyProperty ElapsedSecondsProperty = DependencyProperty.Register(nameof(ElapsedSeconds), typeof(TimeSpan), typeof(ProgressBar), new PropertyMetadata(TimeSpan.FromSeconds(0d), ElapsedSecondsPropertyChanged));
        /// <summary>
        /// Gets or sets the progress details.
        /// </summary>
        public string ProgressDetails
        {
            get => (string)GetValue(ProgressDetailsProperty);
            set => SetValue(ProgressDetailsProperty, value);
        }
        /// <summary>
        /// Gets or sets the progress percent.
        /// </summary>
        public int ProgressPercent
        {
            get => (int)GetValue(ProgressPercentProperty);
            set => SetValue(ProgressPercentProperty, value);
        }
        /// <summary>
        /// Gets or sets the remaining seconds.
        /// </summary>
        public TimeSpan RemainingSeconds
        {
            get => (TimeSpan)GetValue(RemainingSecondsProperty);
            set => SetValue(RemainingSecondsProperty, value);
        }
        /// <summary>
        /// Gets or sets the elapsed seconds.
        /// </summary>
        public TimeSpan ElapsedSeconds
        {
            get => (TimeSpan)GetValue(ElapsedSecondsProperty);
            set => SetValue(ElapsedSecondsProperty, value);
        }

        /// <summary>
        /// Progresses the details property changed.
        /// </summary>
        /// <param name="progressDetails">The progress details.</param>
        private void ProgressDetailsPropertyChanged(string progressDetails) { }
        /// <summary>
        /// Progresses the details property changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ProgressDetailsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ProgressBar)d).ProgressDetailsPropertyChanged((string)e.NewValue);
        /// <summary>
        /// Progresses the percent property changed.
        /// </summary>
        /// <param name="progressPercent">The progress percent.</param>
        private void ProgressPercentPropertyChanged(int progressPercent) { }
        /// <summary>
        /// Progresses the percent property changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ProgressPercentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ProgressBar)d).ProgressPercentPropertyChanged((int)e.NewValue);
        /// <summary>
        /// Remainings the seconds property changed.
        /// </summary>
        /// <param name="remainingSecond">The remaining second.</param>
        private void RemainingSecondsPropertyChanged(TimeSpan remainingSecond) { }
        /// <summary>
        /// Progresses the remaining seconds property changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void RemainingSecondsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ProgressBar)d).RemainingSecondsPropertyChanged((TimeSpan)e.NewValue);
        /// <summary>
        /// Handles the elapsed second property changed event.
        /// </summary>
        /// <param name="remainingSecond">The remaining second.</param>
        private void ElapsedSecondsPropertyChanged(TimeSpan remainingSecond) { }
        /// <summary>
        /// Progresses the elapsed seconds property changed.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ElapsedSecondsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ProgressBar)d).ElapsedSecondsPropertyChanged((TimeSpan)e.NewValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        public ProgressBar() => InitializeComponent();
    }
}
