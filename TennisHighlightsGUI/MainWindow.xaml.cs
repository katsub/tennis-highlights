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
    }
}
