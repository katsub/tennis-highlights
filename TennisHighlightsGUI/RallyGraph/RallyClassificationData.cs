using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights;
using TennisHighlights.Rallies;
using TennisHighlights.Utils;

namespace TennisHighlightsGUI
{  
    /// <summary>
    /// The classified rally
    /// </summary>
    public class ClassifiedRally
    {
        /// <summary>
        /// Gets a value indicating whether [was chosen].
        /// </summary>
        public bool WasChosen { get; }
        /// <summary>
        /// Gets the rally.
        /// </summary>
        public Rally Rally { get; }
        /// <summary>
        /// Gets the index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassifiedRally"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="rally">The rally.</param>
        public ClassifiedRally(int index, Rally rally, bool wasChosen)
        {
            Index = index;
            Rally = rally;
            WasChosen = wasChosen;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Index: {Index.ToString("D3")}, Chosen: {WasChosen}, Rally: {Rally.ToString()}";
    }

    /// <summary>
    /// The rally classification data
    /// </summary>
    public class RallyClassificationData
    {
        /// <summary>
        /// Gets the rallies.
        /// </summary>
        public Dictionary<int, ClassifiedRally> Rallies { get; } = new Dictionary<int, ClassifiedRally>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyClassificationData"/> class.
        /// </summary>
        /// <param name="rallyClassificationFile">The rally classification file.</param>
        /// <param name="rallies">The rallies.</param>
        public RallyClassificationData(List<(Rally rally, bool wasChosen)> rallies)
        {
            Rallies = rallies.ToDictionary(r => rallies.IndexOf(r), r => new ClassifiedRally(rallies.IndexOf(r), r.rally, r.wasChosen));
        }
    }
}
