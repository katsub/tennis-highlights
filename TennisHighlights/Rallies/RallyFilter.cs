using Accord.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using TennisHighlights.Utils;

namespace TennisHighlights.Rallies
{
    /// <summary>
    /// The rally filter
    /// </summary>
    public static class RallyFilter
    {
        /// <summary>
        /// Sorts rallies by distance and duration
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        public static IOrderedEnumerable<(double score, Rally rally)> SortByDistanceAndDuration(List<Rally> rallies)
        {
            var maxDuration = (double)rallies.Max(r => r.DurationInFrames);
            var maxDistance = rallies.Max(r => r.GetBallTotalTravelDistance());

            var scoredRallies = new List<(double score, Rally rally)>();

            foreach (var rally in rallies)
            {
                var durationScore = rally.DurationInFrames / maxDuration;
                var distanceScore = rally.GetBallTotalTravelDistance() / maxDistance;

                scoredRallies.Add((durationScore + distanceScore, rally));
            }

            return scoredRallies.OrderByDescending(r => r.score);
        }

        /// <summary>
        /// Scores the rallies by duration.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        public static (SortedClusters clusters, Dictionary<Rally, RallyFilterInfo> rallies) ScoreByDuration(Dictionary<Rally, RallyFilterInfo> rallies, int numberOfClusters)
        {
            SortedClusters sortedClusters = null;

            //Noise tends to be shorter in duration than actual points, so we cut the shorter points
            //This will cut some shorter points but also filter a lot of noise. Good if video has a lot of time with noone playing
            if (rallies.Count > numberOfClusters)
            {
                //Hypothesis: there's a super-short duration noise cluster, a short point duration cluster and a long point duration cluster
                var kMeans = new KMeans(numberOfClusters);
                Accord.Math.Random.Generator.Seed = 0;
                var clusterDurations = kMeans.Learn(rallies.Select(r => new double[] { r.Key.DurationInFrames }).ToArray());
                var maxDuration = (double)rallies.Max(r => r.Key.DurationInFrames);

                foreach (var rally in rallies)
                {
                    rally.Value.DurationCluster = clusterDurations.Decide(new double[] { rally.Key.DurationInFrames });
                    rally.Value.DurationScore = rally.Key.DurationInFrames / maxDuration;
                }

                var clusters = new List<KMeansClusterCollection.KMeansCluster>();

                for (int i = 0; i < clusterDurations.Clusters.Length; i++)
                {
                    clusters.Add(clusterDurations.Clusters[i]);
                }

                sortedClusters = new SortedClusters(clusters);
            }
            else
            {
                Logger.Log(LogType.Warning, "Couldn't filter rallies by duration because at least " + (numberOfClusters + 1) + " rallies are needed for filtering");
            }

            return (sortedClusters, rallies);
        }

        /// <summary>
        /// Scores the rallies by duration.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        /// <param name="numberOfClusters">The number of clusters.</param>
        /// <returns></returns>
        public static (SortedClusters clusters, Dictionary<Rally, RallyFilterInfo> rallies) ScoreByDetectedFrames(Dictionary<Rally, RallyFilterInfo> rallies, int numberOfClusters)
        {
            SortedClusters sortedClusters = null;

            //Noise tends to be shorter in duration than actual points, so we cut the shorter points
            //This will cut some shorter points but also filter a lot of noise. Good if video has a lot of time with noone playing
            if (rallies.Count > numberOfClusters)
            {
                //Hypothesis: there's a almost-no-detected-frames noise cluster, a few-detected frames duration cluster and a lots-of-detected frames cluster
                var clusterDetectedFrames = new KMeans(numberOfClusters).Learn(rallies.Select(r => new double[] { r.Key.DetectedFramesPercentage }).ToArray());

                var maxDetectedFrames = (double)rallies.Max(r => r.Key.DetectedFramesPercentage);

                foreach (var rally in rallies)
                {
                    rally.Value.DurationCluster = clusterDetectedFrames.Decide(new double[] { rally.Key.DurationInFrames });
                    rally.Value.DurationScore = rally.Key.DetectedFramesPercentage / maxDetectedFrames;
                }

                var clusters = new List<KMeansClusterCollection.KMeansCluster>();

                for (int i = 0; i < clusterDetectedFrames.Clusters.Length; i++)
                {
                    clusters.Add(clusterDetectedFrames.Clusters[i]);
                }

                sortedClusters = new SortedClusters(clusters);
            }
            else
            {
                Logger.Log(LogType.Warning, "Couldn't filter rallies by detected frames because at least " + (numberOfClusters + 1) + " rallies are needed for filtering");
            }

            return (sortedClusters, rallies);
        }

        /// <summary>
        /// Filters the specified rallies.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        /// <param name="type">The type.</param>
        public static List<Rally> Filter(List<Rally> rallies, RallyFilteringType type)
        {
            //The safest filter, very rarely deletes true rallies since only noise can stay on a single axis (purely vertical or horizontal)
            //because playing an entire rally straight is extremely hard and unlikely, and playing horizontal isn't possible since the ball must cross the court
            var filteredRallies = rallies.Where(r => r.ActiveAreaRoundness > 0.2 && r.ActiveAreaRoundness < 6);

            //Noise tends to have more undetected frames. This filters out a decent amount of real points, but combined with
            //the duration filter, any points left are extremely likely to be true.
            if (rallies.Count > 2)
            {
                var clusterUndetectedFrames = new KMeans(2).Learn(rallies.Select(r => new double[] { r.DetectedFramesPercentage }).ToArray());
                var moreDetectedFramesCluster = clusterUndetectedFrames.Centroids[0][0] > clusterUndetectedFrames.Centroids[1][0] ? 0 : 1;

                filteredRallies = filteredRallies.Where(r => clusterUndetectedFrames.Decide(new double[] { r.DetectedFramesPercentage }) == moreDetectedFramesCluster);
            }
            else
            {
                Logger.Log(LogType.Warning, "Couldn't filter rallies by undetected frames because at least 3 rallies are needed for filtering");
            }

            return filteredRallies.ToList();
        }


        public static string ChosenRalliesIndexes(List<Rally> originalRalllies, List<Rally> chosenRallies)
        {
            return string.Join("\n", chosenRallies.Select(r => originalRalllies.IndexOf(r)));
        }

        /// <summary>
        /// Filters the rallies.
        /// </summary>
        /// <param name="rallies">The rallies.</param>
        /// <param name="strictness">The strictness.</param>
        public static List<Rally> FilterRallies(List<Rally> rallies, int strictness)
        {
            return null;
        }

        public static string ClassifyRallies(List<Rally> trueRallies, List<Rally> rallies)
        {
            var trueRalliesCount = rallies.Where(r => trueRallies.Contains(r)).Count();

            return ("True rallies: " + trueRalliesCount + ", false rallies: " + (rallies.Count - trueRalliesCount) + ". True (" + 100d * Math.Round((double)trueRalliesCount / rallies.Count, 3) + "%)");
        }

    }
}
