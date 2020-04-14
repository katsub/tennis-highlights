using System;
using TennisHighlights;

namespace TennisHighlightsGUI.WPF
{
    /// <summary>
    /// The page switch view model
    /// </summary>
    /// <seealso cref="TennisHighlights.ViewModelBase" />
    public class PageSwitchViewModel : ViewModelBase
    {
        //It kinda looks like the typical 1280 / 720 but we need a little more horizontal space for the right column, and we don't
        //need empty space in the bottom
        /// <summary>
        /// The aspect ratio
        /// </summary>
        private const double _aspectRatio = 1280d / 710d;

        private int _width;
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public int Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;

                    OnPropertyChanged();
                }
            }
        }

        private int _height;
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public int Height
        {
            get => _height;
            set
            {             
                if (_height != value)
                {
                    _height = value;

                    var newWidth = (int)Math.Round(_height * _aspectRatio);

                    if (Math.Abs(_width - newWidth) > 3)
                    {
                        _width = newWidth;

                        OnPropertyChanged(nameof(Width));
                    }

                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSwitchViewModel"/> class.
        /// </summary>
        /// <param name="height">The height.</param>
        public PageSwitchViewModel(int height) => Height = height;
    }
}
