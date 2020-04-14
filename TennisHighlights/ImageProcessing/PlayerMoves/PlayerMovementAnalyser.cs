using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The player movement analyser
    /// </summary>
    public class PlayerMovementAnalyser
    {
        /// <summary>
        /// The foreground player frames
        /// </summary>
        public PlayerFrameData[] ForegroundPlayerFrames { get; }
        /// <summary>
        /// The keypoint extractor
        /// </summary>
        private readonly KeypointExtractor _keypointExtractor;
        /// <summary>
        /// The frames per sample
        /// </summary>
        public int FramesPerSample { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerMovementAnalyser" /> class.
        /// </summary>
        /// <param name="videoInfo">The video information.</param>
        public PlayerMovementAnalyser(VideoInfo videoInfo)
        {
            //If source frame rate = 60 then get 1 sample for each 10 frames, if it's 30 then get 1 sample for each 5 frames
            FramesPerSample = GetFramesPerSample(videoInfo.FrameRate);

            var totalSampledFrames = (videoInfo.TotalFrames / FramesPerSample);

            ForegroundPlayerFrames = new PlayerFrameData[totalSampledFrames];

            _keypointExtractor = new KeypointExtractor();
        }

        /// <summary>
        /// Gets the frames per sample
        /// </summary>
        /// <param name="sourceFrameRate">the source frame rate</param>
        public static int GetFramesPerSample(double sourceFrameRate) => sourceFrameRate > 45 ? 10 : 5;

        /// <summary>
        /// Extracts the player keypoints. Returns true if the code found a keypoint, false otherwise.
        /// </summary>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="playerMat">The player mat.</param>
        /// <param name="playerRect">The player rect.</param>
        /// <param name="playerDico">The player dico.</param>
        /// <param name="resizeMat">The resize mat.</param>
        private bool ExtractPlayerKeypoints(int frameId, Mat playerMat, Rect playerRect, PlayerFrameData[] playerDico, Mat resizeMat)
        {            
            Cv2.Resize(playerMat, resizeMat, PoseEstimationBuilder.TargetSize, 0, 0, InterpolationFlags.Nearest);

            var keypoints = _keypointExtractor.GetKeypoints(resizeMat);

            //ImageUtils.DrawKeypoints(keypoints, frameId + ".jpg", resizeMat);

            playerMat.Dispose();

            if (keypoints == null || AnyErrorKeypoint(keypoints)) { return false; }

            var keypointsArray = new float[2 * keypoints.Count];

            for (int i = 0; i < keypoints.Count; i++)
            {
                keypointsArray[2 * i] = keypoints[i].X;
                keypointsArray[2 * i + 1] = keypoints[i].Y;
            }

            playerDico[frameId / FramesPerSample] = new PlayerFrameData(keypointsArray, null, new Accord.Point(playerRect.Left, playerRect.Top));

            return true;
        }

        /// <summary>
        /// Extracts the player keypoints. Returns true if the code found a keypoint, false otherwise.
        /// </summary>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="playerMat">The player mat.</param>
        /// <param name="playerBlob">The player blob.</param>
        /// <param name="playerDico">The player dico.</param>
        /// <param name="keypointResizeMat">The keypoint resize mat.</param>
        private bool ExtractPlayerKeypoints(int frameId, Mat playerMat, ConnectedComponents.Blob playerBlob, PlayerFrameData[] playerDico, MatOfByte3 keypointResizeMat)
                  => ExtractPlayerKeypoints(frameId, playerMat, new Rect(playerBlob.Left, playerBlob.Top, playerBlob.Width, playerBlob.Height), playerDico, keypointResizeMat);

        /// <summary>
        /// Noes the error keypoint.
        /// </summary>
        /// <param name="keypoints">The keypoints.</param>
        public static bool AnyErrorKeypoint(List<Accord.Point> keypoints) => keypoints.Any(k => k.X == 224 && k.Y == 0);

        /// <summary>
        /// True if there's no error keypoint.
        /// </summary>
        /// <param name="keypoints">The keypoints.</param>
        public static bool NoErrorKeypoint(float[] keypoints)
        {
            for (int i = 0; i < keypoints.Length; i += 2)
            {
                //When a keypoint is not found in the picture, it is mapped to 224,0
                if (keypoints[i] == 224 && keypoints[i + 1] == 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the blob mat.
        /// </summary>
        /// <param name="frameMat">The frame mat.</param>
        /// <param name="blob">The blob.</param>
        private MatOfByte3 GetBlobMat(MatOfByte3 frameMat, ConnectedComponents.Blob blob)
        {
            var minX = (int)(blob.Rect.Left * 0.8);
            var maxX = (int)Math.Min(blob.Rect.Right * 1.3, frameMat.Width);
            //The rect Bottom comments indicate the axis origin is on the top and on the left
            var maxY = (int)Math.Min(blob.Rect.Bottom * 1.3, frameMat.Height);
            var minY = (int)(blob.Rect.Top * 0.8);

            //Needs to be square because that the type of picture the neural net takes
            var lengthX = maxX - minX;
            var lengthY = maxY - minY;

            if (lengthX > lengthY)
            {
                var delta = (int)((lengthX - lengthY) / 2);

                minX += delta;
                maxX -= delta;
            }
            else if (lengthY > lengthX)
            {
                var delta = (int)((lengthY - lengthX) / 2);

                minY += delta;
                maxY -= delta;
            }

            lengthX = maxX - minX;
            lengthY = maxY - minY;

            if (lengthY < lengthX) { minX += 1; }
            else if (lengthX < lengthY) { minY += 1; }

            Contract.Assert(maxY - minY == maxX - minX);

            var mat = new MatOfByte3(maxY - minY, maxX - minX);

            var blobIndexer = mat.GetIndexer();
            var sourceIndexer = frameMat.GetIndexer();

            for (int i = minX; i < maxX; i++)
            {
                for (int j = minY; j < maxY; j++)
                {
                    blobIndexer[j - minY, i - minX] = sourceIndexer[j, i];
                }
            }

            return mat;
        }

        /// <summary>
        /// Adds the frame.
        /// </summary>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="frameMat">The frame mat.</param>
        /// <param name="playerBlobs">The player blobs.</param>
        /// <param name="keypointResizeMat">The keypoint resize mat.</param>
        internal void AddFrame(int frameId, MatOfByte3 frameMat, List<ConnectedComponents.Blob> playerBlobs, MatOfByte3 keypointResizeMat)
        {
            if (frameId % FramesPerSample != 0 || playerBlobs == null) { return; }

            var foregroundBlob = playerBlobs.OrderByDescending(b => b.Area).FirstOrDefault();

            if (foregroundBlob != null)
            {
                var foregroundMat = GetBlobMat(frameMat, foregroundBlob);

                ExtractPlayerKeypoints(frameId, foregroundMat, foregroundBlob, ForegroundPlayerFrames, keypointResizeMat);
            }
        }
    }
}
