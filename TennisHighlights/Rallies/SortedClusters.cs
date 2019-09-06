using Accord.MachineLearning;
using System.Collections.Generic;
using System.Linq;

namespace TennisHighlights.Rallies
{
    /// <summary>
    /// The cluster
    /// </summary>
    public class Cluster
    {
        /// <summary>
        /// Gets the centroid.
        /// </summary>
        public double[] Centroid { get; }
        /// <summary>
        /// Gets the error.
        /// </summary>
        public double Error { get; }
        /// <summary>
        /// Gets the label.
        /// </summary>
        public int Label { get; }
        /// <summary>
        /// Gets the proportion.
        /// </summary>
        public double Proportion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cluster" /> class.
        /// </summary>
        /// <param name="centroid">The centroid.</param>
        /// <param name="error">The error.</param>
        /// <param name="label">The label.</param>
        /// <param name="proportion">The proportion.</param>
        public Cluster(double[] centroid, double error, int label, double proportion)
        {
            Centroid = centroid;
            Error = error;
            Label = label;
            Proportion = proportion;
        }
    }

    /// <summary>
    /// The clusters sorted by ascending centroid mean value
    /// </summary>
    public class SortedClusters
    {
        /// <summary>
        /// Gets the clusters.
        /// </summary>
        public IReadOnlyList<KMeansClusterCollection.KMeansCluster> Clusters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedClusters"/> class.
        /// </summary>
        /// <param name="clusters">The clusters.</param>
        public SortedClusters(List<KMeansClusterCollection.KMeansCluster> clusters)
        {
            Clusters = clusters.OrderBy(c => c.Centroid.Sum() / c.Centroid.Length).ToList().AsReadOnly();
        }
    }
}
