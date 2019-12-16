using System.Windows;
using System.Windows.Controls;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : UserControl
    {
        /// <summary>
        /// Gets the view model.
        /// </summary>
        public MainViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow(MainViewModel mainVM = null)
        {
            ViewModel = mainVM ?? new MainViewModel();

            DataContext = ViewModel;

            InitializeComponent();
        }

        /// <summary>
        /// Handles the Closing event of the MainWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        public void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) => ViewModel.OnClosing();

        /// <summary>
        /// Handles the MouseDown event of the Grid control
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var location = this.TranslatePoint(new Point(0, 0), sender as UIElement);

            AlignmentLine.Margin = new System.Windows.Thickness(0, e.GetPosition(this).Y + location.Y, 0, 0);
        }
    }
}
