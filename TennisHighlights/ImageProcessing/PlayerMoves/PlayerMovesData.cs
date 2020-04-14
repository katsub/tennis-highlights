using TennisHighlights.Utils.PoseEstimation;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The player moves data
    /// </summary>
    public class PlayerMovesData
    {
        /// <summary>
        /// Gets the frames per sample.
        /// </summary>
        public int FramesPerSample { get; }
        /// <summary>
        /// The foreground moves
        /// </summary>
        public MoveLabel?[] ForegroundMoves { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerMovesData"/> class.
        /// </summary>
        /// <param name="framesPerSample">The frames per sample.</param>
        /// <param name="foregroundMoves">The foreground moves.</param>
        public PlayerMovesData(VideoInfo videoInfo, ProcessedFileLog log)
        {
            var foregroundMovesDico = TennisMoveDetector.GetForegroundPlayerMovesPerFrame(videoInfo, log);

            FramesPerSample = PlayerMovementAnalyser.GetFramesPerSample(videoInfo.FrameRate);

            ForegroundMoves = new MoveLabel?[videoInfo.TotalFrames];

            foreach (var move in foregroundMovesDico)
            {
                ForegroundMoves[move.Key] = move.Value;
            }
        }
    }
}
