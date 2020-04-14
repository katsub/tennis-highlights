using TennisHighlights;

namespace TennisHighlightsGUI.MultipleFiles
{
    /// <summary>
    /// The single file view model.
    /// </summary>
    public class SingleFileViewModel : ViewModelBase
    {
        /// <summary>
        /// The file path
        /// </summary>
        public string FilePath { get; }

        private string _status;
        /// <summary>
        /// The status
        /// </summary>
        public string Status 
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the class SingleFileViewModel.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="status">The status.</param>
        public SingleFileViewModel(string path, string status)
        {
            FilePath = path;
            Status = status;
        }
    }
}
