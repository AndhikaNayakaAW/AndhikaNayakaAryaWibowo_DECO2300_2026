// script overview: this script connects the clear-board button to the manager. a click requests confirmation
// rather than immediately deleting the board. the confirmation panel and manager handle the final decision.

// search: clear whiteboard | button | confirmation.
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
    // declare the whiteboard clear button class based on unity behaviour.
    public sealed class WhiteboardClearButton : MonoBehaviour
    // begin the class whiteboard clear button block.
    {
        // this button requests confirmation before clearing the whiteboard.
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRStudyWhiteboardManager manager;
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
                // register request clear as a callback on button on click.
                button.onClick.AddListener(RequestClear);
        // finish the method awake block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(XRStudyWhiteboardManager whiteboardManager)
        // begin the method initialize block.
        {
            // set manager to whiteboard manager.
            manager = whiteboardManager;
        // finish the method initialize block.
        }

        // method: request clear.
        // ask the manager to open the clear confirmation instead of deleting notes immediately.
        private void RequestClear()
        // begin the method request clear block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // manager: request clear.
                manager.RequestClear();
        // finish the method request clear block.
        }
    // finish the class whiteboard clear button block.
    }
// finish the namespace block.
}
