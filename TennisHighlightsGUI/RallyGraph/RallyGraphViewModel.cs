using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TennisHighlights.Rallies;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The axis data
    /// </summary>
    public enum AxisData
    {
        [Description("Duration (frames)")]
        Duration,
        [Description("Traveled distance")]
        TraveledDistance,
        [Description("Detected balls/frame %")]
        DetectedFramesPercentage,
        [Description("Number of arcs")]
        NumberOfArcs,
    }

    /// <summary>
    /// The rally graph view model
    /// </summary>
    public class RallyGraphViewModel : ViewModelBase
    {
        /// <summary>
        /// The rally points
        /// </summary>
        private List<(double x, double y, ClassifiedRally rally)> _rallyPoints;

        /// <summary>
        /// The axis data types
        /// </summary>
        public IReadOnlyList<AxisData> AxisDataTypes { get; } = new List<AxisData>(Enum.GetValues(typeof(AxisData)).Cast<AxisData>()).AsReadOnly();

        /// <summary>
        /// The rally data
        /// </summary>
        private readonly RallyClassificationData _rallyData;

        private string _pointDetails;
        /// <summary>
        /// Gets or sets the point details.
        /// </summary>
        public string PointDetails
        {
            get => _pointDetails;
            set
            {
                if (_pointDetails != value)
                {
                    _pointDetails = value;

                    OnPropertyChanged();
                }
            }
        }

        private AxisData _yAxisData = AxisData.DetectedFramesPercentage;
        /// <summary>
        /// Gets or sets the data of the Y axis.
        /// </summary>
        public AxisData YAxisData
        {
            get => _yAxisData;
            set
            {
                if (_yAxisData != value)
                {
                    _yAxisData = value;

                    RebuildPlot();
                    OnPropertyChanged();
                }
            }
        }

        private AxisData _xAxisData = AxisData.Duration;
        /// <summary>
        /// Gets or sets the data of the X axis.
        /// </summary>
        public AxisData XAxisData
        {
            get => _xAxisData;
            set
            {
                if (_xAxisData != value)
                {
                    _xAxisData = value;

                    RebuildPlot();
                    OnPropertyChanged();
                }
            }
        }

        private PlotModel _plotModel;
        /// <summary>
        /// Gets or sets the plot model.
        /// </summary>
        public PlotModel PlotModel
        {
            get => _plotModel;
            set
            {
                _plotModel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyGraphViewModel"/> class.
        /// </summary>
        public RallyGraphViewModel(RallyClassificationData rallyData)
        {
            _rallyData = rallyData;

            RebuildPlot();
        }
        
        /// <summary>
        /// Gets the rally value.
        /// </summary>
        /// <param name="rally">The rally.</param>
        /// <param name="dataType">Type of the data.</param>
        private double GetRallyValue(Rally rally, AxisData dataType)
        {
            switch (dataType)
            {
                case AxisData.Duration:
                    return rally.DurationInFrames;
                case AxisData.DetectedFramesPercentage:
                    return rally.DetectedFramesPercentage;
                case AxisData.NumberOfArcs:
                    return rally.Arcs.Count;
                case AxisData.TraveledDistance:
                    return rally.GetBallTotalTravelDistance();
                default:
                    return 0d;
            }
        }

        /// <summary>
        /// Gets the rally point.
        /// </summary>
        /// <param name="classifiedRally">The rally.</param>
        private (double x, double y, ClassifiedRally rally) GetRallyPoint(ClassifiedRally classifiedRally)
        {
            return (GetRallyValue(classifiedRally.Rally, XAxisData), GetRallyValue(classifiedRally.Rally, YAxisData), classifiedRally);
        }

        /// <summary>
        /// Rebuilds the plot.
        /// </summary>
        private void RebuildPlot()
        { 
            if (PlotModel != null)
            {
                foreach (var series in PlotModel.Series)
                {
                    series.MouseDown -= Series_MouseDown;
                }
            }

            // Create the plot model
            var model = new PlotModel();

            _rallyPoints = _rallyData.Rallies.Select(r => GetRallyPoint(r.Value)).ToList();
              
            var maxX = _rallyPoints.Max(r => r.x);
            var xMargin = maxX * 0.05;
            maxX += xMargin;
            var minX = _rallyPoints.Min(r => r.x) - xMargin;
            var maxY = _rallyPoints.Max(r => r.y);
            var yMargin = maxY * 0.05;
            maxY += yMargin;
            var minY = _rallyPoints.Min(r => r.y) - yMargin;

            // Create two line series (markers are hidden by default)
            var trueRallies = new ScatterSeries { Title = "True", MarkerFill = OxyColors.Green, MarkerType = MarkerType.Circle };
            var falseRallies = new ScatterSeries { Title = "False", MarkerFill = OxyColors.Red, MarkerType = MarkerType.Triangle };
            var partialRallies = new ScatterSeries { Title = "Partial", MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle };
            var unclassifiedRallies = new ScatterSeries { Title = "Unclassified", MarkerFill = OxyColors.Black, MarkerType = MarkerType.Square };

            foreach (var rally in _rallyPoints.Where(r => r.rally.Class == RallyClass.True))
            {
                trueRallies.Points.Add(new ScatterPoint(rally.x, rally.y));
            }

            foreach (var rally in _rallyPoints.Where(r => r.rally.Class == RallyClass.False))
            {
                falseRallies.Points.Add(new ScatterPoint(rally.x, rally.y));
            }

            foreach (var rally in _rallyPoints.Where(r => r.rally.Class == RallyClass.Partial))
            {
                partialRallies.Points.Add(new ScatterPoint(rally.x, rally.y));
            }

            foreach (var rally in _rallyPoints.Where(r => r.rally.Class == RallyClass.Unclassified))
            {
                unclassifiedRallies.Points.Add(new ScatterPoint(rally.x, rally.y));
            }

            // Add the series to the plot model
            model.Series.Add(trueRallies);
            model.Series.Add(falseRallies);
            model.Series.Add(partialRallies);
            model.Series.Add(unclassifiedRallies);

            foreach (var series in model.Series)
            {
                series.MouseDown += Series_MouseDown;
            }

            // Axes are created automatically if they are not defined
            model.Axes.Add(new LinearAxis()
            {
                Title = XAxisData.GetDescription(),
                Position = AxisPosition.Bottom,
                Maximum = maxX,
                Minimum = minX
            });

            model.Axes.Add(new LinearAxis()
            {
                Title = YAxisData.GetDescription(),
                Position = AxisPosition.Left,
                Maximum = maxY,
                Minimum = minY
            });

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            PlotModel = model;
        }

        /// <summary>
        /// Handles the MouseDown event of the Series control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="OxyMouseDownEventArgs"/> instance containing the event data.</param>
        private void Series_MouseDown(object sender, OxyMouseDownEventArgs e)
        {
            var position = (sender as ScatterSeries).InverseTransform(e.Position);

            var (x, y, rally) = _rallyPoints.OrderBy(p => Math.Pow(p.x - position.X,2) + Math.Pow(p.y - position.Y, 2)).First();

            PointDetails = rally.ToString();
        }
    }
}
