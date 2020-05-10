using System;
using TennisHighlights.Utils;
using TennisHighlights.Utils.PoseEstimation;

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
        public MoveData[] ForegroundMoves { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerMovesData"/> class.
        /// </summary>
        /// <param name="framesPerSample">The frames per sample.</param>
        /// <param name="foregroundMoves">The foreground moves.</param>
        public PlayerMovesData(VideoInfo videoInfo, ProcessedFileLog log)
        {
            FramesPerSample = PlayerMovementAnalyser.GetFramesPerSample(videoInfo.FrameRate);

            try
            {
                var foregroundMovesDico = TennisMoveDetector.GetForegroundPlayerMovesPerFrame(videoInfo, log);

                ForegroundMoves = new MoveData[videoInfo.TotalFrames];

                foreach (var move in foregroundMovesDico)
                {
                    ForegroundMoves[move.Key] = move.Value;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, e.ToString());

                ForegroundMoves = new MoveData[videoInfo.TotalFrames];
            }
        }
    }
}
