﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TennisHighlights.Annotation;
using TennisHighlights.Utils;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The video balls extractor: the main class for extracting balls from frames, basically manages the videoFrameExtractor (which extracts frames from
    /// the video), the frameBallExtractors (which extract balls from the frames) and the background extractor (which builds backgrounds that are used in 
    /// the ball extraction process).
    /// </summary>
    public class VideoBallsExtractor
    {
        /// <summary>
        /// The balls per frame
        /// </summary>
        private readonly List<Accord.Point>[] _ballsPerFrame;
        /// <summary>
        /// The ball extractors
        /// </summary>
        private readonly List<FrameBallExtractor> _ballExtractors;
        /// <summary>
        /// The target size
        /// </summary>
        private readonly OpenCvSharp.Size _targetSize;
        /// <summary>
        /// The settings
        /// </summary>
        private readonly TennisHighlightsSettings _settings;
        /// <summary>
        /// The video information
        /// </summary>
        private readonly VideoInfo _videoInfo;
        /// <summary>
        /// The timer
        /// </summary>
        private readonly Timer _timer;
        /// <summary>
        /// The frame extractor
        /// </summary>
        private VideoFrameExtractor _frameExtractor;
        /// <summary>
        /// The background extractor
        /// </summary>
        private BackgroundExtractor _backgroundExtractor;
        /// <summary>
        /// The frame disposer
        /// </summary>
        private FrameDisposer _frameDisposer;
        /// <summary>
        /// The stopwatch
        /// </summary>
        private readonly Stopwatch _stopwatch = new Stopwatch();
        /// <summary>
        /// The processed file log
        /// </summary>
        private readonly ProcessedFileLog _processedFileLog;
        /// <summary>
        /// The progress update action
        /// </summary>
        private readonly Action<Bitmap, int> _progressUpdateAction;
        /// <summary>
        /// The check if cancel requested, signals the user has requested the cancelling of the extraction.
        /// </summary>
        private readonly Func<bool> _checkIfCancelRequested;
        /// <summary>
        /// The should send update to GUI
        /// </summary>
        private bool _shouldSendUpdateToGUI;
        /// <summary>
        /// The updates without sending preview image
        /// </summary> 
        private int _updatesWithoutSendingPreviewImage;

        /// <summary>
        /// The frames processed
        /// </summary>
        public int FramesProcessed { get; private set; }
        /// <summary>
        /// Gets the last assigned frame.
        /// </summary>  
        public int LastAssignedFrame { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoBallsExtractor" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="processedFileLog">The processed file log.</param>
        /// <param name="checkIfCancelRequested">The checkif cancel requested.</param>
        /// <param name="progressUpdateAction">The progress update action.</param>
        public VideoBallsExtractor(TennisHighlightsSettings settings, VideoInfo videoInfo, ProcessedFileLog processedFileLog,
                                   Func<bool> checkIfCancelRequested = null, Action<Bitmap, int> progressUpdateAction = null)
        {
            _settings = settings;
            _videoInfo = videoInfo;
            _processedFileLog = processedFileLog;
            _progressUpdateAction = progressUpdateAction;
            _checkIfCancelRequested = checkIfCancelRequested;
            _ballsPerFrame = new List<Accord.Point>[_videoInfo.TotalFrames];

            _targetSize = settings.General.GetTargetSize(_videoInfo);

            ResolutionDependentParameter.SetTargetResolutionHeight(_targetSize.Height);
            FrameBallExtractor.AllocateResolutionDependentMats();

            _timer = new Timer
            {
                Interval = 5000,
                // Have the timer fire repeated events (true is the default)
                AutoReset = true,
                // Start the timer
                Enabled = true
            };

            void onExtractionOver(int frameId, List<Accord.Point> balls)
            {
                //Not thread safe, but if used only for display it's okay
                FramesProcessed++;

                if (balls != null && balls.Count > 0)
                {
                    _ballsPerFrame[frameId] = balls;
                }
            }

            FrameBallExtractor.SetSize(_targetSize);

            _ballExtractors = Enumerable.Range(1, _settings.General.BallExtractionWorkers)
                                        .Select(o => new FrameBallExtractor(settings.BallDetection, settings.General.DrawGizmos, !settings.General.DisableImagePreview, onExtractionOver)).ToList();
        }

        /// <summary>
        /// Extracts the balls in background task.
        /// </summary>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="previousMat">The previous mat.</param>
        /// <param name="currentMat">The current mat.</param>
        /// <param name="background">The background.</param>
        private async Task ExtractBallsInBackgroundTask(int frameId, MatOfByte3 previousMat, MatOfByte3 currentMat, MatOfByte3 background)
        {
            bool AssignExtractor(out FrameBallExtractor freeExtractor)
            {
                freeExtractor = null;

                freeExtractor = _ballExtractors.FirstOrDefault(e => !e.IsBusy);

                if (freeExtractor != null)
                {
                    Action<Bitmap> onGizmoDrawn = null;

                    if (_shouldSendUpdateToGUI)
                    {
                        _shouldSendUpdateToGUI = false;

                        _updatesWithoutSendingPreviewImage++;

                        if (_updatesWithoutSendingPreviewImage % 3 == 0)
                        {
                            onGizmoDrawn = (bitmap) =>
                            {
                                if (!_checkIfCancelRequested.Invoke() == true)
                                {
                                    _progressUpdateAction?.Invoke(bitmap, frameId);
                                }
                            };
                        }
                        else
                        {
                            _progressUpdateAction?.Invoke(null, frameId);
                        }
                    }

                    freeExtractor.AssignExtraction(new FrameExtractionArguments(frameId, previousMat, currentMat, background, onGizmoDrawn));
                }

                return freeExtractor != null;
            }

            var warned = false;

            FrameBallExtractor extractor = null;

            while (!AssignExtractor(out extractor))
            {
                if (!warned)
                {
                    warned = true;
                }

                await Task.Delay(1000);
            }

            Task.Run(() => extractor.ExtractAndDrawGizmos());
        }

        /// <summary>
        /// Gets the balls per frame dictionary.
        /// </summary>
        private Dictionary<int, List<Accord.Point>> GetBallsPerFrameDictionary()
        {
            var dico = new Dictionary<int, List<Accord.Point>>();

            for (int index = 0; index < _ballsPerFrame.Length; index++)
            {
                var frame = _ballsPerFrame[index];

                if (frame != null)
                {
                    dico.Add(index, frame);
                }
            }

            return dico;
        }

        /// <summary>
        /// Gets the balls per frame.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="videoInfo">The video information.</param>
        /// <param name="drawGizmos">if set to <c>true</c> [draw gizmos].</param>
        public async Task<Dictionary<int, List<Accord.Point>>> GetBallsPerFrame()
        {
            _stopwatch.Start();

            var lastParsedFrame = -1;

            //Gets the cached balls
            foreach (var ball in _processedFileLog.Balls)
            {
                _ballsPerFrame[ball.Key] = ball.Value;
            }

            lastParsedFrame = _processedFileLog.Balls.LastOrDefault().Key;

            //If all frames have already been parsed, return the results
            if (lastParsedFrame >= _videoInfo.TotalFrames)
            {
                return GetBallsPerFrameDictionary();
            }

            using (_frameExtractor = new VideoFrameExtractor(_settings.General.AnalysedVideoPath, _targetSize, _videoInfo))
            {
                _frameExtractor.ExtractFramesInBackgroundTask();

                var previousMat = await _frameExtractor.GetFrameAsync(0);

                var initialFrame = _settings.General.GetFirstFrameToProcess(_videoInfo);
                var lastFrame = _settings.General.GetFinalFrameToProcess(_videoInfo);

                using (_backgroundExtractor = new BackgroundExtractor(_frameExtractor, _targetSize, initialFrame, _settings.BackgroundExtraction, _videoInfo,
                                                                      lastParsedFrame, lastFrame))
                {
                    _backgroundExtractor.ExtractBackgroundsInBackgroundTask();

                    using (_frameDisposer = new FrameDisposer(this, _ballExtractors, _backgroundExtractor, _frameExtractor))
                    {
                        _frameDisposer.DisposeFramesInBackgroundTask();

                        int i = 0;

                        _timer.Elapsed += Timer_Elapsed;

                        //TODO: the videoframeextractor should be set up to start where it had stopped instead of frame skipping till it gets to that point
                        //Frame skipping is kinda quick so that's not critical, but it's an improvement
                        for (i = 1; i < _videoInfo.TotalFrames; i++)
                        {
                            if (i >= lastFrame || _checkIfCancelRequested?.Invoke() == true) { break; }

                            var calculateBalls = i > lastParsedFrame && i > initialFrame;
                            var nextFrameCalculateBalls = i + 1 > lastParsedFrame && i + 1 > initialFrame;

                            var currentMat = await _frameExtractor.GetFrameAsync(i);

                            if (calculateBalls)
                            {
                                var background = await _backgroundExtractor.GetBackground(i);

                                //Can't do anything without background, so break                                
                                if (background == null)
                                {
                                    Logger.Log(LogType.Warning, "Background was null for frame " + i);
                                    break;
                                }

                                await ExtractBallsInBackgroundTask(i, previousMat, currentMat, background);
                            }
                            else
                            {
                                if (_shouldSendUpdateToGUI)
                                {
                                    _shouldSendUpdateToGUI = false;

                                    if (!_checkIfCancelRequested?.Invoke() == true)
                                    {
                                        _progressUpdateAction(null, FramesProcessed);
                                    }
                                }

                                FramesProcessed++;
                            }

                            previousMat = currentMat;

                            LastAssignedFrame = i;

                            //Serialize balls periodically so we won't lose all the work in case the application is suddenly stopped/closed
                            if (i % _settings.General.FramesPerBackup == 0 && calculateBalls)
                            {
                                SerializeBalls();
                            }
                        }

                        //Leaving the using is going to dispose the frames, we wait before the extractors are done using them
                        while (_ballExtractors.Any(b => b.IsBusy)) { await Task.Delay(1000); }

                        _timer.Elapsed -= Timer_Elapsed;
                    }
                }
            }

            SerializeBalls();
            //Needs to be serialized then deserialized so we have a file that yields exactly the same results we're gonna have in that call
            _processedFileLog.ReloadBallsFromSerialization();

            if (_settings.General.DrawGizmos)
            {
                GizmoDrawer.BuildGizmoVideo(_videoInfo.FrameRate, _targetSize, _processedFileLog.Balls.Last().Key);
            }

            return GetBallsPerFrameDictionary();
        }

        /// <summary>
        /// Handles the Elapsed event of the Timer control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs" /> instance containing the event data.</param>
        private void Timer_Elapsed(object source, ElapsedEventArgs e)
        {
            var freeWorkers = _ballExtractors.Where(b => !b.IsBusy).Count();

            _shouldSendUpdateToGUI = true;

            Logger.Log(LogType.Information, "\rFrame[Processed=" + FramesProcessed + ", Loaded=" + _frameExtractor.FramesLoaded
                                                     + ", BG=" + _backgroundExtractor.LastFrameWithBuiltBackground
                                                     + ", Disposed=" + _frameDisposer.LastDisposedFrame + "], Workers[Ball=" + freeWorkers
                                                     + ", Frame=" + _frameExtractor.FreeWorkers
                                                     + "], fps: " + Math.Round((double)LastAssignedFrame / _stopwatch.Elapsed.TotalSeconds, 2) + " ");
        }

        /// <summary>
        /// Serializes the balls.
        /// </summary>
        private void SerializeBalls()
        {
            for (int index = 0; index < _ballsPerFrame.Length; index++)
            {
                var value = _ballsPerFrame[index];

                if (value != null && !_processedFileLog.Balls.ContainsKey(index))
                {
                    _processedFileLog.Balls.Add(index, _ballsPerFrame[index]);
                }
            }

            _processedFileLog.Save();
        }
    }
}
