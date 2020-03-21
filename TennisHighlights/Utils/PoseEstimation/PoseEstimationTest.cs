using OpenCvSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using TennisHighlights.ImageProcessing;
using TennisHighlights.Utils;
using TennisHighlights.Utils.PoseEstimation;

namespace TennisHighlights
{
    public class PoseEstimationBuilder
    {
        private static OpenCvSharp.Size _targetSize = new OpenCvSharp.Size(224,224);

        public static void Test()
        {
            ExtractKeypoints();
        }

        public static void ExtractKeypoints()
        {
            var keypointsRoot = new XElement("Keypoints");

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

                    var label = folderName.Contains("forehand") ? "forehand"
                                                                : folderName.Contains("backhand") ? "backhand"
                                                                                                  : folderName.Contains("service") ? "service"
                                                                                                                                 : string.Empty;

                    if (!string.IsNullOrEmpty(label))
                    { 
                    var sublabel = folderName.Replace(label, "");

                    var o = 0;
                        foreach (var filePath in Directory.GetFiles(folderPath))
                        {
                            o++;
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
                            using (var resizeMat = new MatOfByte3(_targetSize))
                            using (var mat = new MatOfByte3(videoInfo.Height, videoInfo.Width))
                            {
                                var matIndexer = mat.GetIndexer();

                                var frameCount = (int)Math.Min(100, videoInfo.TotalFrames);

                                var moveSample = new XElement("MoveSample", new XAttribute("Move", label), new XAttribute("SubMove", sublabel), new XAttribute("fileName", fileName));

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

                                        Cv2.Resize(matToUse, resizeMat, _targetSize, 0, 0, InterpolationFlags.Nearest);

                                        var keypoints = keypointExtractor.GetKeypoints(resizeMat);                                        

                                        var xFrames = new XElement("Frames", new XAttribute("Id", i));

                                        for (int kp = 0; kp < keypoints.Count; kp++)
                                        {
                                            var keypoint = keypoints[kp];

                                            xFrames.Add(new XAttribute("Keypoint." + kp, keypoint.X + ";" + keypoint.Y));
                                        }

                                        moveSample.Add(xFrames);

                                        /*
                                        var bitmap = BitmapConverter.ToBitmap(resizeMat);

                                        ImageUtils.DrawCircles(bitmap, keypoints, 7, Brushes.Red);

                                        bitmap.Save(sampleFolder + fileName + "_" + i.ToString("D6") + ".jpg");*/
                                    }
                                }

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
