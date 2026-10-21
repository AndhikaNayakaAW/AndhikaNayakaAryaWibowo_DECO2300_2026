// script overview: this script records and replays whiteboard actions with their relative times. recording
// starts with a copy of the existing board, then listens for drawing and selection events. playback restores
// that starting board and sends due events through the same canvas brush. the manager blocks live drawing
// while playback is active. this is action recording rather than audio or video capture.

// search: recording | record | stop | play | replay | timing.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system available to this script.
using System;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // replays the existing canvas brush with the original input, order and
    // relative timing. playback never feeds the live recording listeners.
    // apply unity or c sharp metadata: disallow multiple component.
    [DisallowMultipleComponent]
    // declare the whiteboard session recorder class based on unity behaviour.
    public sealed class WhiteboardSessionRecorder : MonoBehaviour
    // begin the class whiteboard session recorder block.
    {
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use.
        private XRStudyWhiteboardManager manager;
        // canvas: the main board pixel surface that receives drawing operations. keep canvas as whiteboard
        // canvas for later use.
        private WhiteboardCanvas canvas;
        // keep session as whiteboard recording session for later use.
        private WhiteboardRecordingSession session;
        // start time: the real-time clock value when recording or playback began. keep start time as double
        // for later use.
        private double startTime;
        // next event: the index of the next recorded event waiting for playback. keep next event as int for
        // later use.
        private int nextEvent;
        // stroke id: the identifier linking each recorded point to its stroke. keep stroke id as int for later
        // use.
        private int strokeId;
        // has board events: whether the session includes a stroke start or clear operation, making it
        // replayable. keep has board events as bool for later use.
        private bool hasBoardEvents;
        // keep last tool as whiteboard tool for later use.
        private WhiteboardTool lastTool;
        // keep last colour as whiteboard colour for later use.
        private WhiteboardColour lastColour;
        // stopped elapsed: the elapsed session time retained after stopping. keep stopped elapsed as float for
        // later use.
        private float stoppedElapsed;

        // expose the state property for reading while keeping writes inside this class.
        public WhiteboardSessionState State { get; private set; }
        // expose is recording as state equals whiteboard session state recording.
        public bool IsRecording => State == WhiteboardSessionState.Recording;
        // expose is playing as state equals whiteboard session state playing.
        public bool IsPlaying => State == WhiteboardSessionState.Playing;
        // expose has recording as session exists and has board events and not (is recording).
        public bool HasRecording => session != null && hasBoardEvents && !IsRecording;
        // expose duration as if is recording, use elapsed, otherwise use if session exists, use session
        // duration, otherwise use 0.
        public float Duration => IsRecording ? Elapsed : session != null ? session.duration : 0f;
        // expose elapsed as if state equals whiteboard session state idle, use stopped elapsed, otherwise use
        // the larger of 0 and the high-precision real-time clock minus start time converted to float.
        public float Elapsed => State == WhiteboardSessionState.Idle
            // if state equals whiteboard session state idle, use stopped elapsed, otherwise use the larger of
            // 0 and the high-precision real-time clock minus start time converted to float.
            ? stoppedElapsed
            // if state equals whiteboard session state idle, use stopped elapsed, otherwise use the larger of
            // 0 and the high-precision real-time clock minus start time converted to float.
            : Mathf.Max(0f, (float)(Time.realtimeSinceStartupAsDouble - startTime));
        // expose session as session.
        public WhiteboardRecordingSession Session => session;
        // declare state changed so other components can subscribe to notifications.
        public event Action StateChanged;

        // method: configure.
        // detach old listeners, connect the manager and canvas, then listen for selection changes and drawing
        // events.
        public void Configure(XRStudyWhiteboardManager boardManager, WhiteboardCanvas boardCanvas)
        // begin the method configure block.
        {
            // run the following statement only when manager equals board manager and canvas equals board
            // canvas.
            if (manager == boardManager && canvas == boardCanvas)
                // leave this method now without running the remaining statements.
                return;
            // call unsubscribe: remove this component from the events it previously listened to, preventing
            // stale callbacks.
            Unsubscribe();
            // set manager to board manager.
            manager = boardManager;
            // set canvas to board canvas.
            canvas = boardCanvas;
            // run the following statement only when manager exists.
            if (manager != null)
                // subscribe on tool state changed to manager state changed so it runs when the event is
                // raised.
                manager.StateChanged += OnToolStateChanged;
            // run the following statement only when canvas exists.
            if (canvas != null)
                // subscribe on drawing event to canvas drawing event so it runs when the event is raised.
                canvas.DrawingEvent += OnDrawingEvent;
        // finish the method configure block.
        }

        // method: start recording.
        // snapshot the current board and selection, reset timing and event state, then begin collecting action
        // events.
        public void StartRecording()
        // begin the method start recording block.
        {
            // run the following statement only when is recording or manager is missing or canvas is missing.
            if (IsRecording || manager == null || canvas == null)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when is playing.
            if (IsPlaying)
                // call stop: finish the open stroke or replay endpoint, retain elapsed time, and return the
                // recorder to idle.
                Stop();

            // manager: cancel clear.
            manager.CancelClear();
            // end the current stroke on canvas so later hits do not join it.
            canvas.EndStroke();
            // set session to a new whiteboard recording session.
            session = new WhiteboardRecordingSession
            // begin the object initializer expression values.
            {
                // set starting pixels to canvas: capture pixels.
                startingPixels = canvas.CapturePixels(),
                // set starting tool to manager current tool.
                startingTool = manager.CurrentTool,
                // set starting colour to manager current colour.
                startingColour = manager.CurrentColour
            // finish the object initializer expression values.
            };
            // set last tool to manager current tool.
            lastTool = manager.CurrentTool;
            // set last colour to manager current colour.
            lastColour = manager.CurrentColour;
            // set has board events to false.
            hasBoardEvents = false;
            // set stroke id to 0.
            strokeId = 0;
            // set stopped elapsed to 0.
            stoppedElapsed = 0f;
            // set start time to the high-precision real-time clock.
            startTime = Time.realtimeSinceStartupAsDouble;
            // set state to whiteboard session state recording.
            State = WhiteboardSessionState.Recording;
            // if state changed exists, schedule after seconds.
            StateChanged?.Invoke();
        // finish the method start recording block.
        }

        // method: stop.
        // finish the open stroke or replay endpoint, retain elapsed time, and return the recorder to idle.
        public void Stop()
        // begin the method stop block.
        {
            // run the following statement only when is recording.
            if (IsRecording)
            // begin the if statement block.
            {
                // close the final filtered endpoint before stopping the clock.
                // end the current stroke on canvas so later hits do not join it.
                canvas.EndStroke();
                // set session duration to elapsed.
                session.duration = Elapsed;
                // set stopped elapsed to session duration.
                stoppedElapsed = session.duration;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (IsPlaying)
            // begin the if statement block.
            {
                // set stopped elapsed to the smaller of elapsed and duration.
                stoppedElapsed = Mathf.Min(Elapsed, Duration);
                // canvas: apply playback event using a new whiteboard recording event.
                canvas.ApplyPlaybackEvent(new WhiteboardRecordingEvent
                // begin the object initializer expression values.
                {
                    // set type to whiteboard recording event type stroke end.
                    type = WhiteboardRecordingEventType.StrokeEnd
                // finish the object initializer expression values.
                });
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
                // leave this method now without running the remaining statements.
                return;

            // set state to whiteboard session state idle.
            State = WhiteboardSessionState.Idle;
            // if state changed exists, schedule after seconds.
            StateChanged?.Invoke();
        // finish the method stop block.
        }

        // method: play.
        // restore the starting board and tool selection, reset the event cursor, and begin replaying the
        // stored session.
        public void Play()
        // begin the method play block.
        {
            // run the following statement only when is playing or is recording or not (has recording) or
            // canvas is missing or manager is missing.
            if (IsPlaying || IsRecording || !HasRecording || canvas == null || manager == null)
                // leave this method now without running the remaining statements.
                return;

            // manager: cancel clear.
            manager.CancelClear();
            // end the current stroke on canvas so later hits do not join it.
            canvas.EndStroke();
            // set state to whiteboard session state playing.
            State = WhiteboardSessionState.Playing;
            // canvas: restore pixels using session starting pixels.
            canvas.RestorePixels(session.startingPixels);
            // manager: apply playback selection using session starting tool, session starting colour.
            manager.ApplyPlaybackSelection(session.startingTool, session.startingColour);
            // set next event to 0.
            nextEvent = 0;
            // set stopped elapsed to 0.
            stoppedElapsed = 0f;
            // set start time to the high-precision real-time clock.
            startTime = Time.realtimeSinceStartupAsDouble;
            // if state changed exists, schedule after seconds.
            StateChanged?.Invoke();
            // apply events through using 0.
            ApplyEventsThrough(0f);
        // finish the method play block.
        }

        // method: update.
        // during playback, apply events whose timestamps have arrived and stop when the session and event list
        // are complete.
        private void Update()
        // begin the method update block.
        {
            // run the following statement only when not (is playing).
            if (!IsPlaying)
                // leave this method now without running the remaining statements.
                return;

            // store elapsed as the smaller of elapsed and session duration.
            float elapsed = Mathf.Min(Elapsed, session.duration);
            // apply events through using elapsed.
            ApplyEventsThrough(elapsed);
            // run the following statement only when elapsed is at least session duration and next event is at
            // least the number of items in session events.
            if (elapsed >= session.duration && nextEvent >= session.events.Count)
                // call stop: finish the open stroke or replay endpoint, retain elapsed time, and return the
                // recorder to idle.
                Stop();
        // finish the method update block.
        }

        // method: apply events through.
        // process recorded events in order up to the requested elapsed time, routing selections to the manager
        // and drawing to the canvas.
        private void ApplyEventsThrough(float elapsed)
        // begin the method apply events through block.
        {
            // repeat while next event is less than the number of items in session events and the entry at next
            // event in session events timestamp is at most elapsed.
            while (nextEvent < session.events.Count && session.events[nextEvent].timestamp <= elapsed)
            // begin the while statement block.
            {
                // store recorded event as the entry at increase next event by one in session events.
                WhiteboardRecordingEvent recordedEvent = session.events[nextEvent++];
                // run the following statement only when recorded event type equals whiteboard recording event
                // type tool change or recorded event type equals whiteboard recording event type colour
                // change.
                if (recordedEvent.type == WhiteboardRecordingEventType.ToolChange
                    // recorded event type equals whiteboard recording event type tool change or recorded event
                    // type equals whiteboard recording event type colour change.
                    || recordedEvent.type == WhiteboardRecordingEventType.ColourChange)
                // begin the if statement block.
                {
                    // manager: apply playback selection using recorded event tool, recorded event colour.
                    manager.ApplyPlaybackSelection(recordedEvent.tool, recordedEvent.colour);
                // finish the if statement block.
                }
                // handle the alternative when the preceding condition is false.
                else
                // begin the else clause block.
                {
                    // canvas: apply playback event using recorded event.
                    canvas.ApplyPlaybackEvent(recordedEvent);
                // finish the else clause block.
                }
            // finish the while statement block.
            }
        // finish the method apply events through block.
        }

        // method: on drawing event.
        // append a timed drawing event with its stroke number, position, tool, and colour only while
        // recording.
        private void OnDrawingEvent(WhiteboardRecordingEventType type, Vector2 uv, bool trustedDesktopInput)
        // begin the method on drawing event block.
        {
            // run the following statement only when not (is recording).
            if (!IsRecording)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when type equals whiteboard recording event type stroke begin.
            if (type == WhiteboardRecordingEventType.StrokeBegin)
                // increase stroke id by one.
                strokeId++;
            // append a new whiteboard recording event to session events.
            session.events.Add(new WhiteboardRecordingEvent
            // begin the object initializer expression values.
            {
                // set timestamp to elapsed.
                timestamp = Elapsed,
                // set type to type.
                type = type,
                // set stroke id to stroke id.
                strokeId = strokeId,
                // set position to uv.
                position = uv,
                // set tool to manager current tool.
                tool = manager.CurrentTool,
                // set colour to manager current colour.
                colour = manager.CurrentColour,
                // set trusted desktop input to trusted desktop input.
                trustedDesktopInput = trustedDesktopInput
            // finish the object initializer expression values.
            });
            // run the following statement only when type equals whiteboard recording event type stroke begin
            // or type equals whiteboard recording event type clear.
            if (type == WhiteboardRecordingEventType.StrokeBegin || type == WhiteboardRecordingEventType.Clear)
                // set has board events to true.
                hasBoardEvents = true;
        // finish the method on drawing event block.
        }

        // method: on tool state changed.
        // compare current and previous selections and append only the tool or colour changes that actually
        // happened.
        private void OnToolStateChanged()
        // begin the method on tool state changed block.
        {
            // run the following statement only when not (is recording).
            if (!IsRecording)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when last tool differs from manager current tool.
            if (lastTool != manager.CurrentTool)
                // add selection event using whiteboard recording event type tool change.
                AddSelectionEvent(WhiteboardRecordingEventType.ToolChange);
            // run the following statement only when last colour differs from manager current colour.
            if (lastColour != manager.CurrentColour)
                // add selection event using whiteboard recording event type colour change.
                AddSelectionEvent(WhiteboardRecordingEventType.ColourChange);
            // set last tool to manager current tool.
            lastTool = manager.CurrentTool;
            // set last colour to manager current colour.
            lastColour = manager.CurrentColour;
        // finish the method on tool state changed block.
        }

        // method: add selection event.
        // append a timed tool or colour event so replay can reproduce the original brush selection.
        private void AddSelectionEvent(WhiteboardRecordingEventType type)
        // begin the method add selection event block.
        {
            // append a new whiteboard recording event to session events.
            session.events.Add(new WhiteboardRecordingEvent
            // begin the object initializer expression values.
            {
                // set timestamp to elapsed.
                timestamp = Elapsed,
                // set type to type.
                type = type,
                // set stroke id to stroke id.
                strokeId = strokeId,
                // set tool to manager current tool.
                tool = manager.CurrentTool,
                // set colour to manager current colour.
                colour = manager.CurrentColour
            // finish the object initializer expression values.
            });
        // finish the method add selection event block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // call stop: finish the open stroke or replay endpoint, retain elapsed time, and return the
            // recorder to idle.
            Stop();
        // finish the method on disable block.
        }

        // method: on destroy.
        // release owned resources or event listeners when unity destroys this component.
        private void OnDestroy()
        // begin the method on destroy block.
        {
            // call unsubscribe: remove this component from the events it previously listened to, preventing
            // stale callbacks.
            Unsubscribe();
        // finish the method on destroy block.
        }

        // method: unsubscribe.
        // remove this component from the events it previously listened to, preventing stale callbacks.
        private void Unsubscribe()
        // begin the method unsubscribe block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // unsubscribe on tool state changed from manager state changed so this object no longer
                // receives the event.
                manager.StateChanged -= OnToolStateChanged;
            // run the following statement only when canvas exists.
            if (canvas != null)
                // unsubscribe on drawing event from canvas drawing event so this object no longer receives the
                // event.
                canvas.DrawingEvent -= OnDrawingEvent;
        // finish the method unsubscribe block.
        }
    // finish the class whiteboard session recorder block.
    }
// finish the namespace block.
}
