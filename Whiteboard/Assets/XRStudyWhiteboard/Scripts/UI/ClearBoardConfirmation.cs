// script overview: this script controls the confirmation panel shown before clearing the whole whiteboard. it
// connects the cancel and confirm buttons once, shows or hides the panel, and forwards the chosen action to
// the manager. it protects notes from an accidental clear-button press.

// search: clear whiteboard | confirmation | cancel | keep notes.
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
    // declare the clear board confirmation class based on unity behaviour.
    public sealed class ClearBoardConfirmation : MonoBehaviour
    // begin the class clear board confirmation block.
    {
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRStudyWhiteboardManager manager;
        // keep cancel button as button for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Button cancelButton;
        // keep confirm button as button for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Button confirmButton;
        // keep listeners bound as bool for later use.
        private bool listenersBound;

        // expose is visible as game object active self.
        public bool IsVisible => gameObject.activeSelf;

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // call bind listeners: connect cancel and confirm callbacks once, avoiding duplicate actions after
            // repeated initialisation.
            BindListeners();
            // call hide: hide the clear-confirmation panel.
            Hide();
        // finish the method awake block.
        }

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(XRStudyWhiteboardManager whiteboardManager, Button cancel, Button confirm)
        // begin the method initialize block.
        {
            // set manager to whiteboard manager.
            manager = whiteboardManager;
            // set cancel button to cancel.
            cancelButton = cancel;
            // set confirm button to confirm.
            confirmButton = confirm;
            // call bind listeners: connect cancel and confirm callbacks once, avoiding duplicate actions after
            // repeated initialisation.
            BindListeners();
        // finish the method initialize block.
        }

        // method: bind listeners.
        // connect cancel and confirm callbacks once, avoiding duplicate actions after repeated initialisation.
        private void BindListeners()
        // begin the method bind listeners block.
        {
            // run the following statement only when listeners bound.
            if (listenersBound)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when cancel button exists.
            if (cancelButton != null)
                // register cancel as a callback on cancel button on click.
                cancelButton.onClick.AddListener(Cancel);
            // run the following statement only when confirm button exists.
            if (confirmButton != null)
                // register confirm as a callback on confirm button on click.
                confirmButton.onClick.AddListener(Confirm);

            // set listeners bound to cancel button exists or confirm button exists.
            listenersBound = cancelButton != null || confirmButton != null;
        // finish the method bind listeners block.
        }

        // method: show.
        // activate the clear-confirmation panel.
        public void Show()
        // begin the method show block.
        {
            // show and enable game object.
            gameObject.SetActive(true);
        // finish the method show block.
        }

        // method: hide.
        // hide the clear-confirmation panel.
        public void Hide()
        // begin the method hide block.
        {
            // hide and disable game object.
            gameObject.SetActive(false);
        // finish the method hide block.
        }

        // method: cancel.
        // keep the notes and close confirmation through the manager when available.
        private void Cancel()
        // begin the method cancel block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // manager: cancel clear.
                manager.CancelClear();
            // handle the alternative when the preceding condition is false.
            else
                // call hide: hide the clear-confirmation panel.
                Hide();
        // finish the method cancel block.
        }

        // method: confirm.
        // ask the manager to perform the confirmed clear and close the panel.
        private void Confirm()
        // begin the method confirm block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // manager: confirm clear.
                manager.ConfirmClear();
            // handle the alternative when the preceding condition is false.
            else
                // call hide: hide the clear-confirmation panel.
                Hide();
        // finish the method confirm block.
        }
    // finish the class clear board confirmation block.
    }
// finish the namespace block.
}
