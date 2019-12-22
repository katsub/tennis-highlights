namespace TennisHighlightsGUI.JoinFiles
{
    /// <summary>
    /// The join file view model.
    /// </summary>
    public class JoinFileViewModel
    {
        /// <summary>
        /// The remove command
        /// </summary>
        public Command RemoveCommand { get; }

        /// <summary>
        /// The join file path
        /// </summary>
        public string JoinFilePath { get; }

        /// <summary>
        /// Initializes a new instance of the class JoinFileViewModel.
        /// </summary>
        /// <param name="parentViewModel">The parent view model.</param>
        /// <param name="path">The path.</param>
        public JoinFileViewModel(JoinFilesViewModel parentViewModel, string path)
        {
            RemoveCommand = new Command((param) => { parentViewModel.Remove(this); });

            JoinFilePath = path;
        }     
    }
}
