using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using TennisHighlights.Annotation;
using TennisHighlights.ImageProcessing;

namespace TennisHighlights
{
    /// <summary>
    /// Extracts balls from a frame
    /// </summary>
    public class FrameBallExtractor : IDisposable
    {
        /// <summary>
        /// The maximum double check ball center squared distance
        /// </summary>
        private static readonly ResolutionDependentParameter _maxDoubleCheckBallCenterSquaredDistance = new ResolutionDependentParameter(225d, 2d);
        /// <summary>
        /// The draw gizmos. Debug option that allows us to see each frame with gizmos coresponding to detected players and balls in the temp folder
        /// </summary>
        private readonly bool _drawGizmos;
        /// <summary>
        /// The draw previews
        /// </summary>
        private readonly bool _drawPreviews;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly BallDetectionSettings _settings;
        /// <summary>
        /// The on extraction over
        /// </summary>
        private readonly Action<int, List<Accord.Point>> _onExtractionOver;
        /// <summary>
        /// The current frame identifier
        /// </summary>
        private int _currentFrameId;
        /// <summary>
        /// The size
        /// </summary>
        private static OpenCvSharp.Size _size;
        /// <summary>
        /// The connection dilation circle
        /// </summary>
        private static MatOfByte _connectionDilationCircle;
        /// <summary>
        /// The connection dilation circle area
        /// </summary>
        private static double _connectionDilationCircleArea;
        /// <summary>
        /// The contour dilation circle
        /// </summary>
        private static MatOfByte _lightDilationCircle;
        /// <summary>
        /// The player erosion circle
        /// </summary>
        private static MatOfByte _playerErosionCircle;
        /// <summary>
        /// The erosion gizmo circle
        /// </summary>
        private static MatOfByte _erosionGizmoCircle;
        /// <summary>
        /// The player dilation circle
        /// </summary>
        private static MatOfByte _playerDilationCircle;
        /// <summary>
        /// The contour erosion circle
        /// </summary>
        private static MatOfByte _lightErosionCircle;
        /// <summary>
        /// The zeros
        /// </summary>
        private static MatOfByte _zeros;
        /// <summary>
        /// The zeros
        /// </summary>
        private static MatOfByte _ones;

        /// <summary>
        /// Gets a value indicating whether this instance is busy.
        /// </summary>
        public bool IsBusy => ExtractionArguments != null;
        /// <summary>
        /// Gets or sets the assigned frame.
        /// </summary>
        public FrameExtractionArguments ExtractionArguments { get; private set; }

        //We cache the materials to avoid reallocating and freeing them at every frame. 
        private readonly MatOfByte3 _gizmoMat;
        private readonly MatOfByte3 _deltaMat = new MatOfByte3();
        private readonly MatOfByte _dilatedMat = new MatOfByte();
        private readonly MatOfByte _erodedMat = new MatOfByte();
        private readonly MatOfByte _timeDeltaMat = new MatOfByte();
        private readonly MatOfByte _playerBallMat = new MatOfByte();
        private readonly MatOfByte _bgDeltaMat = new MatOfByte();
        private readonly MatOfByte _playerOutline = new MatOfByte();
        private readonly MatOfInt _labels = new MatOfInt();
        private readonly MatOfDouble _centroids = new MatOfDouble();
        private readonly MatOfShort _stats = new MatOfShort();

        /// <summary>
        /// Assigns the extraction.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="System.Exception">FrameExtractionException: tried to make an extraction assignment but the extractor already had one. Frame assigned previously: " + ExtractionArguments.FrameId + ". New frame: " + arguments.FrameId</exception>
        public void AssignExtraction(FrameExtractionArguments arguments)
        {
            if (ExtractionArguments != null)
            {
                throw new Exception("FrameExtractionException: tried to make an extraction assignment but the extractor already had one. Frame assigned previously: " + ExtractionArguments.FrameId + ". New frame: " + arguments.FrameId);
            }

            ExtractionArguments = arguments;
        }

        /// <summary>
        /// Allocates the resolution dependent mats.
        /// </summary>
        public static void AllocateResolutionDependentMats()
        {
            var connectionDilationCircleDiameter = new ResolutionDependentParameter(15d, 1d).IntValue;

            _connectionDilationCircle = ImageUtils.BuildCircleMaskMat(connectionDilationCircleDiameter);
            _connectionDilationCircleArea = Math.PI * Math.Pow(connectionDilationCircleDiameter / 2d, 2d) / 2d;

            //Noise did not seem to reduce size with the resizing, maybe the increased resizing compensates the natural
            //noise size reduction?
            //Was originally 5 for 720p but 5 is also the good value for 480p
            _playerErosionCircle = ImageUtils.BuildCircleMaskMat(5);
            _playerDilationCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(30d, 1d).IntValue);
            _erosionGizmoCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(10d, 1d).IntValue);
            _lightErosionCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(3d, 1d).IntValue);
            _lightDilationCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(5d, 1d).IntValue);
        }

        /// <summary>
        /// Sets the size.
        /// </summary>
        /// <param name="size">The size.</param>
        public static void SetSize(OpenCvSharp.Size size)
        {
            _size = size;
            _zeros = new MatOfByte(_size.Height, _size.Width, (byte)0);
            _ones = new MatOfByte(_size.Height, _size.Width, (byte)255);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameBallExtractor" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="drawGizmos">if set to <c>true</c> [draw gizmos].</param>
        /// <param name="drawPreviews">if set to <c>true</c> [draw previews].</param>
        /// <param name="onExtractionOver">The on extraction over.</param>
        public FrameBallExtractor(BallDetectionSettings settings, bool drawGizmos, bool drawPreviews, Action<int, List<Accord.Point>> onExtractionOver)
        {
            _drawGizmos = drawGizmos;
            _drawPreviews = drawPreviews;
            _settings = settings;
            _onExtractionOver = onExtractionOver;

            if (_drawGizmos || _drawPreviews)
            {
                _gizmoMat = new MatOfByte3(_size.Height, _size.Width);
            }
        }

        /// <summary>
        /// Calculates the difference between the two input materials, binarizes it and stores it in the output mat.
        /// </summary>
        /// <param name="mat1">The mat1.</param>
        /// <param name="mat2">The mat2.</param>
        /// <param name="output">The output.</param>
        private void BinaryDelta(MatOfByte3 mat1, MatOfByte3 mat2, MatOfByte output)
        {
            //Gets the difference between both images
            //Absdiff is used over diff because we need negative values to show in the result, instead of being mapped to zero.
            Cv2.Absdiff(mat1, mat2, _deltaMat);

            //This is equivalent to getting the value channel of the converted HSV image
            Cv2.Max(_deltaMat.ExtractChannel(0), _deltaMat.ExtractChannel(1), output);
            Cv2.Max(_deltaMat.ExtractChannel(2), output, output);

            //Convert into a binary image
            //Also, filter the value channel so there's less noise
            Cv2.InRange(output, _settings.MinBrightness, 255, output);
        }

        /// <summary>
        /// Doubles the check candidate balls on eroded mat.
        /// </summary>
        /// <param name="mat">The mat.</param>
        /// <param name="potentialBalls">The potential balls.</param>
        private void DoubleCheckCandidateBallsOnEroded(Mat mat, List<Accord.Point> potentialBalls)
        {
            var maxDoubleCheckBallCenterSquaredDistance = _maxDoubleCheckBallCenterSquaredDistance.Value;

            //Erosion should delete noise, so absent balls should be removed from the already added candidates
            var numberOfComponents = Cv2.ConnectedComponentsWithStats(mat, _labels, _stats, _centroids, PixelConnectivity.Connectivity4, MatType.CV_16U);

            Span<double> x = stackalloc double[numberOfComponents];
            Span<double> y = stackalloc double[numberOfComponents];

            //GetIndexer (and the index itself) are very slow, and we're gonna access them a lot, so better cache it
            var centroidIndexer = _centroids.GetIndexer();

            for (int j = 0; j < numberOfComponents; j++)
            {
                x[j] = centroidIndexer[j, 0];
                y[j] = centroidIndexer[j, 1];
            }

            for (int i = potentialBalls.Count - 1; i >= 0; i--)
            {
                var potentialBall = potentialBalls[i];

                var foundBall = false;

                for (int j = 0; j < numberOfComponents; j++)
                {
                    //Check if there's at least one ball in there eroded mat that's close to one of the potential balls
                    if (Math.Pow(potentialBall.X - x[j], 2) + Math.Pow(potentialBall.Y - y[j], 2) < maxDoubleCheckBallCenterSquaredDistance)
                    {
                        foundBall = true;
                        break;
                    }
                }

                if (!foundBall)
                {
                    potentialBalls.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets the potential balls. The core idea is that after doing a big dilation, any object that is just a circle must be far
        /// from other objects (players' pieces get connected at this phase, noise from rustling trees too) is a potential ball
        /// </summary>
        /// <param name="bgDiffMat">The bg difference mat.</param>
        /// <param name="playersBoundaries">The players boundaries.</param>
        private void GetPotentialBalls(MatOfByte bgDiffMat, out List<Accord.Point> potentialBalls)
        {
            var minPlayerArea = _settings.MinPlayerArea.Value;
            var numberOfComponents = Cv2.ConnectedComponentsWithStats(bgDiffMat, _labels, _stats, _centroids, PixelConnectivity.Connectivity4, MatType.CV_16U);

            potentialBalls = null;

            if (numberOfComponents > 0)
            {
                var statsIndexer = _stats.GetIndexer();
                MatIndexer<double> centroidIndexer = null;

                //Ignore the background blob (i = 0)
                for (int i = 1; i < numberOfComponents; i++)
                {
                    var area = statsIndexer[i, (int)ConnectedComponentsTypes.Area];

                    //Only pick blobs that aren't too big, but aren't too small either, or they could be noise that got dilated
                    if (area < minPlayerArea && area > _connectionDilationCircleArea)
                    {
                        var width = statsIndexer[i, (int)ConnectedComponentsTypes.Width];
                        var height = statsIndexer[i, (int)ConnectedComponentsTypes.Height];
                        var roundness = (double)height / width;

                        //Only pick somewhat round balls
                        if (roundness < 2d && roundness > 0.5d)
                        {
                            if (potentialBalls == null)
                            {
                                potentialBalls = new List<Accord.Point>();

                                centroidIndexer = _centroids.GetIndexer();
                            }

                            potentialBalls.Add(new Accord.Point((float)centroidIndexer[i, 0], (float)centroidIndexer[i, 1]));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the balls from the assigned current mat.
        /// </summary>
        public List<Accord.Point> Extract(out Mat gizmoMat)
        {
            gizmoMat = null;
            _currentFrameId = ExtractionArguments.FrameId;

            //Calculate the delta between this frame and the previous one (time delta: only moving objects appear)
            BinaryDelta(ExtractionArguments.PreviousMat, ExtractionArguments.CurrentMat, _timeDeltaMat);
            //Calculate the delta between the background and the current frame (background delta: only objects that aren't part of the background appear)
            //This mat is needed because sometimes players don't move enough between the two frames, and the small parts that moved mmay get mistaken by balls
            //This ensures we have a good estimation of where the players are so we can ignore nearby movement picked in the time delta mat
            BinaryDelta(ExtractionArguments.Background, ExtractionArguments.CurrentMat, _bgDeltaMat);

            //Filter moving objects / noise that are part of the background by forcing output to be present in both deltas
            Cv2.BitwiseAnd(_timeDeltaMat, _bgDeltaMat, _timeDeltaMat);
            //Big dilation so every moving body part gets connected
            //no erosion allowed: must ensure the player gets fully connected, must reduce the chance of its parts being perceived as balls
            Cv2.Dilate(_timeDeltaMat, _dilatedMat, _connectionDilationCircle);

            if (_drawGizmos)
            {
                FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_timeDelta.jpeg", _dilatedMat, FileManager.FrameFolder);
            }

            //Get all blobs that aren't too big and are somewhat round: those are the potential balls
            GetPotentialBalls(_dilatedMat, out var balls);

            if (balls?.Any() == true)
            {
                //We remove noise very agressively in order to leave only the players and the ball in this mat
                //Furthermore, the excessive dilation connects everything, ensuring that trees leaves will all be merged together forming a big blob, instead of producing lots of fake balls
                //That's very important because if the wind moves the trees or the camera, this temporarily creates a big blob that hides the ball position 
                //(if it's between the camera and the trees), which is better than having lots of fake balls that might create a random trajectory
                Cv2.Erode(_bgDeltaMat, _dilatedMat, _playerErosionCircle);
                Cv2.Dilate(_dilatedMat, _playerBallMat, _playerDilationCircle);

                if (_drawGizmos)
                {
                    FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_playerBall.jpeg", _playerBallMat, FileManager.FrameFolder);
                }

                //Remove all big blobs: if the balls is far away and on its own, it won't be part of a player / big blob
                var hadPlayers = LeaveOnlyBigBlobs(_playerBallMat);

                //Do some light noise removal on the time delta mat: undesirable regions such as trees leaves will still be visible, but so will the ball
                Cv2.Erode(_timeDeltaMat, _erodedMat, _lightErosionCircle);
                Cv2.Dilate(_erodedMat, _dilatedMat, _lightDilationCircle);
               
                //Delete all balls that inside the big blobs / players
                Cv2.Subtract(_dilatedMat, _playerBallMat, _dilatedMat);

                if (_drawGizmos || _drawPreviews)
                {
                    //Erode a bit then subtract to build an outline around the big blobs / players
                    if (hadPlayers)
                    {
                        Cv2.Erode(_playerBallMat, _playerOutline, _erosionGizmoCircle);

                        Cv2.Subtract(_playerBallMat, _playerOutline, _playerOutline);
                    }

                    var gizmoIndexer = _gizmoMat.GetIndexer();
                    var playerOutlineIndexer = _playerOutline.GetIndexer();
                    var currentFrameIndexer = ExtractionArguments.CurrentMat.GetIndexer();
                    var potentialBallsPiecesIndexer = _dilatedMat.GetIndexer();

                    var width = _size.Width;

                    for (int j = 0; j < _size.Height; j++)
                    {
                        for (int i = 0; i < width; i++)
                        {
                            if (potentialBallsPiecesIndexer[j, i] > 0)
                            {
                                gizmoIndexer[j, i] = new Vec3b(255, 0, 255);
                            }
                            else
                            {
                                gizmoIndexer[j, i] = hadPlayers && playerOutlineIndexer[j, i] > 0 ? new Vec3b(255, 0, 0)
                                                                                                  : currentFrameIndexer[j, i];
                            }
                        }
                    }

                    gizmoMat = _gizmoMat;
                }

                if (_drawGizmos)
                {
                    FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_doubleCheck.jpeg", _dilatedMat, FileManager.FrameFolder);
                }

                //Balls was initially populated from all the balls (good size, roundish, away from moving objects) from the very dilated time delta mat. The big dilation ensures that
                //we get one ball instead of multiple moving parts of that ball (the ball is small so the dilation connects its moving parts)
                //It also got a BitwiseAnd with the bgDeltaMat, so this filters another bit of fake balls, because noise is unlikely to appear in both
                //deltas
                //There was no erosion in that mat, so noise also constitutes potential balls (GetPotentialBalls())
                //We have built another mat out of timeDeltaMat (the last _dilatedMat) that has been eroded, so it has less noise, and all the big blobs
                //have been subtracted from it, so players and regions with lots of "balls" (like clusters of leaves)
                //Basically there's a bunch of noise suppression techniques combined, and the only thing able to bypass all of them should be 
                //a ball far away from any moving object
                DoubleCheckCandidateBallsOnEroded(_dilatedMat, balls);
            }

            return balls;
        }

        /// <summary>
        /// Removes everything but the player blobs.
        /// </summary>
        /// <param name="mat">The mat.</param>
        private bool LeaveOnlyBigBlobs(Mat mat)
        {
            var minPlayerArea = _settings.MinPlayerArea.Value;

            var hadPlayers = false;
            var components = Cv2.ConnectedComponentsEx(mat, PixelConnectivity.Connectivity4);

            //Keep only blobs that are big, but ignore the background blob
            //Since this mat originates from the _bgDelta, we try to be more conservative and only get very big blobs (2 * minPlayerArea).
            //The reason for this is that the bgDelta usually has a lot of noise (it merges 2.5s of backgrounds right now, so even the slightest changes
            //might cause bigger blobs, which might cover the ball
            var blobsToKeep = components.Blobs.Where(b => b.Area > 2 * minPlayerArea
                                                          && (b.Width < _size.Width || b.Height < _size.Height));

            //Clean the mat by copying zeroes then write all ones masked by the blobs we wanna keep
            if (blobsToKeep.Any())
            {
                _zeros.CopyTo(mat);

                hadPlayers = true;

                components.FilterBlobs(_ones, mat, blobsToKeep);
            }

            return hadPlayers;
        }

        /// <summary>
        /// Extracts and draws gizmos.
        /// </summary>
        public void ExtractAndDrawGizmos()
        {
            var frameId = ExtractionArguments.FrameId;

            var balls = Extract(out var gizmoMat);

            if (ExtractionArguments.OnGizmoDrawn != null)
            {
                //Copy must be done immediately, otherwise the arguments might change and it'll copy something else
                //Gizmo mat might be null (no players or balls detected)
                Bitmap frame = null;

                if (_drawPreviews)
                {
                    frame = BitmapConverter.ToBitmap(gizmoMat ?? ExtractionArguments.CurrentMat);
                }

                ExtractionArguments.OnGizmoDrawn(frame);
            }

            ExtractionArguments = null;

            _onExtractionOver(frameId, balls);

            if (_drawGizmos && gizmoMat != null)
            {
                //Copy must be done immediately, otherwise the arguments might change and it'll copy something else
                var frame = BitmapConverter.ToBitmap(gizmoMat);

                Task.Run(() => GizmoDrawer.DrawGizmosAndSaveImage(frame, frameId, balls));
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString() => ExtractionArguments?.FrameId.ToString();

        /// <summary>
        /// Disposes this instances's unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _gizmoMat.Dispose();
            _deltaMat.Dispose();
            _dilatedMat.Dispose();
            _erodedMat.Dispose();
            _timeDeltaMat.Dispose();
            _playerBallMat.Dispose();
            _bgDeltaMat.Dispose();
            _playerOutline.Dispose();
            _labels.Dispose();
            _centroids.Dispose();
            _stats.Dispose();
        }
    }
}
