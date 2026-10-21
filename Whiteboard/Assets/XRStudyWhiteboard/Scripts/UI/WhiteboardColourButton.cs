// script overview: this script makes one interface button choose one whiteboard colour. it asks the shared
// manager to change colour and refreshes the swatch and selection outline when the manager state changes. it
// does not paint pixels itself.

// search: colour | black | red | blue | green | selection.
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
    // declare the whiteboard colour button class based on unity behaviour.
    public sealed class WhiteboardColourButton : MonoBehaviour
    // begin the class whiteboard colour button block.
    {
        // this button selects a marker colour and updates its active indicator.
        // keep colour as whiteboard colour for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private WhiteboardColour colour;
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRStudyWhiteboardManager manager;
        // keep swatch as image for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Image swatch;
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
                // register select colour as a callback on button on click.
                button.onClick.AddListener(SelectColour);

            // run the following statement only when manager exists.
            if (manager != null)
                // subscribe refresh to manager state changed so it runs when the event is raised.
                manager.StateChanged += Refresh;

            // call refresh: update the colour swatch and selection outline to match the manager.
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
        public void Initialize(XRStudyWhiteboardManager whiteboardManager, WhiteboardColour targetColour, Image targetSwatch, Outline outline)
        // begin the method initialize block.
        {
            // set manager to whiteboard manager.
            manager = whiteboardManager;
            // colour: associate this button with the colour supplied during scene setup.
            colour = targetColour;
            // set swatch to target swatch.
            swatch = targetSwatch;
            // set selection outline to outline.
            selectionOutline = outline;
        // finish the method initialize block.
        }

        // method: select colour.
        // request this button colour through the manager rather than changing the texture directly.
        private void SelectColour()
        // begin the method select colour block.
        {
            // run the following statement only when manager exists.
            if (manager != null)
                // colour: ask the whiteboard manager to select this button colour. the canvas will read that
                // shared choice when it paints.
                manager.SetColour(colour);
        // finish the method select colour block.
        }

        // method: refresh.
        // update the colour swatch and selection outline to match the manager.
        private void Refresh()
        // begin the method refresh block.
        {
            // run the following statement only when swatch exists.
            if (swatch != null)
                // colour: paint this button swatch with the actual colour value mapped from its named choice.
                swatch.color = XRStudyWhiteboardManager.GetColour(colour);
            // run the following statement only when selection outline exists.
            if (selectionOutline != null)
                // set whether selection outline is enabled to manager exists and manager current colour equals
                // colour.
                selectionOutline.enabled = manager != null && manager.CurrentColour == colour;
        // finish the method refresh block.
        }
    // finish the class whiteboard colour button block.
    }
// finish the namespace block.
}
