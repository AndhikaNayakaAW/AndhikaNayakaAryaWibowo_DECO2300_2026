// script overview: this script updates the visible whiteboard status labels from the shared manager. it
// supports the original separate labels and the compact selected-tool label. it reports state to the user
// without maintaining a second independent tool or colour selection.

// search: whiteboard status | colour | tool | input feedback.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make text mesh pro types available for readable interface labels.
using TMPro;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // declare the whiteboard status display class based on unity behaviour.
    public sealed class WhiteboardStatusDisplay : MonoBehaviour
    // begin the class whiteboard status display block.
    {
        // this panel displays the current tool, colour, input, and drawing status.
        // keep tool text as tmp text for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private TMP_Text toolText;
        // keep colour text as tmp text for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private TMP_Text colourText;
        // keep input text as tmp text for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private TMP_Text inputText;
        // keep drawing text as tmp text for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private TMP_Text drawingText;
        // keep compact layout as bool for later use.
        private bool compactLayout;

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(TMP_Text toolLabel, TMP_Text colourLabel, TMP_Text inputLabel, TMP_Text drawingLabel)
        // begin the method initialize block.
        {
            // set tool text to tool label.
            toolText = toolLabel;
            // set colour text to colour label.
            colourText = colourLabel;
            // set input text to input label.
            inputText = inputLabel;
            // set drawing text to drawing label.
            drawingText = drawingLabel;
        // finish the method initialize block.
        }

        // method: initialize compact.
        // connect the compact label used by the redesigned board panel.
        public void InitializeCompact(TMP_Text currentLabel)
        // begin the method initialize compact block.
        {
            // set compact layout to true.
            compactLayout = true;
            // set tool text to current label.
            toolText = currentLabel;
            // set colour text to set input text to set drawing text to no object.
            colourText = inputText = drawingText = null;
        // finish the method initialize compact block.
        }

        // method: refresh.
        // read the current tool and colour from the manager and update the available status labels.
        public void Refresh(XRStudyWhiteboardManager manager)
        // begin the method refresh block.
        {
            // run the following statement only when manager is missing.
            if (manager == null)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when compact layout.
            if (compactLayout)
            // begin the if statement block.
            {
                // run the following statement only when tool text exists.
                if (toolText != null)
                    // set tool text text to the text "current " plus manager current tool plus the text " / "
                    // plus manager current colour.
                    toolText.text = "CURRENT  " + manager.CurrentTool + " / " + manager.CurrentColour;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when tool text exists.
            if (toolText != null)
                // set tool text text to the text "current tool / <color=#9be7ff>" plus manager current tool:
                // to string: to upper invariant plus the text "</color>".
                toolText.text = "CURRENT TOOL\n<color=#9BE7FF>" + manager.CurrentTool.ToString().ToUpperInvariant() + "</color>";
            // run the following statement only when colour text exists.
            if (colourText != null)
                // set colour text text to the text "colour / <color=#ffffff>" plus manager current colour: to
                // string: to upper invariant plus the text "</color>".
                colourText.text = "COLOUR\n<color=#FFFFFF>" + manager.CurrentColour.ToString().ToUpperInvariant() + "</color>";
            // run the following statement only when input text exists.
            if (inputText != null)
                // set input text text to the text "input / controller / hands".
                inputText.text = "INPUT\nCONTROLLER / HANDS";
        // finish the method refresh block.
        }

        // method: set drawing state.
        // update the input feedback label to describe whether drawing input is active.
        public void SetDrawingState(bool drawing)
        // begin the method set drawing state block.
        {
            // run the following statement only when drawing text exists.
            if (drawingText != null)
                // set drawing text text to the text "drawing / " plus if drawing, use the text "active",
                // otherwise use the text "ready".
                drawingText.text = "DRAWING\n" + (drawing ? "ACTIVE" : "READY");
        // finish the method set drawing state block.
        }
    // finish the class whiteboard status display block.
    }
// finish the namespace block.
}
