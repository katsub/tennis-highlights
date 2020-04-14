using OpenCvSharp;
using OpenCvSharp.Dnn;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TennisHighlights.Utils.PoseEstimation.Keypoints
{
    /// <summary>
    /// The keypoint extractor
    /// </summary>
    public class KeypointExtractor
    {
        /// <summary>
        /// The net
        /// </summary>
        private readonly Net _net;
        /// <summary>
        /// The net lock
        /// </summary>
        private readonly object _netLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KeypointExtractor"/> class.
        /// </summary>
        public KeypointExtractor()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var prototxt = basePath + "\\" + "pose_deploy_linevec_faster_4_stages.prototxt";
            var caffeModel = basePath + "\\" + "pose_iter_160000.caffemodel";

            _net = CvDnn.ReadNetFromCaffe(prototxt, caffeModel);
        }

        /// <summary>
        /// Gets the keypoints.
        /// </summary>
        /// <param name="inputFrame">The input frame.</param>
        public List<Accord.Point> GetKeypoints(Mat inputFrame)
        {
            var size = inputFrame.Size();

            //It's normal that the dimensions are -1 x -1, that's because the tostring method doesn't display it correctly
            var blob = CvDnn.BlobFromImage(inputFrame, 1.0d / 255, size, new Scalar(0, 0, 0), false, false);

            Mat output = null;

            lock (_netLock)
            {
                _net.SetInput(blob);

                output = _net.Forward();
            }

            var outputIndexer = output.GetGenericIndexer<float>();

            var outputH = output.Size(2);

            var probMap = new MatOfFloat(size.Height, size.Width, MatType.CV_32FC1);
            var probIndexer = probMap.GetIndexer();

            var keypoints = new List<Accord.Point>();

            for (int k = 0; k < 15; k++)
            {
                //Assumes image is square
                for (int i = 0; i < outputH; i++)
                {
                    for (int j = 0; j < outputH; j++)
                    {
                        probIndexer[j, i] = 100f * outputIndexer[0, k, j, i];
                    }
                }

                Cv2.MinMaxLoc(probMap, out OpenCvSharp.Point minValue, out OpenCvSharp.Point maxValue);

                keypoints.Add(new Accord.Point((int)(maxValue.X * size.Width / outputH), (int)(maxValue.Y * size.Height / outputH)));
            }

            var firstKeypoint = keypoints.First();

            var anyDifferentKeypoint = false;

            foreach (var keypoint in keypoints)
            {
                if (keypoint.X != firstKeypoint.X || keypoint.Y != firstKeypoint.Y)
                {
                    anyDifferentKeypoint = true;

                    break;
                }
            }

            return anyDifferentKeypoint ? keypoints : null;
        }
    }
}
