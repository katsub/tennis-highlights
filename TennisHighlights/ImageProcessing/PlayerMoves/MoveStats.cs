using System;
using TennisHighlights.ImageProcessing.PlayerMoves;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights
{
    /// <summary>
    /// The move stats
    /// </summary>
    public class MoveStats
    {
        /// <summary>
        /// Gets the player moves data.
        /// </summary>
        public PlayerMovesData PlayerMovesData { get; }
        /// <summary>
        /// Gets the foreground backhands.
        /// </summary>
        public int ForegroundBackhands { get; private set; }
        /// <summary>
        /// Gets the foreground serves.
        /// </summary>
        public int ForegroundServes { get; private set; }
        /// <summary>
        /// Gets the foreground forehands.
        /// </summary>
        public int ForegroundForehands { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveStats"/> class.
        /// </summary>
        public MoveStats(PlayerMovesData playerMovesData) => PlayerMovesData = playerMovesData;

        /// <summary>
        /// Updates these move stats.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="stop">The stop.</param>
        public void Update(int start, int stop)
        {
            stop = (int)Math.Min(stop, PlayerMovesData.ForegroundMoves.Length);

            //Transform into sampled frames
            start = (int)(start / PlayerMovesData.FramesPerSample);
            stop = (int)(stop / PlayerMovesData.FramesPerSample);

            for (int i = start; i <= stop; i++)
            {
                var foregroundMove = PlayerMovesData.ForegroundMoves[i];

                switch (foregroundMove)
                {
                    case MoveLabel.Forehand:
                        ForegroundForehands++;
                        break;
                    case MoveLabel.Backhand:
                        ForegroundBackhands++;
                        break;
                    case MoveLabel.Service:
                        ForegroundServes++;
                        break;
                }
            }
        }
    }
}
