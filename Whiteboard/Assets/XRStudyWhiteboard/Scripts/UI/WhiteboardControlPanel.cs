using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    /// <summary>Refines the authored board UI while retaining its existing actions and references.</summary>
    public sealed class WhiteboardControlPanel : MonoBehaviour
    {
        private XRStudyWhiteboardManager manager;
        private WhiteboardSessionRecorder recorder;
        private TMP_Text sessionStatus;
        private TMP_Text sessionHint;
        private Button recordButton;
        private Button playButton;
        private Button[] drawingButtons;
        private bool built;
        private int displayedSecond = -1;
        private WhiteboardSessionState displayedState;
        private bool displayedRecording;

        public void Initialize(XRStudyWhiteboardManager whiteboardManager)
        {
            if (built || whiteboardManager == null)
                return;
            manager = whiteboardManager;
            recorder = manager.Recorder;
            Transform panel = transform.Find("ToolPanel");
            if (panel == null || recorder == null)
                return;
            built = true;
            GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 800f);
            Canvas canvas = GetComponent<Canvas>();
            canvas.worldCamera = Camera.main;
            StudyUiStyle.SetRect(panel, Vector2.zero, new Vector2(580f, 800f));
            StudyUiStyle.StyleSurface(panel.GetComponent<Image>(), StudyUiStyle.Panel, true);

            StudyUiStyle.CreateText(panel, "Brand", "XR STUDY", new Vector2(0f, 357f), new Vector2(500f, 25f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Accent).characterSpacing = 4f;
            Text(panel, "Title", "WHITEBOARD", new Vector2(0f, 313f), new Vector2(500f, 50f), 36f, StudyUiStyle.Text).fontStyle = FontStyles.Bold;
            Text(panel, "Subtitle", "Today's study session", new Vector2(0f, 268f), new Vector2(500f, 32f), 21f, StudyUiStyle.Muted);
            Text(panel, "ColourHeading", "COLOUR", new Vector2(0f, 208f), new Vector2(500f, 27f), 17f, StudyUiStyle.Muted);
            string[] colours = { "Black", "Red", "Blue", "Green" };
            for (int i = 0; i < colours.Length; i++)
            {
                Transform item = panel.Find(colours[i]);
                StudyUiStyle.SetRect(item, new Vector2(-192f + i * 128f, 151f), new Vector2(116f, 70f));
                StudyUiStyle.StyleButton(item.GetComponent<Button>());
                TMP_Text label = item.Find("Label").GetComponent<TMP_Text>();
                StudyUiStyle.SetRect(label.transform, new Vector2(12f, 0f), new Vector2(80f, 58f));
                StudyUiStyle.StyleText(label, colours[i].ToUpperInvariant(), 17f, TextAlignmentOptions.Center, StudyUiStyle.Text);
                Transform swatch = item.Find("Swatch");
                StudyUiStyle.SetRect(swatch, new Vector2(-42f, 0f), new Vector2(16f, 25f));
                StudyUiStyle.StyleSurface(swatch.GetComponent<Image>(), XRStudyWhiteboardManager.GetColour((WhiteboardColour)i));
            }

            Text(panel, "ToolHeading", "TOOL", new Vector2(0f, 82f), new Vector2(500f, 26f), 17f, StudyUiStyle.Muted);
            LayoutButton(panel.Find("Marker"), new Vector2(-130f, 30f), new Vector2(244f, 65f), "MARKER");
            LayoutButton(panel.Find("Eraser"), new Vector2(130f, 30f), new Vector2(244f, 65f), "ERASER");

            Transform status = panel.Find("Status");
            StudyUiStyle.SetRect(status, new Vector2(0f, -39f), new Vector2(500f, 40f));
            status.Find("ColourStatus").gameObject.SetActive(false);
            status.Find("InputStatus").gameObject.SetActive(false);
            TMP_Text current = status.Find("ToolStatus").GetComponent<TMP_Text>();
            StudyUiStyle.SetRect(current.transform, Vector2.zero, new Vector2(500f, 36f));
            StudyUiStyle.StyleText(current, "", 21f, TextAlignmentOptions.Left, StudyUiStyle.Accent);
            status.GetComponent<WhiteboardStatusDisplay>().InitializeCompact(current);
            status.GetComponent<WhiteboardStatusDisplay>().Refresh(manager);

            StudyUiStyle.CreateText(panel, "SessionHeading", "SESSION", new Vector2(0f, -101f), new Vector2(500f, 26f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            sessionStatus = StudyUiStyle.CreateText(panel, "SessionStatus", "READY TO RECORD", new Vector2(0f, -143f), new Vector2(500f, 36f), 23f, TextAlignmentOptions.Left, StudyUiStyle.Text);
            recordButton = StudyUiStyle.CreateButton(panel, "RecordSession", "RECORD", new Vector2(-130f, -201f), new Vector2(244f, 64f), RecordOrStop);
            playButton = StudyUiStyle.CreateButton(panel, "PlaySession", "PLAY", new Vector2(130f, -201f), new Vector2(244f, 64f), Play);
            AddSessionIcon(recordButton, StudySessionIcon.Kind.Record);
            AddSessionIcon(playButton, StudySessionIcon.Kind.Play);
            sessionHint = StudyUiStyle.CreateText(panel, "SessionHint", "Records board actions and timing", new Vector2(0f, -252f), new Vector2(500f, 32f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            LayoutButton(panel.Find("ClearBoard"), new Vector2(0f, -326f), new Vector2(500f, 62f), "CLEAR BOARD");

            Transform confirmation = panel.Find("ClearConfirmation");
            if (confirmation != null)
            {
                StudyUiStyle.SetRect(confirmation, Vector2.zero, new Vector2(580f, 800f));
                StudyUiStyle.StyleSurface(confirmation.GetComponent<Image>(), StudyUiStyle.Panel, true);
                confirmation.GetComponent<Image>().raycastTarget = true;
                Text(confirmation, "ConfirmTitle", "Clear the whiteboard?", new Vector2(0f, 66f), new Vector2(500f, 60f), 31f, StudyUiStyle.Text).alignment = TextAlignmentOptions.Center;
                StudyUiStyle.CreateText(confirmation, "ConfirmHint", "This removes the current board notes.", new Vector2(0f, 6f), new Vector2(500f, 40f), 20f, TextAlignmentOptions.Center, StudyUiStyle.Muted);
                LayoutButton(confirmation.Find("Cancel"), new Vector2(-130f, -77f), new Vector2(244f, 64f), "KEEP NOTES");
                LayoutButton(confirmation.Find("Confirm"), new Vector2(130f, -77f), new Vector2(244f, 64f), "CLEAR BOARD");
                confirmation.SetAsLastSibling();
            }
            drawingButtons = new[]
            {
                panel.Find("Black").GetComponent<Button>(), panel.Find("Red").GetComponent<Button>(),
                panel.Find("Blue").GetComponent<Button>(), panel.Find("Green").GetComponent<Button>(),
                panel.Find("Marker").GetComponent<Button>(), panel.Find("Eraser").GetComponent<Button>(),
                panel.Find("ClearBoard").GetComponent<Button>()
            };
            recorder.StateChanged += RefreshSession;
            manager.StateChanged += RefreshSelection;
            RefreshSelection();
            RefreshSession();
        }

        private void Update()
        {
            if (!built || recorder == null)
                return;
            if (GetComponent<Canvas>().worldCamera == null)
                GetComponent<Canvas>().worldCamera = Camera.main;
            int second = Mathf.FloorToInt(recorder.Elapsed);
            if (second != displayedSecond || displayedState != recorder.State || displayedRecording != recorder.HasRecording)
                RefreshSession();
        }

        private void OnDestroy()
        {
            if (recorder != null)
                recorder.StateChanged -= RefreshSession;
            if (manager != null)
                manager.StateChanged -= RefreshSelection;
        }

        private void RecordOrStop()
        {
            if (recorder.IsRecording || recorder.IsPlaying)
                recorder.Stop();
            else
                recorder.StartRecording();
            ControllerHaptics.PulseRightController();
        }

        private void Play()
        {
            recorder.Play();
            ControllerHaptics.PulseRightController();
        }

        private void RefreshSession()
        {
            if (!built)
                return;
            displayedSecond = Mathf.FloorToInt(recorder.Elapsed);
            displayedState = recorder.State;
            displayedRecording = recorder.HasRecording;
            bool busy = recorder.IsRecording || recorder.IsPlaying;
            string recordLabel = busy ? "STOP" : recorder.HasRecording ? "NEW RECORDING" : "RECORD";
            recordButton.GetComponentInChildren<TMP_Text>().text = recordLabel;
            recordButton.GetComponentInChildren<TMP_Text>().fontSize = recordLabel.Length > 8 ? 18f : 21f;
            recordButton.GetComponentInChildren<StudySessionIcon>().SetKind(busy ? StudySessionIcon.Kind.Stop : StudySessionIcon.Kind.Record);
            StudyUiStyle.StyleButton(recordButton, busy);
            playButton.interactable = !busy && recorder.HasRecording;
            playButton.GetComponentInChildren<TMP_Text>().color = playButton.interactable ? StudyUiStyle.Text : StudyUiStyle.Muted;
            playButton.GetComponentInChildren<StudySessionIcon>().color = playButton.interactable ? StudyUiStyle.Text : StudyUiStyle.Muted;
            if (recorder.IsRecording)
            {
                sessionStatus.text = "RECORDING  " + TimeLabel(recorder.Elapsed);
                sessionHint.text = "Draw on the board. Stop to finish.";
            }
            else if (recorder.IsPlaying)
            {
                sessionStatus.text = "PLAYING  " + TimeLabel(recorder.Elapsed) + " / " + TimeLabel(recorder.Duration);
                sessionHint.text = "Replaying your board session";
            }
            else
            {
                sessionStatus.text = recorder.HasRecording ? "SESSION READY  " + TimeLabel(recorder.Duration) : "READY TO RECORD";
                sessionHint.text = recorder.HasRecording ? "Play to revisit your board session" : "Records board actions and timing";
            }
            sessionStatus.color = busy ? StudyUiStyle.Accent : StudyUiStyle.Text;
            foreach (Button button in drawingButtons)
                button.interactable = !recorder.IsPlaying;
        }

        private void RefreshSelection()
        {
            if (manager == null)
                return;
            Transform panel = transform.Find("ToolPanel");
            foreach (WhiteboardColour colour in System.Enum.GetValues(typeof(WhiteboardColour)))
            {
                Transform target = panel.Find(colour.ToString());
                bool selected = manager.CurrentColour == colour;
                StudyUiStyle.StyleButton(target.GetComponent<Button>(), selected);
                target.Find("Label").GetComponent<TMP_Text>().text = colour.ToString().ToUpperInvariant() + (selected ? "\n<size=11>ACTIVE</size>" : "");
            }
            foreach (WhiteboardTool tool in System.Enum.GetValues(typeof(WhiteboardTool)))
            {
                Transform target = panel.Find(tool.ToString());
                bool selected = manager.CurrentTool == tool;
                StudyUiStyle.StyleButton(target.GetComponent<Button>(), selected);
                target.Find("Label").GetComponent<TMP_Text>().text = tool.ToString().ToUpperInvariant() + (selected ? "\n<size=12>ACTIVE</size>" : "");
            }
        }

        private static string TimeLabel(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color colour)
        {
            TMP_Text text = parent.Find(name).GetComponent<TMP_Text>();
            StudyUiStyle.SetRect(text.transform, position, size);
            StudyUiStyle.StyleText(text, value, fontSize, TextAlignmentOptions.Left, colour);
            return text;
        }

        private static void LayoutButton(Transform target, Vector2 position, Vector2 size, string label)
        {
            StudyUiStyle.SetRect(target, position, size);
            StudyUiStyle.StyleButton(target.GetComponent<Button>());
            TMP_Text text = target.Find("Label").GetComponent<TMP_Text>();
            StudyUiStyle.SetRect(text.transform, Vector2.zero, size - new Vector2(20f, 10f));
            StudyUiStyle.StyleText(text, label, 21f, TextAlignmentOptions.Center, StudyUiStyle.Text);
        }

        private static void AddSessionIcon(Button button, StudySessionIcon.Kind kind)
        {
            GameObject icon = new GameObject("StateIcon", typeof(RectTransform), typeof(CanvasRenderer));
            icon.transform.SetParent(button.transform, false);
            StudyUiStyle.SetRect(icon.transform, new Vector2(-96f, 0f), new Vector2(16f, 16f));
            StudySessionIcon graphic = icon.AddComponent<StudySessionIcon>();
            graphic.color = StudyUiStyle.Text;
            graphic.raycastTarget = false;
            graphic.SetKind(kind);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            StudyUiStyle.SetRect(text.transform, new Vector2(11f, 0f), new Vector2(198f, 54f));
        }
    }
}
