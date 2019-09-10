using System.Windows.Controls;
using System.Windows.Threading;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// Interação lógica para RallySelectionView.xam
    /// </summary>
    public partial class RallySelectionView : UserControl
    {
        /// <summary>
        /// The timer
        /// </summary>
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Gets the rally selection view model.
        /// </summary>
        public RallySelectionViewModel RallySelectionViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallySelectionView"/> class.
        /// </summary>
        /// <param name="mainVM">The main vm.</param>
        public RallySelectionView(RallySelectionViewModel rallyVM)
        {
            RallySelectionViewModel = rallyVM;

            DataContext = RallySelectionViewModel;

            InitializeComponent();

            RallySelectionViewModel.SetPlayer(mePlayer, multiSlider);                 
        }

        /// <summary>
        /// Handles the PreviewMouseDown event of the WitMultiRangeSlider control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.BeganDraggingStopSlider();
        /// <summary>
        /// Handles the PreviewMouseUp event of the WitMultiRangeSlider control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.StoppedDraggingStopSlider();
        /// <summary>
        /// Handles the PreviewMouseDown event of the WitMultiRangeSliderItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSliderItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.BeganDraggingStartSlider();
        /// <summary>
        /// Handles the PreviewMouseUp event of the WitMultiRangeSliderItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSliderItem_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.StoppedDraggingStartSlider();
        /// <summary>
        /// Handles the 1 event of the WitMultiRangeSliderItem_PreviewMouseDown control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSliderItem_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.BeganDraggingCurrentSlider();
        /// <summary>
        /// Handles the 1 event of the WitMultiRangeSliderItem_PreviewMouseUp control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void WitMultiRangeSliderItem_PreviewMouseUp_1(object sender, System.Windows.Input.MouseButtonEventArgs e) => RallySelectionViewModel.StoppedDraggingCurrentSlider();
    }
}
