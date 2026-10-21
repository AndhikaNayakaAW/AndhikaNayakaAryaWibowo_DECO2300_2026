// script overview: this script configures the event system for the xr interface. it connects the available
// input actions to the xr ui input module so tracked controllers and supported pointer inputs can interact
// with interface buttons. this is interface input setup, separate from painting the whiteboard texture.

// search: controller ui | input actions | ray pointer | buttons.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make keyboard, mouse, and input-action types available for controller and desktop input.
using UnityEngine.InputSystem;
// make the types in unity engine.xr.interaction.toolkit.ui available to this script.
using UnityEngine.XR.Interaction.Toolkit.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // connects the project's existing official xri action asset to the world
    // space ui module at runtime. this keeps ui selection working even when
    // the scene is rebuilt by the editor tool.
    // declare the xrui input setup class based on unity behaviour.
    public sealed class XRUIInputSetup : MonoBehaviour
    // begin the class xrui input setup block.
    {
        // keep actions as input action asset for later use. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private InputActionAsset actions;
        // keep input module as xrui input module for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRUIInputModule inputModule;

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // connect xr interaction toolkit actions to the world-space interface.
            // run the following statement only when input module is missing.
            if (inputModule == null)
                // set input module to the xrui input module component attached to this object.
                inputModule = GetComponent<XRUIInputModule>();
            // run the following statement only when actions is missing or input module is missing.
            if (actions == null || inputModule == null)
                // leave this method now without running the remaining statements.
                return;

            // set input module enable xr input to true.
            inputModule.enableXRInput = true;
            // editordesktopinteractor dispatches the mouse to world ui once.
            // keep xr rays on the official input module without a second click.
            // set input module enable mouse input to false.
            inputModule.enableMouseInput = false;
            // set input module enable builtin actions as fallback to true.
            inputModule.enableBuiltinActionsAsFallback = true;
            // set input module point action to reference using the text "xri ui/point".
            inputModule.pointAction = Reference("XRI UI/Point");
            // set input module left click action to reference using the text "xri ui/click".
            inputModule.leftClickAction = Reference("XRI UI/Click");
            // set input module middle click action to reference using the text "xri ui/middleclick".
            inputModule.middleClickAction = Reference("XRI UI/MiddleClick");
            // set input module right click action to reference using the text "xri ui/rightclick".
            inputModule.rightClickAction = Reference("XRI UI/RightClick");
            // set input module scroll wheel action to reference using the text "xri ui/scrollwheel".
            inputModule.scrollWheelAction = Reference("XRI UI/ScrollWheel");
            // set input module navigate action to reference using the text "xri ui/navigate".
            inputModule.navigateAction = Reference("XRI UI/Navigate");
            // set input module submit action to reference using the text "xri ui/submit".
            inputModule.submitAction = Reference("XRI UI/Submit");
            // set input module cancel action to reference using the text "xri ui/cancel".
            inputModule.cancelAction = Reference("XRI UI/Cancel");
        // finish the method awake block.
        }

        // method: initialize.
        // configure the xr ui input module with the action references available in the supplied asset.
        public void Initialize(InputActionAsset inputActions, XRUIInputModule module)
        // begin the method initialize block.
        {
            // set actions to input actions.
            actions = inputActions;
            // set input module to module.
            inputModule = module;
        // finish the method initialize block.
        }

        // method: reference.
        // look up a named input action and wrap it as a reference when it exists.
        private InputActionReference Reference(string actionName)
        // begin the method reference block.
        {
            // store action as actions: find action using action name, false.
            InputAction action = actions.FindAction(actionName, false);
            // return if action is missing, use no object, otherwise use input action reference: create using
            // action to the caller.
            return action == null ? null : InputActionReference.Create(action);
        // finish the method reference block.
        }
    // finish the class xrui input setup block.
    }
// finish the namespace block.
}
