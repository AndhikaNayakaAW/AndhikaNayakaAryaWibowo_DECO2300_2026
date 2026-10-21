// script overview: this script keeps the main whiteboard tool and colour in one shared place. buttons change
// this state, and drawing reads the same state so the interface and ink agree. it also manages clear-board
// confirmation, connects the session recorder, and prevents live tool changes during playback. paper has its
// own tool selection.

// search: colour | marker | eraser | clear whiteboard | shared state.
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
    // define the named whiteboard tool choices used instead of unexplained numbers.
    public enum WhiteboardTool
    // begin the enum whiteboard tool block.
    {
        // add marker as one of the available choices.
        Marker,
        // add eraser as one of the available choices.
        Eraser
    // finish the enum whiteboard tool block.
    }

    // define the named whiteboard colour choices used instead of unexplained numbers.
    public enum WhiteboardColour
    // begin the enum whiteboard colour block.
    {
        // add black as one of the available choices.
        Black,
        // add red as one of the available choices.
        Red,
        // add blue as one of the available choices.
        Blue,
        // add green as one of the available choices.
        Green
    // finish the enum whiteboard colour block.
    }

    // keeps the whiteboard state in one place so controllers, hands, and ui
    // all use the same tool and colour selection.
    // declare the xr study whiteboard manager class based on unity behaviour.
    public sealed class XRStudyWhiteboardManager : MonoBehaviour
    // begin the class xr study whiteboard manager block.
    {
        // canvas: the main board pixel surface that receives drawing operations. keep canvas as whiteboard
        // canvas for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardCanvas canvas;
        // keep status display as whiteboard status display for later use. allow unity to save and configure
        // this field in the inspector.
        [SerializeField] private WhiteboardStatusDisplay statusDisplay;
        // keep clear confirmation as clear board confirmation for later use. allow unity to save and configure
        // this field in the inspector.
        [SerializeField] private ClearBoardConfirmation clearConfirmation;
        // starting tool: the initial marker or eraser choice. keep starting tool as whiteboard tool, initially
        // whiteboard tool marker. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardTool startingTool = WhiteboardTool.Marker;
        // starting colour: the initial named marker colour. keep starting colour as whiteboard colour,
        // initially whiteboard colour black. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardColour startingColour = WhiteboardColour.Black;

        // all input paths read the same selected whiteboard tool and colour.
        // expose the current tool property for reading while keeping writes inside this class.
        public WhiteboardTool CurrentTool { get; private set; }
        // expose the current colour property for reading while keeping writes inside this class.
        public WhiteboardColour CurrentColour { get; private set; }
        // expose is clear confirmation visible as clear confirmation exists and clear confirmation is visible.
        public bool IsClearConfirmationVisible => clearConfirmation != null && clearConfirmation.IsVisible;

        // expose the recorder property for reading while keeping writes inside this class.
        public WhiteboardSessionRecorder Recorder { get; private set; }
        // expose is playback active as recorder exists and recorder is playing.
        public bool IsPlaybackActive => Recorder != null && Recorder.IsPlaying;

        // declare state changed so other components can subscribe to notifications.
        public event Action StateChanged;

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // set current tool to starting tool.
            CurrentTool = startingTool;
            // colour: begin with the configured starting marker colour.
            CurrentColour = startingColour;

            // run the following statement only when canvas exists.
            if (canvas != null)
                // canvas: set manager using this.
                canvas.SetManager(this);

            // call ensure recorder: find or add the runtime session recorder and connect it to this manager
            // and canvas.
            EnsureRecorder();
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
        // finish the method awake block.
        }

        // method: set tool.
        // select marker or eraser unless playback is active. end the old stroke before notifying the
        // interface.
        public void SetTool(WhiteboardTool tool)
        // begin the method set tool block.
        {
            // run the following statement only when is playback active or current tool equals tool.
            if (IsPlaybackActive || CurrentTool == tool)
                // leave this method now without running the remaining statements.
                return;

            // if canvas exists, end the current stroke so later hits do not join it.
            canvas?.EndStroke();
            // store marker or eraser as the active tool that the whiteboard canvas will use.
            CurrentTool = tool;
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
        // finish the method set tool block.
        }

        // method: set colour.
        // select a marker colour unless playback is active. end the old stroke, refresh shared feedback, and
        // request a short vibration.
        public void SetColour(WhiteboardColour colour)
        // begin the method set colour block.
        {
            // run the following statement only when is playback active or current colour equals colour.
            if (IsPlaybackActive || CurrentColour == colour)
                // leave this method now without running the remaining statements.
                return;

            // if canvas exists, end the current stroke so later hits do not join it.
            canvas?.EndStroke();
            // colour: store the requested named colour as the shared active whiteboard selection.
            CurrentColour = colour;
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
        // finish the method set colour block.
        }

        // method: get current ink colour.
        // convert the current named colour selection into the actual colour used to paint pixels.
        public Color GetCurrentInkColour()
        // begin the method get current ink colour block.
        {
            // colour: return the actual ink colour represented by the current shared selection.
            return GetColour(CurrentColour);
        // finish the method get current ink colour block.
        }

        // method: get colour.
        // map black, red, blue, and green choices to explicit colour values. unmatched choices use the dark
        // default.
        public static Color GetColour(WhiteboardColour colour)
        // begin the method get colour block.
        {
            // choose a branch using colour.
            switch (colour)
            // begin the switch statement block.
            {
                // handle the whiteboard colour red choice.
                case WhiteboardColour.Red:
                    // return a new colour using 0.88, 0.08, 0.08 to the caller.
                    return new Color(0.88f, 0.08f, 0.08f);
                // handle the whiteboard colour blue choice.
                case WhiteboardColour.Blue:
                    // return a new colour using 0.05, 0.25, 0.92 to the caller.
                    return new Color(0.05f, 0.25f, 0.92f);
                // handle the whiteboard colour green choice.
                case WhiteboardColour.Green:
                    // return a new colour using 0.05, 0.58, 0.18 to the caller.
                    return new Color(0.05f, 0.58f, 0.18f);
                // handle choices not matched by an earlier case.
                default:
                    // return a new colour using 0.025, 0.03, 0.04 to the caller.
                    return new Color(0.025f, 0.03f, 0.04f);
            // finish the switch statement block.
            }
        // finish the method get colour block.
        }

        // method: request clear.
        // end drawing and open the clear-board confirmation so a single accidental click does not delete
        // notes.
        public void RequestClear()
        // begin the method request clear block.
        {
            // run the following statement only when is playback active.
            if (IsPlaybackActive)
                // leave this method now without running the remaining statements.
                return;
            // if canvas exists, end the current stroke so later hits do not join it.
            canvas?.EndStroke();
            // ask for confirmation before deleting all whiteboard notes.
            // run the following statement only when clear confirmation exists.
            if (clearConfirmation != null)
            // begin the if statement block.
            {
                // clear confirmation: show.
                clearConfirmation.Show();
                // request a short right-controller vibration if the device supports it.
                ControllerHaptics.PulseRightController();
            // finish the if statement block.
            }
        // finish the method request clear block.
        }

        // method: confirm clear.
        // clear the board after confirmation, close the panel, and refresh the interface.
        public void ConfirmClear()
        // begin the method confirm clear block.
        {
            // run the following statement only when is playback active.
            if (IsPlaybackActive)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when canvas exists.
            if (canvas != null)
                // canvas: clear board.
                canvas.ClearBoard();

            // run the following statement only when clear confirmation exists.
            if (clearConfirmation != null)
                // clear confirmation: hide.
                clearConfirmation.Hide();

            // request a short right-controller vibration if the device supports it.
            ControllerHaptics.PulseRightController();
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
        // finish the method confirm clear block.
        }

        // method: cancel clear.
        // close the confirmation without clearing the board.
        public void CancelClear()
        // begin the method cancel clear block.
        {
            // run the following statement only when clear confirmation exists.
            if (clearConfirmation != null)
                // clear confirmation: hide.
                clearConfirmation.Hide();
        // finish the method cancel clear block.
        }

        // method: set references.
        // connect the existing scene objects used by this component rather than creating another copy of those
        // systems.
        public void SetReferences(WhiteboardCanvas boardCanvas, WhiteboardStatusDisplay display, ClearBoardConfirmation confirmation)
        // begin the method set references block.
        {
            // set canvas to board canvas.
            canvas = boardCanvas;
            // set status display to display.
            statusDisplay = display;
            // set clear confirmation to confirmation.
            clearConfirmation = confirmation;

            // run the following statement only when canvas exists.
            if (canvas != null)
                // canvas: set manager using this.
                canvas.SetManager(this);

            // call ensure recorder: find or add the runtime session recorder and connect it to this manager
            // and canvas.
            EnsureRecorder();
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
        // finish the method set references block.
        }

        // method: ensure recorder.
        // find or add the runtime session recorder and connect it to this manager and canvas.
        private void EnsureRecorder()
        // begin the method ensure recorder block.
        {
            // run the following statement only when not (whether the application is running in play mode).
            if (!Application.isPlaying)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when recorder is missing.
            if (Recorder == null)
                // set recorder to the whiteboard session recorder component attached to this object.
                Recorder = GetComponent<WhiteboardSessionRecorder>();
            // run the following statement only when recorder is missing.
            if (Recorder == null)
                // set recorder to a newly attached add component<whiteboard session recorder> component on
                // game object.
                Recorder = gameObject.AddComponent<WhiteboardSessionRecorder>();
            // recorder: configure using this, canvas.
            Recorder.Configure(this, canvas);
        // finish the method ensure recorder block.
        }

        // method: apply playback selection.
        // apply a recorded tool and colour directly so replay uses the original selection.
        internal void ApplyPlaybackSelection(WhiteboardTool tool, WhiteboardColour colour)
        // begin the method apply playback selection block.
        {
            // store marker or eraser as the active tool that the whiteboard canvas will use.
            CurrentTool = tool;
            // colour: store the requested named colour as the shared active whiteboard selection.
            CurrentColour = colour;
            // call refresh state: update the status display and notify every subscriber that the shared state
            // should be refreshed.
            RefreshState();
        // finish the method apply playback selection block.
        }

        // method: refresh state.
        // update the status display and notify every subscriber that the shared state should be refreshed.
        private void RefreshState()
        // begin the method refresh state block.
        {
            // run the following statement only when status display exists.
            if (statusDisplay != null)
                // status display: refresh using this.
                statusDisplay.Refresh(this);

            // if state changed exists, schedule after seconds.
            StateChanged?.Invoke();
        // finish the method refresh state block.
        }
    // finish the class xr study whiteboard manager block.
    }
// finish the namespace block.
}
