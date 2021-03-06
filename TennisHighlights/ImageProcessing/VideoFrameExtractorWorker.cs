﻿using OpenCvSharp;
using System;
using System.Threading.Tasks;
using TennisHighlights.Utils;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The video frame extractor worker
    /// </summary>
    public class VideoFrameExtractorWorker
    {
        /// <summary>
        /// Gets a value indicating whether this instance is busy.
        /// </summary>
        public bool IsBusy => _mat != null;
        /// <summary>
        /// The assigned frame index
        /// </summary>
        private int _assignedFrameIndex;
        /// <summary>
        /// The mat the frame shall be stored into
        /// </summary>
        private BusyMat _mat;
        /// <summary>
        /// The video frame extractor
        /// </summary>
        private readonly VideoFrameExtractor _videoFrameExtractor;
        /// <summary>
        /// The resize mat
        /// </summary>
        private BusyMat _resizeMat;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoFrameExtractorWorker"/> class.
        /// </summary>
        /// <param name="videoFrameExtractor">The video frame extractor.</param>
        public VideoFrameExtractorWorker(VideoFrameExtractor videoFrameExtractor) => _videoFrameExtractor = videoFrameExtractor;

        /// <summary>
        /// Assigns the frame.
        /// </summary>
        /// <param name="frameIndex">Index of the frame.</param>
        /// <param name="mat">The mat.</param>
        public void AssignFrame(int frameIndex, BusyMat mat, BusyMat resizeMat)
        {
            _assignedFrameIndex = frameIndex;
            _mat = mat;
            _resizeMat = resizeMat;
        }

        /// <summary>
        /// Processes the frame.
        /// </summary>
        public void ProcessFrame()
        {
            Task.Run(() =>
            {
                try
                {
                    if (_videoFrameExtractor.IsResizingFrames)
                    {
                        Cv2.Resize(_mat.Mat, _resizeMat.Mat, _videoFrameExtractor.TargetSize, 0, 0, InterpolationFlags.Nearest);
                    }

                    if (_resizeMat == null)
                    {
                        _videoFrameExtractor.AddFrame(_assignedFrameIndex, _mat, null);
                    }
                    else
                    {
                        _videoFrameExtractor.AddFrame(_assignedFrameIndex, _resizeMat, _mat);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, e.ToString());
                }
                finally
                {
                    _mat = null;
                }
            });
        }
    }
}
