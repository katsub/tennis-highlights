using System.Windows;

namespace TennisHighlightsGUI.MultipleFiles
{
    /// <summary>
    /// The multiple files window interaction logic
    /// </summary>
    public partial class MultipleFilesWindow : Window
    {
        /// <summary>
        /// Gets the multiple files view model.
        /// </summary>
        public MultipleFilesViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleFilesWindow" /> class.
        /// </summary>
        /// <param name="mainVM">THe main view model</param>
        public MultipleFilesWindow(MainViewModel mainVM)
        {
            ViewModel = new MultipleFilesViewModel(mainVM);

            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
