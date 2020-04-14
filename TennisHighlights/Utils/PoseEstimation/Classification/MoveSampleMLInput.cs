using Microsoft.ML.Data;
using System;
using System.IO;
using System.Linq;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.Utils.PoseEstimation.Classification
{
    /// <summary>
    /// The move sample ML.Net input
    /// </summary>
    public class MoveSampleMLInput
    {
        /// <summary>
        /// Gets or sets the move label. 3 categories are possible: backhand, forehand or serve
        /// </summary>
        [ColumnName("Label")]
        [KeyType(3)]
        public uint MoveLabel { get; set; }

        /// <summary>
        /// Gets or sets the keypoints. Those are the first 15 keypoints of the MPI model, listed in the following order: x0,y0,x1,y1,x2,y2,...
        /// We consider the moves to have 100 frames, and we get a frame each 5 frames, so we have 20 frames per sample. So we have 15x2 keypoint coordinates
        /// for 20 frames, which makes a total of 600 inputs
        /// </summary>
        [VectorType(40)]
        [ColumnName("Features")]
        public float[] Keypoints { get; set; } = new float[40];

        /// <summary>
        /// The used keypoints
        /// </summary>
        private static int[] _usedKeypoints = new int[] { 7 };

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveSampleMLInput"/> class.
        /// </summary>
        public MoveSampleMLInput() { }

        /// <summary>
        /// Converts to csv.
        /// </summary>
        public string ToCSV(string fileName)
        {
            var csv = "";

            for (int i = 0; i < Keypoints.Length; i++)
            {
                if (i % (2 * _usedKeypoints.Length) == 0 && i > 0)
                {
                    csv += Environment.NewLine;
                }

                csv += Keypoints[i].ToString() + " ";
            }

            try
            {
                var folder = "C:\\Users\\diego\\Downloads\\";

                File.WriteAllText(folder + fileName + ".csv", csv);
            }
            catch
            {

            }

            return csv;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveSampleMLInput"/> class.
        /// </summary>
        /// <param name="features">The features.</param>
        public MoveSampleMLInput(float[] features)
        {
            var j = 0;

            for (int i = 0; i < 20; i++)
            {
                foreach (var usedKeypoint in _usedKeypoints.OrderBy(k => k))
                {
                    Keypoints[j] = features[2 * usedKeypoint + 30 * i];
                    Keypoints[j + 1] = features[2 *usedKeypoint + 1 + 30 * i];

                    j += 2;
                }
            }

            var bodyXPerframe = new float[20];

            for (int i = 0; i < features.Length; i++)
            {
                if (i % 30 == 2)
                {
                    var frameIndex = (int)Math.Round((double)i / 30);

                    bodyXPerframe[frameIndex] = features[i];
                }
            }

            NormalizeFeatures(bodyXPerframe);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveSampleMLInput"/> class.
        /// </summary>
        /// <param name="moveSample">The move sample.</param>
        public MoveSampleMLInput(MoveSample moveSample)
        {
            MoveLabel = /*((MoveLabel)moveSample.MoveLabel).ToString(); */(uint)moveSample.MoveLabel;

            var j = 0;

            foreach (var frame in moveSample.FrameKeypoints)
            {
                var i = 0;

                foreach (var keypoint in frame.Keypoints)
                {
                    if (_usedKeypoints.Any(k => k == i))
                    {
                        Keypoints[j] = keypoint.X;
                        Keypoints[j + 1] = keypoint.Y;

                        j += 2;
                    }

                    i++;
                }
            }

            var bodyXPerFrame = new float[20];

            var s = 0;

            foreach (var frame in moveSample.FrameKeypoints)
            {
                bodyXPerFrame[s] = frame.Keypoints[1].X;

                s++;
            }

            NormalizeFeatures(bodyXPerFrame);
        }

        /// <summary>
        /// Normalizes the features.
        /// </summary>
        /// <param name="bodyXperFrame">The X center of the body per frame</param>
        private void NormalizeFeatures(float[] bodyXperFrame)
        {
            var numberOfKeypointsXY = Keypoints.Length / 20;

            var maxPerKeypoint = new float[numberOfKeypointsXY];

            for (int i = 0; i < numberOfKeypointsXY; i++)
            {
                maxPerKeypoint[i] = 1;
            }

            for (int i = 0; i < numberOfKeypointsXY; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    var abs = Math.Abs(Keypoints[j]);

                    if (abs > maxPerKeypoint[i])
                    {
                        maxPerKeypoint[i] = abs;
                    }
                }
            }

            for (int i = 0; i < Keypoints.Length; i++)
            {
                var keypointIndex = i % numberOfKeypointsXY;
                var frameIndex = (int)Math.Floor((double)(i / numberOfKeypointsXY));
                
                Keypoints[i] = i % 2 == 0 ? ((Keypoints[i] - bodyXperFrame[frameIndex]) / maxPerKeypoint[keypointIndex])
                                          : (Keypoints[i] / maxPerKeypoint[keypointIndex]);
            }
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => "Label: " + MoveLabel;
    }
}
