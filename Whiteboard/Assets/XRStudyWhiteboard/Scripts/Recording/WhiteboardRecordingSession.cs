// script overview: this file defines the data used to record one whiteboard session in memory. it stores the
// starting pixels and tool selection, then timed stroke, tool, colour, and clear events. the recorder uses
// these events to replay the drawing. it does not record microphone audio or camera video and does not save a
// lesson file to disk.

// search: recording | playback | event data | no audio.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system available to this script.
using System;
// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // define the named whiteboard session state choices used instead of unexplained numbers.
    public enum WhiteboardSessionState { Idle, Recording, Playing }
    // define the named whiteboard recording event type choices used instead of unexplained numbers.
    public enum WhiteboardRecordingEventType { StrokeBegin, StrokePoint, StrokeEnd, ToolChange, ColourChange, Clear }

    // apply unity or c sharp metadata: serializable.
    [Serializable]
    // declare the whiteboard recording event class.
    public sealed class WhiteboardRecordingEvent
    // begin the class whiteboard recording event block.
    {
        // timestamp: the time of this recorded event relative to the recording start. keep timestamp as float
        // for later use.
        public float timestamp;
        // keep type as whiteboard recording event type for later use.
        public WhiteboardRecordingEventType type;
        // stroke id: the identifier linking each recorded point to its stroke. keep stroke id as int for later
        // use.
        public int strokeId;
        // position: the recorded normalised writing position when this field belongs to a drawing event. keep
        // position as two-dimensional vector for later use.
        public Vector2 position;
        // keep tool as whiteboard tool for later use.
        public WhiteboardTool tool;
        // keep colour as whiteboard colour for later use.
        public WhiteboardColour colour;
        // keep trusted desktop input as bool for later use.
        public bool trustedDesktopInput;
    // finish the class whiteboard recording event block.
    }

    // one in-memory lesson. the single starting pixel buffer preserves notes
    // already on the board; all subsequent changes are timed input events.
    // no frame captures are taken and nothing is written to the saved scene.
    // apply unity or c sharp metadata: serializable.
    [Serializable]
    // declare the whiteboard recording session class.
    public sealed class WhiteboardRecordingSession
    // begin the class whiteboard recording session block.
    {
        // duration: the total recorded session length in seconds. keep duration as float for later use.
        public float duration;
        // starting tool: the initial marker or eraser choice. keep starting tool as whiteboard tool for later
        // use.
        public WhiteboardTool startingTool;
        // starting colour: the initial named marker colour. keep starting colour as whiteboard colour for
        // later use.
        public WhiteboardColour startingColour;
        // keep events as list<whiteboard recording event>, initially a new list<whiteboard recording event>.
        public List<WhiteboardRecordingEvent> events = new List<WhiteboardRecordingEvent>();
        // starting pixels: the single initial board snapshot restored before timed events replay. keep
        // starting pixels as 32-bit colour[] for later use.
        [NonSerialized] public Color32[] startingPixels;
    // finish the class whiteboard recording session block.
    }
// finish the namespace block.
}
