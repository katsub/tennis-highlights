using System.Windows;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The rally graph window interaction logic
    /// </summary>
    public partial class RallyGraphWindow : Window
    {
        /// <summary>
        /// Gets the rally graph view model.
        /// </summary>
        public RallyGraphViewModel RallyGraphViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyGraphWindow" /> class.
        /// </summary>
        /// <param name="rallyGraphViewModel">The rally graph view model.</param>
        public RallyGraphWindow(RallyGraphViewModel rallyGraphViewModel)
        {
            RallyGraphViewModel = rallyGraphViewModel;

            DataContext = RallyGraphViewModel;

            InitializeComponent();
        }
    }
}
