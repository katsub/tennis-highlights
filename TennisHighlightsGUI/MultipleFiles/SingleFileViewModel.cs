namespace TennisHighlightsGUI.MultipleFiles
{
    /// <summary>
    /// The single file view model.
    /// </summary>
    public class SingleFileViewModel
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Initializes a new instance of the class SingleFileViewModel.
        /// </summary>
        /// <param name="path">The path.</param>
        public SingleFileViewModel(string path) => FilePath = path;
    }
}
