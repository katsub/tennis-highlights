using Microsoft.ML;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using TennisHighlights.ImageProcessing;
using TennisHighlights.ImageProcessing.PlayerMoves;
using TennisHighlights.Utils;
using TennisHighlights.Utils.PoseEstimation.Classification;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights
{
    public class PoseEstimationBuilder
    {
        public static OpenCvSharp.Size TargetSize = new OpenCvSharp.Size(224,224);
        private const string _keypointsRootKey = "Keypoints";
        private const string _pointSeparator = ";";
        private const string _moveSampleKey = "MoveSample";
        private const string _moveKey = "Move";
        private const string _subMoveKey = "SubMove";
        private const string _fileNameKey = "fileName";
        private const string _framesKey = "Frames";
        private const string _idKey = "Id";
        private const string _keypointPreffix = "Keypoint.";
        private const string _backhandKey = "backhand";
        private const string _forehandKey = "forehand";
        private const string _serviceKey = "service";

        private static MLContext _mlContext;

        public static void Test()
        {
            LoadPoseEstimationModelAndTest();
        }

        public static void ClassifyLogFile(ProcessedFileLog log, VideoInfo videoInfo)
        {
            BuildAndSavePoseEstimationModel();

            var foregroundMoves = TennisMoveDetector.GetForegroundPlayerMovesPerFrame(videoInfo, log);

            //          var keypoints = GetKeypointSequence(643, 7, log);

            //ExtractKeypoints();
        }

        public static List<Accord.Point> GetKeypointSequence(int startFrame, int keypointIndex, ProcessedFileLog log)
        {
            var keypoints = new List<Accord.Point>();

            for (int i = startFrame; i < startFrame + 20; i++)
            {
                if (log.ForegroundPlayerKeypoints.TryGetValue(i, out var keypoint))
                {
                    var point = new Accord.Point(keypoint.Keypoints[2 * keypointIndex], keypoint.Keypoints[2 * keypointIndex + 1]);

                    keypoints.Add(point);
                }
                else
                {
                    keypoints.Add(new Accord.Point(float.NaN, float.NaN));
                }
            }

            return keypoints;
        }

        private static void LoadPoseEstimationModelAndTest()
        {
            _mlContext = new MLContext();

            var trainedModel = _mlContext.Model.Load("poseEstimationModel.zip", out var modelSchema);

            var predEngine = _mlContext.Model.CreatePredictionEngine<MoveSampleMLInput, MoveLabelMLPrediction>(trainedModel);

            var keypoints = ReadKeypoints();
            var keypointsMLInput = keypoints.Select(k => new MoveSampleMLInput(k)).ToList();

            var pred = predEngine.Predict(keypointsMLInput.First());
            var pred2 = predEngine.Predict(keypointsMLInput.Last());
        }

        private static void BuildAndSavePoseEstimationModel()
        {
            _mlContext = new MLContext();

            var keypoints = ReadKeypoints();
            var keypointsMLInput = keypoints.Select(k => new MoveSampleMLInput(k)).ToList();

            var mlData = _mlContext.Data.LoadFromEnumerable(keypointsMLInput);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var sdcaEstimator = _mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy("Label", "Features");

            //            var estimator = sdcaEstimator.Fit(mlData);

            //testar se consegue treinar com todas as samples , e em que tempo
            var cvResults = _mlContext.MulticlassClassification.CrossValidate(mlData, sdcaEstimator);

            //var trainedModel = trainingPipeline.Fit(mlData);

            Logger.Log(LogType.Information, stopwatch.Elapsed.TotalSeconds.ToString());
            Debug.WriteLine(stopwatch.Elapsed.TotalSeconds.ToString());

            IEnumerable<double> rSquared = cvResults.Select(fold => fold.Metrics.MacroAccuracy);

            // Select all models
            ITransformer[] models = cvResults.OrderByDescending(fold => fold.Metrics.MacroAccuracy)
                                             .Select(fold => fold.Model)
                                             .ToArray();

            // Get Top Model
            var trainedModel = models[0];

            var predEngine = _mlContext.Model.CreatePredictionEngine<MoveSampleMLInput, MoveLabelMLPrediction>(trainedModel);

            var pred = predEngine.Predict(keypointsMLInput.First());
            var pred2 = predEngine.Predict(keypointsMLInput[200]);

            _mlContext.Model.Save(trainedModel, mlData.Schema, "poseEstimationModel.zip");
        }

        /// <summary>
        /// Reads the keypoints and returns them in a list.
        /// </summary>
        public static List<MoveSample> ReadKeypoints()
        {
            var samples = new List<MoveSample>();

            var keypointsDocument = XDocument.Load("./keypoints.xml").Element(_keypointsRootKey);

            foreach (var xMoveSample in keypointsDocument.Elements(_moveSampleKey))
            {
                var xMove = xMoveSample.GetStringAttribute(_moveKey);

                int move = (int)(xMove == _backhandKey ? MoveLabel.Backhand
                                                 : xMove == _forehandKey ? MoveLabel.Forehand
                                                                         : MoveLabel.Service);

                var subMove = xMoveSample.GetStringAttribute(_subMoveKey);
                var fileName = xMoveSample.GetStringAttribute(_fileNameKey);

                var frameKeypoints = new List<FrameKeypoints>();

                foreach (var xFrameKeypoints in xMoveSample.Elements("Frames"))
                {
                    var id = xFrameKeypoints.GetIntAttribute(_idKey);

                    var i = 0;

                    var keypoints = new List<Accord.Point>();

                    while (i != int.MinValue)
                    {
                        var xKeypoint = xFrameKeypoints.GetStringAttribute(_keypointPreffix + i);

                        if (xKeypoint != null)
                        {
                            var values = xKeypoint.Split(_pointSeparator);

                            keypoints.Add(new Accord.Point(int.Parse(values[0]), int.Parse(values[1])));

                            i++;
                        }
                        else
                        {
                            i = int.MinValue;
                        }
                    }

                    frameKeypoints.Add(new FrameKeypoints(id, keypoints));
                }

                var moveSample = new MoveSample(move, subMove, fileName, frameKeypoints);

                samples.Add(moveSample);
            }

            return samples;
        }

        /// <summary>
        /// Extracts the keypoints from a given folder with labeled folders.
        /// </summary>
        public static void ExtractKeypoints()
        {
            var keypointsRoot = new XElement(_keypointsRootKey);

            var baseFolderPath = "C:\\Users\\diego\\Downloads\\VIDEO_RGB\\VIDEO_RGB";

            var keypointExtractor = new KeypointExtractor();

            var sampleFolder = "./samples/";

            if (!Directory.Exists(sampleFolder))
            {
                Directory.CreateDirectory(sampleFolder);
            }

            try
            {
                foreach (var folderPath in Directory.GetDirectories(baseFolderPath))
                {
                    var folderName = new DirectoryInfo(folderPath).Name.ToLower();

                    var label = folderName.Contains("forehand") ? _forehandKey
                                                                : folderName.Contains("backhand") ? _backhandKey
                                                                                                  : folderName.Contains("service") ? _serviceKey
                                                                                                                                   : string.Empty;

                    if (!string.IsNullOrEmpty(label))
                    {
                        var sublabel = folderName.Replace(label, "");

                        foreach (var filePath in Directory.GetFiles(folderPath))
                        {
                            var videoInfo = new VideoInfo(filePath);
                            var size = new OpenCvSharp.Size(videoInfo.Width, videoInfo.Height);

                            if (videoInfo.Height > videoInfo.Width) { throw new Exception(); }

                            MatOfByte3 croppedMat = null;
                            MatIndexer<Vec3b> cropIndexer = null;

                            if (videoInfo.Width > videoInfo.Height)
                            {
                                croppedMat = new MatOfByte3(videoInfo.Height, videoInfo.Height);
                                cropIndexer = croppedMat.GetIndexer();
                            }

                            var stopwatch = new Stopwatch();
                            stopwatch.Start();

                            var fileName = new FileInfo(filePath).Name;

                            using (var videoCapture = new VideoCapture(filePath))
                            using (var resizeMat = new MatOfByte3(TargetSize))
                            using (var mat = new MatOfByte3(videoInfo.Height, videoInfo.Width))
                            {
                                var matIndexer = mat.GetIndexer();

                                var frameCount = (int)Math.Min(100, videoInfo.TotalFrames);

                                var moveSample = new XElement(_moveSampleKey, new XAttribute(_moveKey, label), new XAttribute(_subMoveKey, sublabel), new XAttribute(_fileNameKey, fileName));

                                for (int i = 0; i < frameCount; i++)
                                {
                                    videoCapture.Read(mat);

                                    if (i % 5 == 0)
                                    {
                                        var matToUse = mat;

                                        if (croppedMat != null)
                                        {
                                            var deltaCrop = videoInfo.Width - videoInfo.Height;
                                            var cropStart = (int)Math.Floor(deltaCrop / 2d);

                                            for (int a = 0; a < videoInfo.Height; a++)
                                            {
                                                for (int b = 0; b < videoInfo.Height; b++)
                                                {
                                                    cropIndexer[a, b] = matIndexer[a, cropStart + b];
                                                }
                                            }

                                            matToUse = croppedMat;
                                        }

                                        Cv2.Resize(matToUse, resizeMat, TargetSize, 0, 0, InterpolationFlags.Nearest);

                                        var keypoints = keypointExtractor.GetKeypoints(resizeMat);

                                        ImageUtils.DrawKeypoints(keypoints, fileName + "_" + i + ".jpg", resizeMat);

                                        var xFrames = new XElement(_framesKey, new XAttribute(_idKey, i));

                                        for (int kp = 0; kp < keypoints.Count; kp++)
                                        {
                                            var keypoint = keypoints[kp];

                                            xFrames.Add(new XAttribute(_keypointPreffix + kp, keypoint.X + _pointSeparator + keypoint.Y));
                                        }

                                        moveSample.Add(xFrames);

                                        /*
                                        var bitmap = BitmapConverter.ToBitmap(resizeMat);

                                        ImageUtils.DrawCircles(bitmap, keypoints, 7, Brushes.Red);

                                        bitmap.Save(sampleFolder + fileName + "_" + i.ToString("D6") + ".jpg");*/
                                    }
                                }

                                return;

                                Thread.Sleep(5000);

                                Logger.Log(LogType.Information, folderName + " " + fileName);

                                keypointsRoot.Add(moveSample);
                            }
                        }
                    }
                }
            }
            finally
            {
                var keypointDocument = new XDocument();

                keypointDocument.Add(keypointsRoot);

                keypointDocument.Save("./keypoints.xml");
            }
        }

        /// <summary>
        /// Draws the keypoints.
        /// </summary>
        public static void DrawKeypoints()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var filePath = basePath + "\\" + "federerForehand2.jpg";

            var inputFrame = Cv2.ImRead(filePath, ImreadModes.Unchanged);

            var inputBitmap = new Bitmap(filePath);

            var keypointExtractor = new KeypointExtractor();

            ImageUtils.DrawCircles(inputBitmap, keypointExtractor.GetKeypoints(inputFrame), 7, Brushes.Red);

            inputBitmap.Save("./fedKeypoints.jpg");
        }
    }
}
