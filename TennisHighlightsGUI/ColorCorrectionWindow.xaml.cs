using System.Windows;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// Logique d'interaction pour ColorCorrectionWindow.xaml
    /// </summary>
    public partial class ColorCorrectionWindow : Window
    {
        /// <summary>
        /// Gets the color correction view model.
        /// </summary>
        public ColorCorrectionViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorCorrectionWindow" /> class.
        /// </summary>
        /// <param name="mainVM">THe main view model</param>
        public ColorCorrectionWindow(MainViewModel mainVM)
        {
            ViewModel = new ColorCorrectionViewModel(mainVM);

            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
