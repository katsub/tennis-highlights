using System;
using TennisHighlights;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The rally edit view model
    /// </summary>
    public class RallyEditViewModel : ViewModelBase
    {
        /// <summary>
        /// The action called when data is modified
        /// </summary>
        private readonly Action _onDataModifiedAction;
        /// <summary>
        /// The last frame
        /// </summary>
        private readonly int _lastFrame;
        /// <summary>
        /// The delta frames
        /// </summary>
        private readonly int _deltaFrames;
        /// <summary>
        /// The frame rate
        /// </summary>
        private readonly double _frameRate;
        /// <summary>
        /// The rally edit data
        /// </summary>
        public RallyEditData Data { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        public string OriginalIndex => Data.OriginalIndex;

        /// <summary>
        /// Gets the minimum start.
        /// </summary>
        public int MinStart { get; private set; }

        /// <summary>
        /// Gets the maximum stop.
        /// </summary>
        public int MaxStop { get; private set; }

        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        public int Start
        {
            get => Data.Start;
            set
            {
                if (value != Data.Start)
                {
                    Data.Start = value;
                    UpdateMinStart();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DurationSeconds));
                }
            }
        }

        /// <summary>
        /// Gets or sets the stop.
        /// </summary>
        public int Stop
        {
            get => Data.Stop;
            set
            {
                if (value != Data.Stop)
                {
                    Data.Stop = value;
                    UpdateMaxStop();

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DurationSeconds));
                }
            }
        }

        /// <summary>
        /// Gets the duration seconds.
        /// </summary>
        public TimeSpan DurationSeconds => TimeSpan.FromSeconds((Stop - Start) / _frameRate);

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        public bool IsSelected
        {
            get => Data.IsSelected;
            set
            {
                if (Data.IsSelected != value)
                {
                    Data.IsSelected = value;

                    _onDataModifiedAction();

                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// Updates the max stop.
        /// </summary>
        private void UpdateMaxStop() => MaxStop = (int)Math.Min(_lastFrame, Data.Stop + _deltaFrames);

        private void UpdateMinStart() => MinStart = (int)Math.Max(0, Data.Start - _deltaFrames);

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyEditViewModel" /> class.
        /// </summary>
        /// <param name="rallyEditData">The rally edit data.</param>
        /// <param name="deltaFrames">The delta frames.</param>
        /// <param name="lastFrame">The last frame.</param>
        /// <param name="frameRate">The frame rate.</param>
        /// <param name="onDataModifiedAction">The action called when data is modified.</param>
        public RallyEditViewModel(RallyEditData rallyEditData, int deltaFrames, int lastFrame, double frameRate, Action onDataModifiedAction)
        {
            _frameRate = frameRate;

            _onDataModifiedAction = onDataModifiedAction;

            Data = rallyEditData;

            _deltaFrames = deltaFrames;
            _lastFrame = lastFrame;

            UpdateMinStart();
            UpdateMaxStop();
        }
    }
}
