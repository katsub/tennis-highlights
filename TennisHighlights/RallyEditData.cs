using System.Xml.Linq;
using TennisHighlights.ImageProcessing.PlayerMoves;
using TennisHighlights.Utils.PoseEstimation;

namespace TennisHighlights
{
    /// <summary>
    /// The x rally serialization keys
    /// </summary>
    public class XRallyKeys
    {
        public const string OriginalIndex = "OriginalIndex";
        public const string Start = "Start";
        public const string Stop = "Stop";
        public const string IsSelected = "IsSelected";
    }

    /// <summary>
    /// The rally edit data
    /// </summary>
    public class RallyEditData
    {
        /// <summary>
        /// The rally original index
        /// </summary>
        public string OriginalIndex { get; }
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        public int Start { get; set; }
        /// <summary>
        /// Gets or sets the stop.
        /// </summary>
        public int Stop { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        public bool IsSelected { get; set; }
        /// <summary>
        /// Gets the move stats.
        /// </summary>
        public MoveStats MoveStats { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyEditData"/> class.
        /// </summary>
        /// <param name="originalIndex">Index of the original.</param>
        /// <param name="start">The start.</param>
        /// <param name="stop">The stop.</param>
        /// <param name="playerMovesData">The player moves data.</param>
        public RallyEditData(string originalIndex, int start, int stop, PlayerMovesData playerMovesData) : this(originalIndex, start, stop)
        {
            SetMoveStats(playerMovesData);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyEditData"/> class.
        /// </summary>
        /// <param name="originalIndex">Index of the original.</param>
        /// <param name="start">The start.</param>
        /// <param name="stop">The stop.</param>
        public RallyEditData(string originalIndex, int start, int stop)
        {
            OriginalIndex = originalIndex;
            Start = start;
            Stop = stop;
        }

        /// <summary>
        /// Sets the move stats.
        /// </summary>
        /// <param name="playerMovesData">The player moves data.</param>
        public void SetMoveStats(PlayerMovesData playerMovesData)
        {
            MoveStats = playerMovesData != null ? new MoveStats(playerMovesData) : null;

            MoveStats.Update(Start, Stop);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyEditData"/> class.
        /// </summary>
        /// <param name="serializedRallyEditData">The serialized rally edit data.</param>
        /// <param name="playerMovesData">The player moves data.</param>
        public RallyEditData(XElement serializedRallyEditData, PlayerMovesData playerMovesData) 
               : this(serializedRallyEditData.GetStringAttribute(XRallyKeys.OriginalIndex),
                      serializedRallyEditData.GetIntAttribute(XRallyKeys.Start, -1),
                      serializedRallyEditData.GetIntAttribute(XRallyKeys.Stop, -1), playerMovesData)
        {
            IsSelected = serializedRallyEditData.GetBoolAttribute(XRallyKeys.IsSelected, false);
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        public XElement Serialize()
        {
            var xRally = new XElement(LogKeys.Rally);

            xRally.SetAttributeValue(XRallyKeys.OriginalIndex, OriginalIndex);
            xRally.SetAttributeValue(XRallyKeys.Start, Start);
            xRally.SetAttributeValue(XRallyKeys.Stop, Stop);
            xRally.SetAttributeValue(XRallyKeys.IsSelected, IsSelected);

            return xRally;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Id: {OriginalIndex}, Start: {Start}, Stop: {Stop}, Selected: {IsSelected}";
    }
}
