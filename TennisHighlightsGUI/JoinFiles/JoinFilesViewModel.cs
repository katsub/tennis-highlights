using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TennisHighlights.Utils;

namespace TennisHighlightsGUI.JoinFiles
{
    /// <summary>
    /// The join files view model
    /// </summary>
    public class JoinFilesViewModel : ViewModelBase
    {
        /// <summary>
        /// The add file command
        /// </summary>
        public Command AddFileCommand { get; }

        /// <summary>
        /// The join files command
        /// </summary>
        public Command JoinFilesCommand { get; }

        private int _progress;
        /// <summary>
        /// The progress
        /// </summary>
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The joined file path
        /// </summary>
        public string JoinedFilePath
        {
            get
            {
                var filePath = FilesToJoin.FirstOrDefault()?.JoinFilePath;

                if (string.IsNullOrEmpty(filePath))
                {
                    return string.Empty;
                }

                var path = Path.GetDirectoryName(filePath);

                return path + "\\joined.mp4";
            }
        }

        private bool _isBusy;

        /// <summary>
        /// True if files can be joined
        /// </summary>
        public bool CanJoinFiles => FilesToJoin.Count > 1 && !_isBusy;

        /// <summary>
        /// Gets the files to join
        /// </summary>
        public ObservableCollection<JoinFileViewModel> FilesToJoin { get; } = new ObservableCollection<JoinFileViewModel>();

        /// <summary>
        /// Initializes a new instance of the class JoinFilesViewModel.
        /// </summary>
        public JoinFilesViewModel()
        {
            FilesToJoin.CollectionChanged += FilesToJoin_CollectionChanged;

            AddFileCommand = new Command((param) =>
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Video files (*.mp4) | *.mp4"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    FilesToJoin.Add(new JoinFileViewModel(this, openFileDialog.FileName));
                }
            });

            JoinFilesCommand = new Command((param) =>
            {
                _isBusy = true;
                OnPropertyChanged(nameof(CanJoinFiles));

                var filesToJoin = FilesToJoin.Select(f => f.JoinFilePath).ToList();

                new Task(() =>
                {
                    Progress = 0;

                    var expectedSize = filesToJoin.Sum(f => new FileInfo(f).Length);

                    while (_isBusy)
                    {
                        if (File.Exists(JoinedFilePath))
                        {
                            try
                            {
                                Progress = (int)Math.Round(Math.Min(100d, 100d * ((double)new FileInfo(JoinedFilePath).Length) / expectedSize));
                            }
                            //The file might be deleted by other tasks, to be recreated
                            catch { }
                        }

                        Task.Delay(200);
                    }

                    Progress = 100;

                }).Start();

                new Task(() =>
                {
                    var error = FFmpegCaller.JoinFiles(JoinedFilePath, filesToJoin);

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "Error");
                    }

                    _isBusy = false;
                    OnPropertyChanged(nameof(CanJoinFiles));
                }).Start();
            });
        }

        /// <summary>
        /// Handles the CollectionChanged event of the FilesToJoin control
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void FilesToJoin_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(JoinedFilePath));
            OnPropertyChanged(nameof(CanJoinFiles));
        }

        /// <summary>
        /// Removes a file from the files to join
        /// </summary>
        /// <param name="fileToRemove">The file to remove</param>
        public void Remove(JoinFileViewModel fileToRemove) => FilesToJoin.Remove(fileToRemove);
    }
}
