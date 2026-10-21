// script overview: this script arranges and styles the whiteboard control panel and adds the record, stop, and
// play controls. it updates the selected-tool feedback, recording status, elapsed time, and button
// availability. drawing controls are disabled during playback. the manager and recorder perform the actual
// board and session operations.

// search: whiteboard controls | colour | marker | eraser | recording | clear board.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make text mesh pro types available for readable interface labels.
using TMPro;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // refines the authored board ui while retaining its existing actions and references.
    // declare the whiteboard control panel class based on unity behaviour.
    public sealed class WhiteboardControlPanel : MonoBehaviour
    // begin the class whiteboard control panel block.
    {
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use.
        private XRStudyWhiteboardManager manager;
        // keep recorder as whiteboard session recorder for later use.
        private WhiteboardSessionRecorder recorder;
        // keep session status as tmp text for later use.
        private TMP_Text sessionStatus;
        // keep session hint as tmp text for later use.
        private TMP_Text sessionHint;
        // keep record button as button for later use.
        private Button recordButton;
        // keep play button as button for later use.
        private Button playButton;
        // keep drawing buttons as button[] for later use.
        private Button[] drawingButtons;
        // keep built as bool for later use.
        private bool built;
        // keep displayed second as int, initially the negative of 1.
        private int displayedSecond = -1;
        // keep displayed state as whiteboard session state for later use.
        private WhiteboardSessionState displayedState;
        // keep displayed recording as bool for later use.
        private bool displayedRecording;

        // method: initialize.
        // rearrange the existing board controls, add recording actions, and subscribe to manager and recorder
        // updates.
        public void Initialize(XRStudyWhiteboardManager whiteboardManager)
        // begin the method initialize block.
        {
            // run the following statement only when built or whiteboard manager is missing.
            if (built || whiteboardManager == null)
                // leave this method now without running the remaining statements.
                return;
            // set manager to whiteboard manager.
            manager = whiteboardManager;
            // set recorder to manager recorder.
            recorder = manager.Recorder;
            // store panel as the child or resource the text "toolpanel" found in transform.
            Transform panel = transform.Find("ToolPanel");
            // run the following statement only when panel is missing or recorder is missing.
            if (panel == null || recorder == null)
                // leave this method now without running the remaining statements.
                return;
            // set built to true.
            built = true;
            // set the rect transform component attached to this object size delta to a new two-dimensional
            // vector using 580, 800.
            GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 800f);
            // store canvas as the canvas component attached to this object.
            Canvas canvas = GetComponent<Canvas>();
            // set canvas world camera to camera main.
            canvas.worldCamera = Camera.main;
            // study ui style: set rect using panel, the zero two-dimensional vector, a new two-dimensional
            // vector using 580, 800.
            StudyUiStyle.SetRect(panel, Vector2.zero, new Vector2(580f, 800f));
            // study ui style: style surface using the the image component attached to this object component on
            // panel, study ui style panel, true.
            StudyUiStyle.StyleSurface(panel.GetComponent<Image>(), StudyUiStyle.Panel, true);

            // set study ui style: create text using panel, the text "brand", the text "xr study", a new
            // two-dimensional vector using 0, 357, a new two-dimensional vector using 500, 25, 17, text
            // alignment options left, study ui style accent character spacing to 4.
            StudyUiStyle.CreateText(panel, "Brand", "XR STUDY", new Vector2(0f, 357f), new Vector2(500f, 25f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Accent).characterSpacing = 4f;
            // set text using panel, the text "title", the text "whiteboard", a new two-dimensional vector
            // using 0, 313, a new two-dimensional vector using 500, 50, 36, study ui style text font style to
            // font styles bold.
            Text(panel, "Title", "WHITEBOARD", new Vector2(0f, 313f), new Vector2(500f, 50f), 36f, StudyUiStyle.Text).fontStyle = FontStyles.Bold;
            // text using panel, the text "subtitle", the text "today's study session", a new two-dimensional
            // vector using 0, 268, a new two-dimensional vector using 500, 32, 21, study ui style muted.
            Text(panel, "Subtitle", "Today's study session", new Vector2(0f, 268f), new Vector2(500f, 32f), 21f, StudyUiStyle.Muted);
            // text using panel, the text "colourheading", the text "colour", a new two-dimensional vector
            // using 0, 208, a new two-dimensional vector using 500, 27, 17, study ui style muted.
            Text(panel, "ColourHeading", "COLOUR", new Vector2(0f, 208f), new Vector2(500f, 27f), 17f, StudyUiStyle.Muted);
            // store colours as the listed initial values.
            string[] colours = { "Black", "Red", "Blue", "Green" };
            // repeat this loop while i is less than the number of items in colours. increase i by one after
            // each pass.
            for (int i = 0; i < colours.Length; i++)
            // begin the for statement block.
            {
                // store item as the child or resource the entry at i in colours found in panel.
                Transform item = panel.Find(colours[i]);
                // study ui style: set rect using item, a new two-dimensional vector using the negative of 192
                // plus i multiplied by 128, 151, a new two-dimensional vector using 116, 70.
                StudyUiStyle.SetRect(item, new Vector2(-192f + i * 128f, 151f), new Vector2(116f, 70f));
                // study ui style: style button using the the button component attached to this object
                // component on item.
                StudyUiStyle.StyleButton(item.GetComponent<Button>());
                // store label as the the tmp text component attached to this object component on the child or
                // resource the text "label" found in item.
                TMP_Text label = item.Find("Label").GetComponent<TMP_Text>();
                // study ui style: set rect using label transform, a new two-dimensional vector using 12, 0, a
                // new two-dimensional vector using 80, 58.
                StudyUiStyle.SetRect(label.transform, new Vector2(12f, 0f), new Vector2(80f, 58f));
                // study ui style: style text using label, the entry at i in colours: to upper invariant, 17,
                // text alignment options center, study ui style text.
                StudyUiStyle.StyleText(label, colours[i].ToUpperInvariant(), 17f, TextAlignmentOptions.Center, StudyUiStyle.Text);
                // store swatch as the child or resource the text "swatch" found in item.
                Transform swatch = item.Find("Swatch");
                // study ui style: set rect using swatch, a new two-dimensional vector using the negative of
                // 42, 0, a new two-dimensional vector using 16, 25.
                StudyUiStyle.SetRect(swatch, new Vector2(-42f, 0f), new Vector2(16f, 25f));
                // study ui style: style surface using the the image component attached to this object
                // component on swatch, xr study whiteboard manager: get colour using i converted to whiteboard
                // colour.
                StudyUiStyle.StyleSurface(swatch.GetComponent<Image>(), XRStudyWhiteboardManager.GetColour((WhiteboardColour)i));
            // finish the for statement block.
            }

            // text using panel, the text "toolheading", the text "tool", a new two-dimensional vector using 0,
            // 82, a new two-dimensional vector using 500, 26, 17, study ui style muted.
            Text(panel, "ToolHeading", "TOOL", new Vector2(0f, 82f), new Vector2(500f, 26f), 17f, StudyUiStyle.Muted);
            // layout button using the child or resource the text "marker" found in panel, a new
            // two-dimensional vector using the negative of 130, 30, a new two-dimensional vector using 244,
            // 65, the text "marker".
            LayoutButton(panel.Find("Marker"), new Vector2(-130f, 30f), new Vector2(244f, 65f), "MARKER");
            // layout button using the child or resource the text "eraser" found in panel, a new
            // two-dimensional vector using 130, 30, a new two-dimensional vector using 244, 65, the text
            // "eraser".
            LayoutButton(panel.Find("Eraser"), new Vector2(130f, 30f), new Vector2(244f, 65f), "ERASER");

            // store status as the child or resource the text "status" found in panel.
            Transform status = panel.Find("Status");
            // study ui style: set rect using status, a new two-dimensional vector using 0, the negative of 39,
            // a new two-dimensional vector using 500, 40.
            StudyUiStyle.SetRect(status, new Vector2(0f, -39f), new Vector2(500f, 40f));
            // hide and disable the child or resource the text "colourstatus" found in status game object.
            status.Find("ColourStatus").gameObject.SetActive(false);
            // hide and disable the child or resource the text "inputstatus" found in status game object.
            status.Find("InputStatus").gameObject.SetActive(false);
            // store current as the the tmp text component attached to this object component on the child or
            // resource the text "toolstatus" found in status.
            TMP_Text current = status.Find("ToolStatus").GetComponent<TMP_Text>();
            // study ui style: set rect using current transform, the zero two-dimensional vector, a new
            // two-dimensional vector using 500, 36.
            StudyUiStyle.SetRect(current.transform, Vector2.zero, new Vector2(500f, 36f));
            // study ui style: style text using current, the text "", 21, text alignment options left, study ui
            // style accent.
            StudyUiStyle.StyleText(current, "", 21f, TextAlignmentOptions.Left, StudyUiStyle.Accent);
            // the the whiteboard status display component attached to this object component on status:
            // initialize compact using current.
            status.GetComponent<WhiteboardStatusDisplay>().InitializeCompact(current);
            // the the whiteboard status display component attached to this object component on status: refresh
            // using manager.
            status.GetComponent<WhiteboardStatusDisplay>().Refresh(manager);

            // study ui style: create text using panel, the text "sessionheading", the text "session", a new
            // two-dimensional vector using 0, the negative of 101, a new two-dimensional vector using 500, 26,
            // 17, text alignment options left, study ui style muted.
            StudyUiStyle.CreateText(panel, "SessionHeading", "SESSION", new Vector2(0f, -101f), new Vector2(500f, 26f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            // set session status to study ui style: create text using panel, the text "sessionstatus", the
            // text "ready to record", a new two-dimensional vector using 0, the negative of 143, a new
            // two-dimensional vector using 500, 36, 23, text alignment options left, study ui style text.
            sessionStatus = StudyUiStyle.CreateText(panel, "SessionStatus", "READY TO RECORD", new Vector2(0f, -143f), new Vector2(500f, 36f), 23f, TextAlignmentOptions.Left, StudyUiStyle.Text);
            // set record button to study ui style: create button using panel, the text "recordsession", the
            // text "record", a new two-dimensional vector using the negative of 130, the negative of 201, a
            // new two-dimensional vector using 244, 64, record or stop.
            recordButton = StudyUiStyle.CreateButton(panel, "RecordSession", "RECORD", new Vector2(-130f, -201f), new Vector2(244f, 64f), RecordOrStop);
            // set play button to study ui style: create button using panel, the text "playsession", the text
            // "play", a new two-dimensional vector using 130, the negative of 201, a new two-dimensional
            // vector using 244, 64, play.
            playButton = StudyUiStyle.CreateButton(panel, "PlaySession", "PLAY", new Vector2(130f, -201f), new Vector2(244f, 64f), Play);
            // add session icon using record button, study session icon kind record.
            AddSessionIcon(recordButton, StudySessionIcon.Kind.Record);
            // add session icon using play button, study session icon kind play.
            AddSessionIcon(playButton, StudySessionIcon.Kind.Play);
            // set session hint to study ui style: create text using panel, the text "sessionhint", the text
            // "records board actions and timing", a new two-dimensional vector using 0, the negative of 252, a
            // new two-dimensional vector using 500, 32, 17, text alignment options left, study ui style muted.
            sessionHint = StudyUiStyle.CreateText(panel, "SessionHint", "Records board actions and timing", new Vector2(0f, -252f), new Vector2(500f, 32f), 17f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            // layout button using the child or resource the text "clearboard" found in panel, a new
            // two-dimensional vector using 0, the negative of 326, a new two-dimensional vector using 500, 62,
            // the text "clear board".
            LayoutButton(panel.Find("ClearBoard"), new Vector2(0f, -326f), new Vector2(500f, 62f), "CLEAR BOARD");

            // store confirmation as the child or resource the text "clearconfirmation" found in panel.
            Transform confirmation = panel.Find("ClearConfirmation");
            // run the following statement only when confirmation exists.
            if (confirmation != null)
            // begin the if statement block.
            {
                // study ui style: set rect using confirmation, the zero two-dimensional vector, a new
                // two-dimensional vector using 580, 800.
                StudyUiStyle.SetRect(confirmation, Vector2.zero, new Vector2(580f, 800f));
                // study ui style: style surface using the the image component attached to this object
                // component on confirmation, study ui style panel, true.
                StudyUiStyle.StyleSurface(confirmation.GetComponent<Image>(), StudyUiStyle.Panel, true);
                // set the the image component attached to this object component on confirmation raycast target
                // to true.
                confirmation.GetComponent<Image>().raycastTarget = true;
                // set text using confirmation, the text "confirmtitle", the text "clear the whiteboard?", a
                // new two-dimensional vector using 0, 66, a new two-dimensional vector using 500, 60, 31,
                // study ui style text alignment to text alignment options center.
                Text(confirmation, "ConfirmTitle", "Clear the whiteboard?", new Vector2(0f, 66f), new Vector2(500f, 60f), 31f, StudyUiStyle.Text).alignment = TextAlignmentOptions.Center;
                // study ui style: create text using confirmation, the text "confirmhint", the text "this
                // removes the current board notes.", a new two-dimensional vector using 0, 6, a new
                // two-dimensional vector using 500, 40, 20, text alignment options center, study ui style
                // muted.
                StudyUiStyle.CreateText(confirmation, "ConfirmHint", "This removes the current board notes.", new Vector2(0f, 6f), new Vector2(500f, 40f), 20f, TextAlignmentOptions.Center, StudyUiStyle.Muted);
                // layout button using the child or resource the text "cancel" found in confirmation, a new
                // two-dimensional vector using the negative of 130, the negative of 77, a new two-dimensional
                // vector using 244, 64, the text "keep notes".
                LayoutButton(confirmation.Find("Cancel"), new Vector2(-130f, -77f), new Vector2(244f, 64f), "KEEP NOTES");
                // layout button using the child or resource the text "confirm" found in confirmation, a new
                // two-dimensional vector using 130, the negative of 77, a new two-dimensional vector using
                // 244, 64, the text "clear board".
                LayoutButton(confirmation.Find("Confirm"), new Vector2(130f, -77f), new Vector2(244f, 64f), "CLEAR BOARD");
                // confirmation: set as last sibling.
                confirmation.SetAsLastSibling();
            // finish the if statement block.
            }
            // set drawing buttons to a new array containing the listed entries.
            drawingButtons = new[]
            // begin the array initializer expression values.
            {
                // the the button component attached to this object component on the child or resource the text
                // "black" found in panel.
                panel.Find("Black").GetComponent<Button>(), panel.Find("Red").GetComponent<Button>(),
                // the the button component attached to this object component on the child or resource the text
                // "blue" found in panel.
                panel.Find("Blue").GetComponent<Button>(), panel.Find("Green").GetComponent<Button>(),
                // the the button component attached to this object component on the child or resource the text
                // "marker" found in panel.
                panel.Find("Marker").GetComponent<Button>(), panel.Find("Eraser").GetComponent<Button>(),
                // the the button component attached to this object component on the child or resource the text
                // "clearboard" found in panel.
                panel.Find("ClearBoard").GetComponent<Button>()
            // finish the array initializer expression values.
            };
            // subscribe refresh session to recorder state changed so it runs when the event is raised.
            recorder.StateChanged += RefreshSession;
            // subscribe refresh selection to manager state changed so it runs when the event is raised.
            manager.StateChanged += RefreshSelection;
            // call refresh selection: highlight the current colour and tool using the manager as the single
            // state source.
            RefreshSelection();
            // call refresh session: display the current recording state and time, enable playback when a
            // recording exists, and disable drawing choices during replay.
            RefreshSession();
        // finish the method initialize block.
        }

        // method: update.
        // unity calls this once per frame while the component is enabled. process the current input or state
        // described below.
        private void Update()
        // begin the method update block.
        {
            // run the following statement only when not (built) or recorder is missing.
            if (!built || recorder == null)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when the canvas component attached to this object world camera
            // is missing.
            if (GetComponent<Canvas>().worldCamera == null)
                // set the canvas component attached to this object world camera to camera main.
                GetComponent<Canvas>().worldCamera = Camera.main;
            // store second as recorder elapsed rounded downward to an integer.
            int second = Mathf.FloorToInt(recorder.Elapsed);
            // run the following statement only when second differs from displayed second or displayed state
            // differs from recorder state or displayed recording differs from recorder has recording.
            if (second != displayedSecond || displayedState != recorder.State || displayedRecording != recorder.HasRecording)
                // call refresh session: display the current recording state and time, enable playback when a
                // recording exists, and disable drawing choices during replay.
                RefreshSession();
        // finish the method update block.
        }

        // method: on destroy.
        // release owned resources or event listeners when unity destroys this component.
        private void OnDestroy()
        // begin the method on destroy block.
        {
            // run the following statement only when recorder exists.
            if (recorder != null)
                // unsubscribe refresh session from recorder state changed so this object no longer receives
                // the event.
                recorder.StateChanged -= RefreshSession;
            // run the following statement only when manager exists.
            if (manager != null)
                // unsubscribe refresh selection from manager state changed so this object no longer receives
                // the event.
                manager.StateChanged -= RefreshSelection;
        // finish the method on destroy block.
        }

        // method: record or stop.
        // stop an active recording or playback, otherwise start a new recording.
        private void RecordOrStop()
        // begin the method record or stop block.
        {
            // run the following statement only when recorder is recording or recorder is playing.
            if (recorder.IsRecording || recorder.IsPlaying)
                // recorder: stop.
                recorder.Stop();
            // handle the alternative when the preceding condition is false.
            else
                // recorder: start recording.
                recorder.StartRecording();
            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
        // finish the method record or stop block.
        }

        // method: play.
        // ask the recorder to replay the stored board actions and request controller feedback.
        private void Play()
        // begin the method play block.
        {
            // recorder: play.
            recorder.Play();
            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
        // finish the method play block.
        }

        // method: refresh session.
        // display the current recording state and time, enable playback when a recording exists, and disable
        // drawing choices during replay.
        private void RefreshSession()
        // begin the method refresh session block.
        {
            // run the following statement only when not (built).
            if (!built)
                // leave this method now without running the remaining statements.
                return;
            // set displayed second to recorder elapsed rounded downward to an integer.
            displayedSecond = Mathf.FloorToInt(recorder.Elapsed);
            // set displayed state to recorder state.
            displayedState = recorder.State;
            // set displayed recording to recorder has recording.
            displayedRecording = recorder.HasRecording;
            // store busy as recorder is recording or recorder is playing.
            bool busy = recorder.IsRecording || recorder.IsPlaying;
            // store record label as if busy, use the text "stop", otherwise use if recorder has recording, use
            // the text "new recording", otherwise use the text "record".
            string recordLabel = busy ? "STOP" : recorder.HasRecording ? "NEW RECORDING" : "RECORD";
            // set the requested component in the children of record button text to record label.
            recordButton.GetComponentInChildren<TMP_Text>().text = recordLabel;
            // set the requested component in the children of record button font size to if the number of items
            // in record label is greater than 8, use 18, otherwise use 21.
            recordButton.GetComponentInChildren<TMP_Text>().fontSize = recordLabel.Length > 8 ? 18f : 21f;
            // the requested component in the children of record button: set kind using if busy, use study
            // session icon kind stop, otherwise use study session icon kind record.
            recordButton.GetComponentInChildren<StudySessionIcon>().SetKind(busy ? StudySessionIcon.Kind.Stop : StudySessionIcon.Kind.Record);
            // study ui style: style button using record button, busy.
            StudyUiStyle.StyleButton(recordButton, busy);
            // set play button interactable to not (busy) and recorder has recording.
            playButton.interactable = !busy && recorder.HasRecording;
            // set the requested component in the children of play button colour to if play button
            // interactable, use study ui style text, otherwise use study ui style muted.
            playButton.GetComponentInChildren<TMP_Text>().color = playButton.interactable ? StudyUiStyle.Text : StudyUiStyle.Muted;
            // set the requested component in the children of play button colour to if play button
            // interactable, use study ui style text, otherwise use study ui style muted.
            playButton.GetComponentInChildren<StudySessionIcon>().color = playButton.interactable ? StudyUiStyle.Text : StudyUiStyle.Muted;
            // run the following statement only when recorder is recording.
            if (recorder.IsRecording)
            // begin the if statement block.
            {
                // set session status text to the text "recording " plus time label using recorder elapsed.
                sessionStatus.text = "RECORDING  " + TimeLabel(recorder.Elapsed);
                // set session hint text to the text "draw on the board. stop to finish.".
                sessionHint.text = "Draw on the board. Stop to finish.";
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (recorder.IsPlaying)
            // begin the if statement block.
            {
                // set session status text to the text "playing " plus time label using recorder elapsed plus
                // the text " / " plus time label using recorder duration.
                sessionStatus.text = "PLAYING  " + TimeLabel(recorder.Elapsed) + " / " + TimeLabel(recorder.Duration);
                // set session hint text to the text "replaying your board session".
                sessionHint.text = "Replaying your board session";
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // set session status text to if recorder has recording, use the text "session ready " plus
                // time label using recorder duration, otherwise use the text "ready to record".
                sessionStatus.text = recorder.HasRecording ? "SESSION READY  " + TimeLabel(recorder.Duration) : "READY TO RECORD";
                // set session hint text to if recorder has recording, use the text "play to revisit your board
                // session", otherwise use the text "records board actions and timing".
                sessionHint.text = recorder.HasRecording ? "Play to revisit your board session" : "Records board actions and timing";
            // finish the else clause block.
            }
            // set session status colour to if busy, use study ui style accent, otherwise use study ui style
            // text.
            sessionStatus.color = busy ? StudyUiStyle.Accent : StudyUiStyle.Text;
            // visit each button in drawing buttons.
            foreach (Button button in drawingButtons)
                // set button interactable to not (recorder is playing).
                button.interactable = !recorder.IsPlaying;
        // finish the method refresh session block.
        }

        // method: refresh selection.
        // highlight the current colour and tool using the manager as the single state source.
        private void RefreshSelection()
        // begin the method refresh selection block.
        {
            // run the following statement only when manager is missing.
            if (manager == null)
                // leave this method now without running the remaining statements.
                return;
            // store panel as the child or resource the text "toolpanel" found in transform.
            Transform panel = transform.Find("ToolPanel");
            // visit each colour in system enum: get values using the type information for whiteboard colour.
            foreach (WhiteboardColour colour in System.Enum.GetValues(typeof(WhiteboardColour)))
            // begin the for each statement block.
            {
                // store target as the child or resource colour: to string found in panel.
                Transform target = panel.Find(colour.ToString());
                // store selected as manager current colour equals colour.
                bool selected = manager.CurrentColour == colour;
                // study ui style: style button using the the button component attached to this object
                // component on target, selected.
                StudyUiStyle.StyleButton(target.GetComponent<Button>(), selected);
                // set the the tmp text component attached to this object component on the child or resource
                // the text "label" found in target text to colour: to string: to upper invariant plus if
                // selected, use the text " / <size=11>active</size>", otherwise use the text "".
                target.Find("Label").GetComponent<TMP_Text>().text = colour.ToString().ToUpperInvariant() + (selected ? "\n<size=11>ACTIVE</size>" : "");
            // finish the for each statement block.
            }
            // visit each tool in system enum: get values using the type information for whiteboard tool.
            foreach (WhiteboardTool tool in System.Enum.GetValues(typeof(WhiteboardTool)))
            // begin the for each statement block.
            {
                // store target as the child or resource tool: to string found in panel.
                Transform target = panel.Find(tool.ToString());
                // store selected as manager current tool equals tool.
                bool selected = manager.CurrentTool == tool;
                // study ui style: style button using the the button component attached to this object
                // component on target, selected.
                StudyUiStyle.StyleButton(target.GetComponent<Button>(), selected);
                // set the the tmp text component attached to this object component on the child or resource
                // the text "label" found in target text to tool: to string: to upper invariant plus if
                // selected, use the text " / <size=12>active</size>", otherwise use the text "".
                target.Find("Label").GetComponent<TMP_Text>().text = tool.ToString().ToUpperInvariant() + (selected ? "\n<size=12>ACTIVE</size>" : "");
            // finish the for each statement block.
            }
        // finish the method refresh selection block.
        }

        // method: time label.
        // format elapsed seconds as minutes and two-digit seconds for the session display.
        private static string TimeLabel(float seconds)
        // begin the method time label block.
        {
            // store total as the larger of 0 and seconds rounded downward to an integer.
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            // return total divided by 60: to string using the text "00" plus the text ":" plus total modulo
            // 60: to string using the text "00" to the caller.
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        // finish the method time label block.
        }

        // method: text.
        // reuse and style an existing named text element rather than creating a duplicate label.
        private static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color colour)
        // begin the method text block.
        {
            // store text as the the tmp text component attached to this object component on the child or
            // resource name found in parent.
            TMP_Text text = parent.Find(name).GetComponent<TMP_Text>();
            // study ui style: set rect using text transform, position, size.
            StudyUiStyle.SetRect(text.transform, position, size);
            // study ui style: style text using text, value, font size, text alignment options left, colour.
            StudyUiStyle.StyleText(text, value, fontSize, TextAlignmentOptions.Left, colour);
            // return text to the caller.
            return text;
        // finish the method text block.
        }

        // method: layout button.
        // apply a consistent rectangle, button style, and label style to an existing control.
        private static void LayoutButton(Transform target, Vector2 position, Vector2 size, string label)
        // begin the method layout button block.
        {
            // study ui style: set rect using target, position, size.
            StudyUiStyle.SetRect(target, position, size);
            // study ui style: style button using the the button component attached to this object component on
            // target.
            StudyUiStyle.StyleButton(target.GetComponent<Button>());
            // store text as the the tmp text component attached to this object component on the child or
            // resource the text "label" found in target.
            TMP_Text text = target.Find("Label").GetComponent<TMP_Text>();
            // study ui style: set rect using text transform, the zero two-dimensional vector, size minus a new
            // two-dimensional vector using 20, 10.
            StudyUiStyle.SetRect(text.transform, Vector2.zero, size - new Vector2(20f, 10f));
            // study ui style: style text using text, label, 21, text alignment options center, study ui style
            // text.
            StudyUiStyle.StyleText(text, label, 21f, TextAlignmentOptions.Center, StudyUiStyle.Text);
        // finish the method layout button block.
        }

        // method: add session icon.
        // add the vector record, stop, or play symbol to a session button and leave room for its label.
        private static void AddSessionIcon(Button button, StudySessionIcon.Kind kind)
        // begin the method add session icon block.
        {
            // store icon as a new game object using the text "stateicon", the type information for rect
            // transform, the type information for canvas renderer.
            GameObject icon = new GameObject("StateIcon", typeof(RectTransform), typeof(CanvasRenderer));
            // parent icon transform under button transform using local coordinates.
            icon.transform.SetParent(button.transform, false);
            // study ui style: set rect using icon transform, a new two-dimensional vector using the negative
            // of 96, 0, a new two-dimensional vector using 16, 16.
            StudyUiStyle.SetRect(icon.transform, new Vector2(-96f, 0f), new Vector2(16f, 16f));
            // store graphic as a newly attached add component<study session icon> component on icon.
            StudySessionIcon graphic = icon.AddComponent<StudySessionIcon>();
            // set graphic colour to study ui style text.
            graphic.color = StudyUiStyle.Text;
            // set graphic raycast target to false.
            graphic.raycastTarget = false;
            // graphic: set kind using kind.
            graphic.SetKind(kind);
            // store text as the requested component in the children of button.
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            // study ui style: set rect using text transform, a new two-dimensional vector using 11, 0, a new
            // two-dimensional vector using 198, 54.
            StudyUiStyle.SetRect(text.transform, new Vector2(11f, 0f), new Vector2(198f, 54f));
        // finish the method add session icon block.
        }
    // finish the class whiteboard control panel block.
    }
// finish the namespace block.
}
