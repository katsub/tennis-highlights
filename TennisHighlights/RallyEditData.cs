using System.Xml.Linq;

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
        /// Initializes a new instance of the <see cref="RallyEditData"/> class.
        /// </summary>
        /// <param name="originalIndex">Index of the original.</param>
        public RallyEditData(string originalIndex) => OriginalIndex = originalIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyEditData" /> class.
        /// </summary>
        /// <param name="serializedRallyEditData">The serialized rally edit data.</param>
        public RallyEditData(XElement serializedRallyEditData) : this(serializedRallyEditData.GetStringAttribute(XRallyKeys.OriginalIndex))
        {
            Start = serializedRallyEditData.GetIntAttribute(XRallyKeys.Start, -1);
            Stop = serializedRallyEditData.GetIntAttribute(XRallyKeys.Stop, -1);
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
