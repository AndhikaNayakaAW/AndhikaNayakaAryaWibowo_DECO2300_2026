using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRStudyWhiteboard
{
    public enum WhiteboardSessionState { Idle, Recording, Playing }
    public enum WhiteboardRecordingEventType { StrokeBegin, StrokePoint, StrokeEnd, ToolChange, ColourChange, Clear }

    [Serializable]
    public sealed class WhiteboardRecordingEvent
    {
        public float timestamp;
        public WhiteboardRecordingEventType type;
        public int strokeId;
        public Vector2 position;
        public WhiteboardTool tool;
        public WhiteboardColour colour;
        public bool trustedDesktopInput;
    }

    /// <summary>
    /// One in-memory lesson. The single starting pixel buffer preserves notes
    /// already on the board; all subsequent changes are timed input events.
    /// No frame captures are taken and nothing is written to the saved scene.
    /// </summary>
    [Serializable]
    public sealed class WhiteboardRecordingSession
    {
        public float duration;
        public WhiteboardTool startingTool;
        public WhiteboardColour startingColour;
        public List<WhiteboardRecordingEvent> events = new List<WhiteboardRecordingEvent>();
        [NonSerialized] public Color32[] startingPixels;
    }
}
