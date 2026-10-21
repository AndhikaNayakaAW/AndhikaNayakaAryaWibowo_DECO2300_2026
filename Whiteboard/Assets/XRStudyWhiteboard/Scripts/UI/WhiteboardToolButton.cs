// script overview: this script connects a marker or eraser button to the shared whiteboard manager. clicking
// requests the tool, and manager notifications update the selection outline. the canvas reads that same tool
// state when applying ink or erasing.

// search: marker | eraser | whiteboard tool | selection outline.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // declare the whiteboard tool button class based on unity behaviour.
    public sealed class WhiteboardToolButton : MonoBehaviour
    // begin the class whiteboard tool button block.
    {
        // this button selects the whiteboard marker or eraser.
        // keep tool as whiteboard tool for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private WhiteboardTool tool;
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRStudyWhiteboardManager manager;
        // keep selection outline as outline for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Outline selectionOutline;

        // keep button as button for later use.
        private Button button;

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // set button to the button component attached to this object.
            button = GetComponent<Button>();
            // run the following statement only when button exists.
            if (button != null)
                // register select tool as a callback on button on click.
                button.onClick.AddListener(SelectTool);

            // run the following statement only when manager exists.
            if (manager != null)
                // subscribe refresh to manager state changed so it runs when the event is raised.
                manager.StateChanged += Refresh;

            // call refresh: show the selection outline only when this button tool matches the manager.
            Refresh();
        // finish the method awake block.
        }

        // method: on destroy.
        // release owned resources or event listeners when unity destroys this component.
        private void OnDestroy()
        // begin the method on destroy block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // unsubscribe refresh from manager state changed so this object no longer receives the event.
                manager.StateChanged -= Refresh;
        // finish the method on destroy block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(XRStudyWhiteboardManager whiteboardManager, WhiteboardTool targetTool, Outline outline)
        // begin the method initialize block.
        {
            // set manager to whiteboard manager.
            manager = whiteboardManager;
            // set tool to target tool.
            tool = targetTool;
            // set selection outline to outline.
            selectionOutline = outline;
        // finish the method initialize block.
        }

        // method: select tool.
        // request this button marker or eraser choice through the manager.
        private void SelectTool()
        // begin the method select tool block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // manager: set tool using tool.
                manager.SetTool(tool);
        // finish the method select tool block.
        }

        // method: refresh.
        // show the selection outline only when this button tool matches the manager.
        private void Refresh()
        // begin the method refresh block.
        {
            // run the following statement only when selection outline exists.
            if (selectionOutline != null)
                // set whether selection outline is enabled to manager exists and manager current tool equals
                // tool.
                selectionOutline.enabled = manager != null && manager.CurrentTool == tool;
        // finish the method refresh block.
        }
    // finish the class whiteboard tool button block.
    }
// finish the namespace block.
}
