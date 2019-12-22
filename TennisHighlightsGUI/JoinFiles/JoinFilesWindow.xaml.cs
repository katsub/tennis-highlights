using System.Windows;

namespace TennisHighlightsGUI.JoinFiles
{
    /// <summary>
    /// The join files window interaction logic
    /// </summary>
    public partial class JoinFilesWindow : Window
    {
        /// <summary>
        /// Gets the join files view model.
        /// </summary>
        public JoinFilesViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinFilesWindow" /> class.
        /// </summary>
        public JoinFilesWindow()
        {
            ViewModel = new JoinFilesViewModel();

            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
