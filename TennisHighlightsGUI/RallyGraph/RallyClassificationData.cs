using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights;
using TennisHighlights.Rallies;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The rally class
    /// </summary>
    public enum RallyClass
    {
        True,
        False,
        Partial,
        Unclassified
    }

    /// <summary>
    /// The classified rally
    /// </summary>
    public class ClassifiedRally
    {
        /// <summary>
        /// Gets the class.
        /// </summary>
        public RallyClass Class { get; }
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
        /// <param name="rallyClass">The rally class.</param>
        public ClassifiedRally(int index, Rally rally, RallyClass rallyClass)
        {
            Index = index;
            Rally = rally;
            Class = rallyClass;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => $"Index: {Index.ToString("D3")}, Class: {Class}, Rally: {Rally.ToString()}";
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
        public RallyClassificationData(string rallyClassificationFile, List<Rally> rallies)
        {
            //Parse rallies from file
            var lines = rallyClassificationFile.Split('\n');

            var possibleClasses = Enum.GetValues(typeof(RallyClass)).Cast<RallyClass>().ToList();

            foreach (var line in lines)
            {
                var splitLine = line.Replace("\r","").Split(' ').Where(s => s != string.Empty).ToList();

                if (splitLine.Count == 2 && int.TryParse(splitLine[0], out var index))
                {
                    if (index >= rallies.Count)
                    {
                        Logger.Log(LogType.Warning, "Rally classification parsing will ignore index " + index + " bigger than or equal to rallies.Count: " + rallies.Count);
                        continue;
                    }

                    RallyClass? rallyClass = null;
                    var classToParse = splitLine[1].ToLower();
                   
                    foreach (var possibleClass in possibleClasses)
                    {
                        if (classToParse == possibleClass.ToString().ToLower())
                        {
                            rallyClass = possibleClass;

                            break;
                        }
                    }

                    if (rallyClass != null)
                    {
                        Rallies.Add(index, new ClassifiedRally(index, rallies[index], rallyClass.Value));
                    }
                    else
                    {
                        Logger.Log(LogType.Warning, "Rally classification parsing will ignore index " + index + " because it could not parse its class from string: " + classToParse);
                        continue;
                    }
                }
            }

            //Add remaining rallies as "unclassified"
            foreach (var rally in rallies.Where(r => !Rallies.ContainsKey(rallies.IndexOf(r)))
                                         .ToDictionary(r => rallies.IndexOf(r)
                                                       , r => new ClassifiedRally(rallies.IndexOf(r), r, RallyClass.Unclassified)))
            {
                Rallies.Add(rally.Key, rally.Value);
            }
        }
    }
}
