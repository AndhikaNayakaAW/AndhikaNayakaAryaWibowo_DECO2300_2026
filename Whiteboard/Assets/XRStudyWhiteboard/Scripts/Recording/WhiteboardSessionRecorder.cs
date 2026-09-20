using System;
using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Replays the existing canvas brush with the original input, order and
    /// relative timing. Playback never feeds the live recording listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboardSessionRecorder : MonoBehaviour
    {
        private XRStudyWhiteboardManager manager;
        private WhiteboardCanvas canvas;
        private WhiteboardRecordingSession session;
        private double startTime;
        private int nextEvent;
        private int strokeId;
        private bool hasBoardEvents;
        private WhiteboardTool lastTool;
        private WhiteboardColour lastColour;
        private float stoppedElapsed;

        public WhiteboardSessionState State { get; private set; }
        public bool IsRecording => State == WhiteboardSessionState.Recording;
        public bool IsPlaying => State == WhiteboardSessionState.Playing;
        public bool HasRecording => session != null && hasBoardEvents && !IsRecording;
        public float Duration => IsRecording ? Elapsed : session != null ? session.duration : 0f;
        public float Elapsed => State == WhiteboardSessionState.Idle
            ? stoppedElapsed
            : Mathf.Max(0f, (float)(Time.realtimeSinceStartupAsDouble - startTime));
        public WhiteboardRecordingSession Session => session;
        public event Action StateChanged;

        public void Configure(XRStudyWhiteboardManager boardManager, WhiteboardCanvas boardCanvas)
        {
            if (manager == boardManager && canvas == boardCanvas)
                return;
            Unsubscribe();
            manager = boardManager;
            canvas = boardCanvas;
            if (manager != null)
                manager.StateChanged += OnToolStateChanged;
            if (canvas != null)
                canvas.DrawingEvent += OnDrawingEvent;
        }

        public void StartRecording()
        {
            if (IsRecording || manager == null || canvas == null)
                return;
            if (IsPlaying)
                Stop();

            manager.CancelClear();
            canvas.EndStroke();
            session = new WhiteboardRecordingSession
            {
                startingPixels = canvas.CapturePixels(),
                startingTool = manager.CurrentTool,
                startingColour = manager.CurrentColour
            };
            lastTool = manager.CurrentTool;
            lastColour = manager.CurrentColour;
            hasBoardEvents = false;
            strokeId = 0;
            stoppedElapsed = 0f;
            startTime = Time.realtimeSinceStartupAsDouble;
            State = WhiteboardSessionState.Recording;
            StateChanged?.Invoke();
        }

        public void Stop()
        {
            if (IsRecording)
            {
                // Close the final filtered endpoint before stopping the clock.
                canvas.EndStroke();
                session.duration = Elapsed;
                stoppedElapsed = session.duration;
            }
            else if (IsPlaying)
            {
                stoppedElapsed = Mathf.Min(Elapsed, Duration);
                canvas.ApplyPlaybackEvent(new WhiteboardRecordingEvent
                {
                    type = WhiteboardRecordingEventType.StrokeEnd
                });
            }
            else
                return;

            State = WhiteboardSessionState.Idle;
            StateChanged?.Invoke();
        }

        public void Play()
        {
            if (IsPlaying || IsRecording || !HasRecording || canvas == null || manager == null)
                return;

            manager.CancelClear();
            canvas.EndStroke();
            State = WhiteboardSessionState.Playing;
            canvas.RestorePixels(session.startingPixels);
            manager.ApplyPlaybackSelection(session.startingTool, session.startingColour);
            nextEvent = 0;
            stoppedElapsed = 0f;
            startTime = Time.realtimeSinceStartupAsDouble;
            StateChanged?.Invoke();
            ApplyEventsThrough(0f);
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            float elapsed = Mathf.Min(Elapsed, session.duration);
            ApplyEventsThrough(elapsed);
            if (elapsed >= session.duration && nextEvent >= session.events.Count)
                Stop();
        }

        private void ApplyEventsThrough(float elapsed)
        {
            while (nextEvent < session.events.Count && session.events[nextEvent].timestamp <= elapsed)
            {
                WhiteboardRecordingEvent recordedEvent = session.events[nextEvent++];
                if (recordedEvent.type == WhiteboardRecordingEventType.ToolChange
                    || recordedEvent.type == WhiteboardRecordingEventType.ColourChange)
                {
                    manager.ApplyPlaybackSelection(recordedEvent.tool, recordedEvent.colour);
                }
                else
                {
                    canvas.ApplyPlaybackEvent(recordedEvent);
                }
            }
        }

        private void OnDrawingEvent(WhiteboardRecordingEventType type, Vector2 uv, bool trustedDesktopInput)
        {
            if (!IsRecording)
                return;
            if (type == WhiteboardRecordingEventType.StrokeBegin)
                strokeId++;
            session.events.Add(new WhiteboardRecordingEvent
            {
                timestamp = Elapsed,
                type = type,
                strokeId = strokeId,
                position = uv,
                tool = manager.CurrentTool,
                colour = manager.CurrentColour,
                trustedDesktopInput = trustedDesktopInput
            });
            if (type == WhiteboardRecordingEventType.StrokeBegin || type == WhiteboardRecordingEventType.Clear)
                hasBoardEvents = true;
        }

        private void OnToolStateChanged()
        {
            if (!IsRecording)
                return;
            if (lastTool != manager.CurrentTool)
                AddSelectionEvent(WhiteboardRecordingEventType.ToolChange);
            if (lastColour != manager.CurrentColour)
                AddSelectionEvent(WhiteboardRecordingEventType.ColourChange);
            lastTool = manager.CurrentTool;
            lastColour = manager.CurrentColour;
        }

        private void AddSelectionEvent(WhiteboardRecordingEventType type)
        {
            session.events.Add(new WhiteboardRecordingEvent
            {
                timestamp = Elapsed,
                type = type,
                strokeId = strokeId,
                tool = manager.CurrentTool,
                colour = manager.CurrentColour
            });
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (manager != null)
                manager.StateChanged -= OnToolStateChanged;
            if (canvas != null)
                canvas.DrawingEvent -= OnDrawingEvent;
        }
    }
}
