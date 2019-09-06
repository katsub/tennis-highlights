using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The frame disposer
    /// </summary>
    public class FrameDisposer : IDisposable
    {
        /// <summary>
        /// The video balls extractor
        /// </summary>
        private readonly VideoBallsExtractor _videoBallsExtractor;
        /// <summary>
        /// The frame ball extractors
        /// </summary>
        private readonly List<FrameBallExtractor> _frameBallExtractors;
        /// <summary>
        /// The background extractor
        /// </summary>
        private readonly BackgroundExtractor _backgroundExtractor;
        /// <summary>
        /// The video frame extractor
        /// </summary>
        private readonly VideoFrameExtractor _videoFrameExtractor;
        /// <summary>
        /// The is disposed
        /// </summary>
        private bool _isDisposed;
        /// <summary>
        /// Gets the last disposed frame.
        /// </summary>
        public int LastDisposedFrame { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameDisposer"/> class.
        /// </summary>
        /// <param name="videoBallsExtractor">The video balls extractor.</param>
        /// <param name="ballExtractors">The ball extractors.</param>
        /// <param name="backgroundExtractor">The background extractor.</param>
        /// <param name="videoFrameExtractor">The video frame extractor.</param>
        public FrameDisposer(VideoBallsExtractor videoBallsExtractor, List<FrameBallExtractor> ballExtractors, BackgroundExtractor backgroundExtractor, VideoFrameExtractor videoFrameExtractor)
        {
            _videoFrameExtractor = videoFrameExtractor;
            _videoBallsExtractor = videoBallsExtractor;
            _frameBallExtractors = ballExtractors;
            _backgroundExtractor = backgroundExtractor;
        }

        /// <summary>
        /// Disposes the frames in background task.
        /// </summary>
        public void DisposeFramesInBackgroundTask() => Task.Run(() => DisposeFrames());

        /// <summary>
        /// Disposes the frames.
        /// </summary>
        private async void DisposeFrames()
        {
            while (!_isDisposed)
            {
                //We authorize disposal of frames we know are no longer needed by the ball extractors and the background extractor
                var lastDisposableFrameForBallExtractors = _videoBallsExtractor.LastAssignedFrame - 1;

                foreach (var extractor in _frameBallExtractors)
                {
                    //For any extractor working, do not dispose its current frame or its previous frame
                    if (extractor.IsBusy && extractor.ExtractionArguments.FrameId - 1 < lastDisposableFrameForBallExtractors)
                    {
                        lastDisposableFrameForBallExtractors = extractor.ExtractionArguments.FrameId - 1;
                    }
                }

                var lastDisposedFrame = Math.Min(lastDisposableFrameForBallExtractors, _backgroundExtractor.LastBuiltBackground);

                if (lastDisposedFrame > LastDisposedFrame)
                {
                    LastDisposedFrame = lastDisposedFrame;

                    _videoFrameExtractor.DisposeFramesBefore(LastDisposedFrame);

                    _backgroundExtractor.DisposeBackgroundOlderThan(LastDisposedFrame);
                }

                await Task.Delay(300);
            }
        }

        /// <summary>
        /// Disposes this instance's unmanaged resources.
        /// </summary>
        public void Dispose() => _isDisposed = true;
    }
}
