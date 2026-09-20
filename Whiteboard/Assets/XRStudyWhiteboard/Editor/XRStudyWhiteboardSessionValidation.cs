using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>Regression checks for visible surface coordinates and timed lesson playback.</summary>
    public static class XRStudyWhiteboardSessionValidation
    {
        [MenuItem("Tools/XR Study Whiteboard/Validate Drawing and Recording", priority = 13)]
        public static void ValidateDrawingAndRecording()
        {
            int checks = 0;
            int errors = 0;
            Run(ref checks, ref errors);
            if (errors > 0)
                throw new InvalidOperationException("Drawing and recording validation failed: " + errors + " of " + checks + " checks.");
            Debug.Log("XR Study Whiteboard drawing and recording validation passed (" + checks + " checks).");
        }

        public static void Run(ref int checks, ref int errors)
        {
            ValidateSurfaceCoordinates(ref checks, ref errors);
            ValidateStrokeBoundaries(ref checks, ref errors);
            ValidateSession(ref checks, ref errors);
        }

        private static void ValidateSurfaceCoordinates(ref int checks, ref int errors)
        {
            GameObject paperObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paperObject.hideFlags = HideFlags.HideAndDontSave;
            boardObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                paperObject.transform.SetPositionAndRotation(new Vector3(3f, 1f, -2f), Quaternion.Euler(7f, 33f, -4f));
                paperObject.transform.localScale = new Vector3(0.55f, 0.018f, 0.38f);
                PaperNoteCanvas paper = paperObject.AddComponent<PaperNoteCanvas>();
                paper.Configure(paperObject.GetComponent<Renderer>(), paperObject.GetComponent<Collider>(), new Vector2(0.55f, 0.38f));
                Transform surface = paperObject.transform.Find("Paper Writing Surface");
                Mesh mesh = surface.GetComponent<MeshFilter>().sharedMesh;
                bool paperMatchesRenderedUv = true;
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    // Inset from the edge to avoid testing floating-point collider boundaries.
                    Vector3 vertex = mesh.vertices[i] * 0.7f;
                    Vector2 expectedUv = Vector2.Lerp(new Vector2(0.5f, 0.5f), mesh.uv[i], 0.7f);
                    Vector3 worldPoint = surface.TransformPoint(vertex);
                    Ray ray = new Ray(worldPoint + paperObject.transform.up * 0.2f, -paperObject.transform.up);
                    paperMatchesRenderedUv &= paper.TryGetUV(ray, 0.5f, out Vector2 actualUv)
                        && Vector2.Distance(actualUv, expectedUv) < 0.001f;
                }
                Check(paperMatchesRenderedUv, "Paper ray coordinates match the visible Quad UVs under rotation and nonuniform scale", ref checks, ref errors);

                boardObject.transform.SetPositionAndRotation(new Vector3(-2f, 1.5f, 4f), Quaternion.Euler(0f, 74f, 0f));
                boardObject.transform.localScale = new Vector3(3f, 1.5f, 0.035f);
                Material originalBoardMaterial = boardObject.GetComponent<Renderer>().sharedMaterial;
                WhiteboardCanvas board = boardObject.AddComponent<WhiteboardCanvas>();
                board.InitializeSurface();
                Check(boardObject.GetComponent<Renderer>().sharedMaterial != originalBoardMaterial,
                    "Initializing a board isolates its texture from the shared source material", ref checks, ref errors);
                Mesh boardMesh = boardObject.GetComponent<MeshFilter>().sharedMesh;
                bool boardMatchesRenderedUv = true;
                int frontVertices = 0;
                for (int i = 0; i < boardMesh.vertexCount; i++)
                {
                    if (boardMesh.normals[i].z < 0.99f)
                        continue;
                    frontVertices++;
                    Vector3 vertex = boardMesh.vertices[i];
                    vertex.x *= 0.7f;
                    vertex.y *= 0.7f;
                    Vector2 expectedUv = Vector2.Lerp(new Vector2(0.5f, 0.5f), boardMesh.uv[i], 0.7f);
                    Vector3 worldPoint = boardObject.transform.TransformPoint(vertex);
                    Ray ray = new Ray(worldPoint + boardObject.transform.forward * 0.2f, -boardObject.transform.forward);
                    boardMatchesRenderedUv &= board.TryGetUV(ray, 0.5f, out Vector2 actualUv)
                        && Vector2.Distance(actualUv, expectedUv) < 0.001f;
                }
                Check(frontVertices == 4 && boardMatchesRenderedUv, "Whiteboard contact matches actual +Z Cube face UVs independently of paper", ref checks, ref errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(paperObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
            }
        }

        private static void ValidateStrokeBoundaries(ref int checks, ref int errors)
        {
            GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject paperObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardObject.hideFlags = HideFlags.HideAndDontSave;
            paperObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                WhiteboardCanvas board = boardObject.AddComponent<WhiteboardCanvas>();
                board.BeginStroke(new Vector2(0.1f, 0.5f));
                board.ContinueStroke(new Vector2(0.15f, 0.5f));
                board.ContinueStroke(new Vector2(0.8f, 0.5f));
                board.ContinueStroke(new Vector2(0.81f, 0.5f));
                board.ContinueStroke(new Vector2(0.82f, 0.5f));
                board.EndStroke();
                Check(IsWhiteAt(board.CapturePixels(), 768, new Vector2(0.5f, 0.5f)),
                    "A lost controller contact never creates a giant whiteboard join", ref checks, ref errors);
                Check(!IsWhiteAt(board.CapturePixels(), 768, new Vector2(0.82f, 0.5f)),
                    "Three stable controller samples reacquire the whiteboard", ref checks, ref errors);

                board.ClearBoard();
                board.BeginStroke(new Vector2(0.2f, 0.8f));
                board.ContinueStroke(new Vector2(0.2f, 0.2f), true);
                board.ContinueStroke(new Vector2(0.8f, 0.2f), true);
                board.EndStroke();
                Check(!IsWhiteAt(board.CapturePixels(), 768, new Vector2(0.8f, 0.2f)),
                    "A fast trusted desktop stroke reaches the final point after a sharp corner", ref checks, ref errors);

                PaperNoteCanvas paper = paperObject.AddComponent<PaperNoteCanvas>();
                paper.Configure(paperObject.GetComponent<Renderer>(), paperObject.GetComponent<Collider>(), new Vector2(0.55f, 0.38f));
                paper.DrawAtUV(new Vector2(0.1f, 0.5f), false, true);
                paper.DrawAtUV(new Vector2(0.25f, 0.5f), false, true);
                paper.EndStroke();
                paper.DrawAtUV(new Vector2(0.75f, 0.5f), false, true);
                paper.DrawAtUV(new Vector2(0.9f, 0.5f), false, true);
                paper.EndStroke();
                Color32[] paperPixels = (Color32[])GetField(paper, "pixels");
                Check(IsWhiteAt(paperPixels, 768, new Vector2(0.5f, 0.5f)),
                    "Releasing and recontacting paper starts a separate stroke", ref checks, ref errors);
                Check(!IsWhiteAt(paperPixels, 768, new Vector2(0.25f, 0.5f))
                    && !IsWhiteAt(paperPixels, 768, new Vector2(0.9f, 0.5f)),
                    "Paper strokes finish at their contact endpoints", ref checks, ref errors);
                paper.ClearNote();
                paper.DrawAtUV(new Vector2(0.2f, 0.8f), false, true);
                paper.DrawAtUV(new Vector2(0.2f, 0.2f), false, true);
                paper.DrawAtUV(new Vector2(0.8f, 0.2f), false, true);
                paper.EndStroke();
                Check(!IsWhiteAt((Color32[])GetField(paper, "pixels"), 768, new Vector2(0.8f, 0.2f)),
                    "A fast trusted paper stroke reaches the final point after a sharp corner", ref checks, ref errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardObject);
                UnityEngine.Object.DestroyImmediate(paperObject);
            }
        }

        private static void ValidateSession(ref int checks, ref int errors)
        {
            GameObject boardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject managerObject = new GameObject("Session validation");
            boardObject.hideFlags = HideFlags.HideAndDontSave;
            managerObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                WhiteboardCanvas board = boardObject.AddComponent<WhiteboardCanvas>();
                XRStudyWhiteboardManager manager = managerObject.AddComponent<XRStudyWhiteboardManager>();
                manager.SetReferences(board, null, null);
                WhiteboardSessionRecorder recorder = manager.Recorder;
                if (recorder == null)
                {
                    recorder = managerObject.AddComponent<WhiteboardSessionRecorder>();
                    // Edit-mode validation does not invoke the manager's runtime Awake.
                    SetField(manager, "<Recorder>k__BackingField", recorder);
                    recorder.Configure(manager, board);
                }
                recorder.Play();
                Check(!recorder.HasRecording && recorder.State == WhiteboardSessionState.Idle, "Play is unavailable before a recording exists", ref checks, ref errors);
                recorder.StartRecording();
                recorder.Stop();
                recorder.Play();
                Check(!recorder.HasRecording && !recorder.IsPlaying, "An empty recording cannot be played", ref checks, ref errors);

                Line(board, new Vector2(0.12f, 0.12f), new Vector2(0.26f, 0.12f));
                Color32[] baseline = board.CapturePixels();
                recorder.StartRecording();
                WhiteboardRecordingSession firstSession = recorder.Session;
                recorder.StartRecording();
                Check(ReferenceEquals(firstSession, recorder.Session) && recorder.IsRecording, "Repeated Record does not replace an active session", ref checks, ref errors);

                SetElapsed(recorder, 1f);
                manager.SetColour(WhiteboardColour.Black);
                Line(board, new Vector2(0.15f, 0.3f), new Vector2(0.4f, 0.3f));
                SetElapsed(recorder, 2f);
                manager.SetColour(WhiteboardColour.Red);
                Line(board, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.7f));
                SetElapsed(recorder, 3f);
                manager.SetColour(WhiteboardColour.Blue);
                board.BeginStroke(new Vector2(0.77f, 0.55f));
                for (int i = 1; i <= 32; i++)
                {
                    float angle = i / 32f * Mathf.PI * 2f;
                    board.ContinueStroke(new Vector2(0.68f + Mathf.Cos(angle) * 0.09f, 0.55f + Mathf.Sin(angle) * 0.12f), true);
                }
                board.EndStroke();
                SetElapsed(recorder, 4f);
                manager.SetColour(WhiteboardColour.Green);
                board.BeginStroke(new Vector2(0.82f, 0.7f));
                board.ContinueStroke(new Vector2(0.82f, 0.3f), true);
                board.ContinueStroke(new Vector2(0.92f, 0.3f), true);
                board.EndStroke();
                // Include rejected XR pose jumps and reacquisition, which use
                // a different filter path from trusted desktop input.
                board.BeginStroke(new Vector2(0.15f, 0.85f));
                board.ContinueStroke(new Vector2(0.2f, 0.85f));
                board.ContinueStroke(new Vector2(0.7f, 0.85f));
                board.ContinueStroke(new Vector2(0.71f, 0.85f));
                board.ContinueStroke(new Vector2(0.72f, 0.85f));
                board.EndStroke();
                SetElapsed(recorder, 5f);
                manager.SetTool(WhiteboardTool.Eraser);
                Line(board, new Vector2(0.22f, 0.28f), new Vector2(0.22f, 0.32f));
                Color32[] beforeClear = board.CapturePixels();
                SetElapsed(recorder, 6f);
                board.ClearBoard();
                SetElapsed(recorder, 7f);
                manager.SetTool(WhiteboardTool.Marker);
                manager.SetColour(WhiteboardColour.Black);
                Line(board, new Vector2(0.15f, 0.5f), new Vector2(0.25f, 0.5f));
                Line(board, new Vector2(0.75f, 0.5f), new Vector2(0.85f, 0.5f));
                Color32[] expected = board.CapturePixels();
                SetElapsed(recorder, 9f);
                recorder.Stop();
                Check(recorder.HasRecording && recorder.Duration >= 9f, "Stop preserves the recorded explanation and trailing pause", ref checks, ref errors);
                int eventCount = recorder.Session.events.Count;
                bool ordered = true;
                for (int i = 1; i < eventCount; i++)
                    ordered &= recorder.Session.events[i].timestamp >= recorder.Session.events[i - 1].timestamp;
                Check(ordered, "Recording events have chronological timestamps", ref checks, ref errors);

                board.ClearBoard();
                recorder.Play();
                Check(PixelsEqual(baseline, board.CapturePixels()), "Playback restores the pre-recording board snapshot", ref checks, ref errors);
                manager.SetColour(WhiteboardColour.Red);
                board.BeginStroke(new Vector2(0.5f, 0.5f));
                board.ClearBoard();
                Check(manager.CurrentColour == recorder.Session.startingColour && PixelsEqual(baseline, board.CapturePixels()), "Live drawing, colour and clear cannot modify playback", ref checks, ref errors);
                AdvancePlayback(recorder, 0.5f);
                Check(PixelsEqual(baseline, board.CapturePixels()), "Playback preserves the initial pause", ref checks, ref errors);
                AdvancePlayback(recorder, 5.5f);
                Check(PixelsEqual(beforeClear, board.CapturePixels()), "All four colours, circle, L, erasing and controller reacquisition replay pixel-for-pixel", ref checks, ref errors);
                AdvancePlayback(recorder, 6.5f);
                Check(IsBlank(board.CapturePixels()), "Clear is replayed at its recorded time", ref checks, ref errors);
                AdvancePlayback(recorder, 8f);
                Check(PixelsEqual(expected, board.CapturePixels()) && recorder.IsPlaying, "Separate strokes replay without a join and preserve the ending pause", ref checks, ref errors);
                AdvancePlayback(recorder, 10f);
                Check(!recorder.IsPlaying && recorder.Session.events.Count == eventCount, "Playback finishes without recording or duplicating its own events", ref checks, ref errors);
                recorder.Play();
                AdvancePlayback(recorder, 2.5f);
                recorder.Stop();
                Check(!recorder.IsPlaying && recorder.HasRecording, "Stop interrupts playback and preserves the saved session", ref checks, ref errors);
                recorder.StartRecording();
                Check(recorder.IsRecording && !ReferenceEquals(firstSession, recorder.Session) && recorder.Session.events.Count == 0, "A new recording can start after playback", ref checks, ref errors);
                recorder.Stop();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(managerObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
            }
        }

        private static void Line(WhiteboardCanvas canvas, Vector2 start, Vector2 end)
        {
            canvas.BeginStroke(start);
            for (int i = 1; i <= 12; i++)
                canvas.ContinueStroke(Vector2.Lerp(start, end, i / 12f), true);
            canvas.EndStroke();
        }

        private static void SetElapsed(WhiteboardSessionRecorder recorder, float elapsed)
        {
            SetField(recorder, "startTime", Time.realtimeSinceStartupAsDouble - elapsed);
        }

        private static void AdvancePlayback(WhiteboardSessionRecorder recorder, float elapsed)
        {
            SetElapsed(recorder, elapsed);
            typeof(WhiteboardSessionRecorder).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(recorder, null);
        }

        private static void SetField(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private static object GetField(object target, string field)
        {
            return target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        }

        private static bool IsWhiteAt(Color32[] pixels, int width, Vector2 uv)
        {
            int height = pixels.Length / width;
            Color32 pixel = pixels[Mathf.RoundToInt(uv.y * (height - 1)) * width + Mathf.RoundToInt(uv.x * (width - 1))];
            return pixel.r == 255 && pixel.g == 255 && pixel.b == 255;
        }

        private static bool PixelsEqual(Color32[] expected, Color32[] actual)
        {
            if (expected.Length != actual.Length)
                return false;
            for (int i = 0; i < expected.Length; i++)
                if (!expected[i].Equals(actual[i]))
                    return false;
            return true;
        }

        private static bool IsBlank(Color32[] pixels)
        {
            foreach (Color32 pixel in pixels)
                if (pixel.r != 255 || pixel.g != 255 || pixel.b != 255)
                    return false;
            return true;
        }

        private static void Check(bool condition, string label, ref int checks, ref int errors)
        {
            checks++;
            if (condition)
                return;
            errors++;
            Debug.LogError("[XR Study Whiteboard] FAILED: " + label);
        }
    }
}
