using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private static ResolutionDependentParameter _maxDoubleCheckBallCenterSquaredDistance = new ResolutionDependentParameter(225d, 2d);
        /// <summary>
        /// The draw gizmos
        /// </summary>
        private readonly bool _drawGizmos;
        /// <summary>
        /// The draw previews
        /// </summary>
        private readonly bool _drawPreviews;
        /// <summary>
        /// The current frame identifier
        /// </summary>
        private int _currentFrameId;
        /// <summary>
        /// The gizmo count, used for quickly writing debug images without worrying about their names
        /// </summary>
        private static int _gizmoCount = 0;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly BallDetectionSettings _settings;
        /// <summary>
        /// The on extraction over
        /// </summary>
        private readonly Action<int, List<Accord.Point>> _onExtractionOver;
        /// <summary>
        /// The size
        /// </summary>
        private static OpenCvSharp.Size _size;
        /// <summary>
        /// The dilation circle
        /// </summary>
        private static Mat _dilationCircle;
        /// <summary>
        /// The dilation circle area
        /// </summary>
        private static double _dilationCircleArea;
        /// <summary>
        /// The contour dilation circle
        /// </summary>
        private static Mat _contourDilationCircle;
        /// <summary>
        /// The player erosion circle
        /// </summary>
        private static Mat _playerErosionCircle;
        /// <summary>
        /// The erosion gizmo circle
        /// </summary>
        private static Mat _erosionGizmoCircle;
        /// <summary>
        /// The player dilation circle
        /// </summary>
        private static Mat _playerDilationCircle;
        /// <summary>
        /// The contour erosion circle
        /// </summary>
        private static Mat _contourErosionCircle;
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
        //TODO: there should be classes for each method so that they each have their private variables, these shared local variables are confusing and could lead to bugs
        private readonly Mat _bgDiffMat = new Mat();
        private readonly MatOfByte3 _gizmoMat;
        private readonly Mat _diffMat = new Mat();
        private readonly MatOfByte _dilatedMat = new MatOfByte();
        private readonly Mat _erodedMat = new Mat();
        private readonly MatOfByte _timeDeltaMat = new MatOfByte();
        private readonly Mat _timeAntiDeltaMat = new Mat();
        private readonly MatOfByte _bgGrey = new MatOfByte();
        private readonly MatOfByte _bgAntiGrey = new MatOfByte();

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
            var dilationCircleDiameter = new ResolutionDependentParameter(15d, 1d).IntValue;

            _dilationCircle = ImageUtils.BuildCircleMaskMat(dilationCircleDiameter);
            _dilationCircleArea = Math.PI * Math.Pow(dilationCircleDiameter / 2d, 2d) / 2d;

            _contourDilationCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(5d, 1d).IntValue);
            _playerErosionCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(5d, 1d).IntValue);
            _erosionGizmoCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(10d, 1d).IntValue);
            _playerDilationCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(30d, 1d).IntValue);
            _contourErosionCircle = ImageUtils.BuildCircleMaskMat(new ResolutionDependentParameter(3d, 1d).IntValue);
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

            _gizmoMat = new MatOfByte3(_size.Height, _size.Width);
        }

        /// <summary>
        /// Gets the minimum background range.
        /// </summary>
        private static Scalar _minBackgroundGreyRange { get; } = new Scalar(30);
        /// <summary>
        /// Gets the maximum range.
        /// </summary>
        private static Scalar _maxGreyRange { get; } = new Scalar(255);

        /// <summary>
        /// Gets the background difference mat.
        /// </summary>
        /// <param name="background">The background.</param>
        /// <param name="currentMat">The current mat.</param>
        private Mat GetBackgroundDiffMat(Mat background, MatOfByte3 currentMat)
        {
            Cv2.Absdiff(background, currentMat, _bgDiffMat);

            Cv2.CvtColor(_bgDiffMat, _bgGrey, ColorConversionCodes.BGR2GRAY);

            Cv2.Normalize(_bgGrey, _bgGrey, 0, 255, NormTypes.MinMax);

            Cv2.InRange(_bgGrey, _minBackgroundGreyRange, _maxGreyRange, _bgGrey);

            return _bgGrey;
        }

        /// <summary>
        /// Gets the time delta mat.
        /// </summary>
        /// <param name="previousMat">The previous mat.</param>
        /// <param name="currentMat">The current mat.</param>
        private Mat GetTimeDeltaMat(MatOfByte3 previousMat, MatOfByte3 currentMat)
        {
            Cv2.Absdiff(currentMat, previousMat, _diffMat);

            Cv2.Max(_diffMat.ExtractChannel(0), _diffMat.ExtractChannel(1), _timeDeltaMat);
            Cv2.Max(_diffMat.ExtractChannel(2), _timeDeltaMat, _timeDeltaMat);

            Cv2.InRange(_timeDeltaMat, _settings.MinBrightness, 255, _timeDeltaMat);

            return _timeDeltaMat;
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
            var components = Cv2.ConnectedComponentsEx(mat, PixelConnectivity.Connectivity4);

            for (int i = potentialBalls.Count - 1; i >= 0; i--)
            {
                var potentialBall = potentialBalls[i];

                //If the potential ball cannot be found on this 'noiseless' mat, then it was added by noise that was deleted on the erosion
                if (!components.Blobs.Any(b => Math.Pow(potentialBall.X - b.Centroid.X, 2)
                                               + Math.Pow(potentialBall.Y - b.Centroid.Y, 2) < maxDoubleCheckBallCenterSquaredDistance))
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
        private void GetPotentialBalls(Mat bgDiffMat, out List<Accord.Point> potentialBalls)
        {
            var minPlayerArea = _settings.MinPlayerArea.Value;
            var components = Cv2.ConnectedComponentsEx(bgDiffMat, PixelConnectivity.Connectivity4);

            potentialBalls = null;

            foreach (var blob in components.Blobs)
            {
                if (blob.Width < _size.Width || blob.Height < _size.Height)
                {
                    if (blob.Area < minPlayerArea && blob.Area > _dilationCircleArea)
                    {
                        var roundness = (double)blob.Height / blob.Width;

                        if (roundness < 2d && roundness > 0.5d)
                        {
                            if (potentialBalls == null)
                            {
                                potentialBalls = new List<Accord.Point>();
                            }

                            potentialBalls.Add(new Accord.Point((float)blob.Centroid.X, (float)blob.Centroid.Y));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the balls from the given current mat.
        /// </summary>
        /// <param name="previousMat">The previous mat.</param>
        /// <param name="currentMat">The current mat.</param>
        /// <param name="background">The background.</param>
        /// <param name="players">The players.</param>
        public List<Accord.Point> Extract(out Mat gizmoMat)
        {
            gizmoMat = null;
            _currentFrameId = ExtractionArguments.FrameId;
            _gizmoCount = 0;

            //Same as below comment, no need to dispose
            var timeDeltaMat = GetTimeDeltaMat(ExtractionArguments.PreviousMat, ExtractionArguments.CurrentMat);
            //Actually it's a reference to _bgGrey, no need to dispose it, will be disposed in the end. There's probably a way to code that makes this more obvious
            var bgDiffMat = GetBackgroundDiffMat(ExtractionArguments.Background, ExtractionArguments.CurrentMat);

            //We remove noise very agressively in order to leave only the players and the ball in this mat
            Cv2.Erode(bgDiffMat, _dilatedMat, _playerErosionCircle);
            Cv2.Dilate(_dilatedMat, _timeAntiDeltaMat, _playerDilationCircle);

            if (_drawGizmos)
            {
                FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_playerBall.jpeg", _timeAntiDeltaMat, FileManager.FrameFolder);
                _gizmoCount++;
            }

            Cv2.BitwiseAnd(timeDeltaMat, bgDiffMat, timeDeltaMat);
            //big dilation so every moving body part gets connected
            Cv2.Dilate(timeDeltaMat, _dilatedMat, _dilationCircle);

            if (_drawGizmos)
            {
                FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_timeDelta.jpeg", _dilatedMat, FileManager.FrameFolder);
                _gizmoCount++;
            }

            //If fake balls close to body become a problem, subtracting the combined player blobs material would probably solve it (at the cost of
            //not detecting balls that become close to body) : might be worth it
            //Good for detecting far balls that have lost shape, and potential player regions
            GetPotentialBalls(_dilatedMat, out var balls);

            if (balls?.Any() == true)
            {
                Cv2.Erode(timeDeltaMat, _erodedMat, _contourErosionCircle);
                Cv2.Dilate(_erodedMat, _dilatedMat, _contourDilationCircle);

                //Remove the balls from the mat so we can use to subtract the players areas 
                var hadPlayers = ExtractPlayersFromUnionMat(_timeAntiDeltaMat);

                Cv2.Subtract(_dilatedMat, _timeAntiDeltaMat, _dilatedMat);

                if (_drawGizmos || _drawPreviews)
                {
                    if (hadPlayers)
                    {
                        Cv2.Erode(_timeAntiDeltaMat, _bgAntiGrey, _erosionGizmoCircle);

                        Cv2.Subtract(_timeAntiDeltaMat, _bgAntiGrey, _bgAntiGrey);
                    }

                    var outIndexer = _gizmoMat.GetIndexer();
                    var playerIndexer = _bgAntiGrey.GetIndexer();
                    var currentFrameIndexer = ExtractionArguments.CurrentMat.GetIndexer();
                    var finalBallsIndexer = _dilatedMat.GetIndexer();

                    for (int j = 0; j < _gizmoMat.Height; j++)
                    {
                        for (int i = 0; i < _gizmoMat.Width; i++)
                        {
                            if (finalBallsIndexer[j, i] > 0)
                            {
                                outIndexer[j, i] = new Vec3b(255, 0, 255);
                            }
                            else
                            {
                                outIndexer[j, i] = hadPlayers && playerIndexer[j, i] > 0 ? new Vec3b(255, 0, 0)
                                                                                         : currentFrameIndexer[j, i];
                            }
                        }
                    }

                    gizmoMat = _gizmoMat;
                }

                if (_drawGizmos)
                {
                    FileManager.WriteTempFile(_currentFrameId.ToString("D6") + "_doubleCheck.jpeg", _dilatedMat, FileManager.FrameFolder);
                    _gizmoCount++;
                }

                DoubleCheckCandidateBallsOnEroded(_dilatedMat, balls);
            }

            return balls;
        }

        /// <summary>
        /// Extracts the players from union mat.
        /// </summary>
        /// <param name="unionMat">The union mat.</param>
        private bool ExtractPlayersFromUnionMat(Mat unionMat)
        {
            var minPlayerArea = _settings.MinPlayerArea.Value;

            var hadPlayers = false;
            var components = Cv2.ConnectedComponentsEx(unionMat, PixelConnectivity.Connectivity4);

            var blobsToKeep = components.Blobs.Where(b => b.Area > 2 * minPlayerArea
                                                          && (b.Width < _size.Width || b.Height < _size.Height));

            if (blobsToKeep.Any())
            {
                _zeros.CopyTo(unionMat);

                hadPlayers = true;

                components.FilterBlobs(_ones, unionMat, blobsToKeep);
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
            _bgDiffMat.Dispose();
            _gizmoMat.Dispose();
            _diffMat.Dispose();
            _dilatedMat.Dispose();
            _erodedMat.Dispose();
            _timeDeltaMat.Dispose();
            _timeAntiDeltaMat.Dispose();
            _bgGrey.Dispose();
            _bgAntiGrey.Dispose();
        }
    }
}
