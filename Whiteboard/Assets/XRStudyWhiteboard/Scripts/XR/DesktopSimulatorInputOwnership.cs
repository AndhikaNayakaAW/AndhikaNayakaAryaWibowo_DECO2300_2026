// script overview: this script separates desktop room movement from simulated controller movement. it
// temporarily replaces simulator actions so the keyboard does not move the rig and a controller at the same
// time. explicit controller mode gives the selected simulated hand direct mouse input. disposing this helper
// restores the original actions and releases the runtime input objects. it does not change physical headset
// tracking.

// search: desktop simulator | t key | y key | controller movement | input ownership.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system available to this script.
using System;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make keyboard, mouse, and input-action types available for controller and desktop input.
using UnityEngine.InputSystem;
// make the types in unity engine.xr.interaction.toolkit.inputs.simulation available to this script.
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // keeps desktop rig navigation separate from explicit virtual-controller posing.
    // declare the desktop simulator input ownership class based on i disposable.
    internal sealed class DesktopSimulatorInputOwnership : IDisposable
    // begin the class desktop simulator input ownership block.
    {
        // keep simulator as xr device simulator for later use.
        private readonly XRDeviceSimulator simulator;
        // keep original as input action reference[] for later use.
        private readonly InputActionReference[] original;
        // keep actions as input action asset for later use.
        private readonly InputActionAsset actions;
        // keep free delta as input action reference for later use.
        private readonly InputActionReference freeDelta;
        // keep free scroll as input action reference for later use.
        private readonly InputActionReference freeScroll;
        // keep free trigger as input action reference for later use.
        private readonly InputActionReference freeTrigger;
        // keep delta as input action reference for later use.
        private readonly InputActionReference delta;
        // keep scroll as input action reference for later use.
        private readonly InputActionReference scroll;
        // keep trigger as input action reference for later use.
        private readonly InputActionReference trigger;
        // keep stationary axis as input action reference for later use.
        private readonly InputActionReference stationaryAxis;
        // keep stationary vector as input action reference for later use.
        private readonly InputActionReference stationaryVector;

        // method: desktop simulator input ownership.
        // remember the simulator original actions, create temporary gated actions, and give room movement and
        // controller posing separate owners.
        public DesktopSimulatorInputOwnership(XRDeviceSimulator target)
        // begin the constructor desktop simulator input ownership block.
        {
            // set simulator to target.
            simulator = target;
            // set original to a new array containing the listed entries.
            original = new[] { target.keyboardXTranslateAction, target.keyboardYTranslateAction,
                // target keyboard z translate action.
                target.keyboardZTranslateAction, target.axis2DAction, target.restingHandAxis2DAction,
                // target mouse delta action.
                target.mouseDeltaAction, target.mouseScrollAction, target.triggerAction };
            // set actions to scriptable object: create instance.
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            // set actions name to the text "desktop controller posing (runtime)".
            actions.name = "Desktop controller posing (runtime)";
            // store map as a new input action map using the text "explicit controller posing".
            var map = new InputActionMap("Explicit controller posing");
            // actions: add action map using map.
            actions.AddActionMap(map);
            // set stationary axis to input action reference: create using map: add action using the text "rig
            // owns keyboard translation", input action type value, the text "scale(factor=0)".
            stationaryAxis = InputActionReference.Create(map.AddAction("Rig owns keyboard translation", InputActionType.Value, processors: "scale(factor=0)"));
            // stationary axis action: add composite binding using the text "1daxis": with using the text
            // "negative", the text "<keyboard>/q": with using the text "positive", the text "<keyboard>/e".
            stationaryAxis.action.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            // set stationary vector to input action reference: create using map: add action using the text
            // "rig owns thumbstick movement", input action type value, the text "scaletwo-dimensional
            // vector(x=0,y=0)".
            stationaryVector = InputActionReference.Create(map.AddAction("Rig owns thumbstick movement", InputActionType.Value, processors: "scaleVector2(x=0,y=0)"));
            // set stationary vector action expected control type to the text "two-dimensional vector".
            stationaryVector.action.expectedControlType = "Vector2";
            // the official simulator ui displays four key labels. keep valid
            // controls while consuming their movement value in desktop mode.
            // stationary vector action: add composite binding using the text "2dvector": with using the text
            // "up", the text "<keyboard>/w": with using the text "down", the text "<keyboard>/s": with using
            // the text "left", the text "<keyboard>/a": with using the text "right", the text "<keyboard>/d".
            stationaryVector.action.AddCompositeBinding("2DVector")
                // stationary vector action: add composite binding using the text "2dvector" with.
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                // stationary vector action: add composite binding using the text "2dvector": with using the
                // text "up", the text "<keyboard>/w": with using the text "down", the text "<keyboard>/s"
                // with.
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            // set delta to gated mouse using map, the text "pose", the text "<mouse>/delta".
            delta = GatedMouse(map, "Pose", "<Mouse>/delta");
            // set scroll to gated mouse using map, the text "depth", the text "<mouse>/scroll".
            scroll = GatedMouse(map, "Depth", "<Mouse>/scroll");
            // set trigger to gated mouse using map, the text "trigger", the text "<mouse>/leftbutton", the
            // text "button".
            trigger = GatedMouse(map, "Trigger", "<Mouse>/leftButton", "Button");
            // set free delta to input action reference: create using map: add action using the text "selected
            // controller aim", input action type value, the text "<mouse>/delta".
            freeDelta = InputActionReference.Create(map.AddAction("Selected controller aim", InputActionType.Value, "<Mouse>/delta"));
            // set free scroll to input action reference: create using map: add action using the text "selected
            // controller depth", input action type value, the text "<mouse>/scroll".
            freeScroll = InputActionReference.Create(map.AddAction("Selected controller depth", InputActionType.Value, "<Mouse>/scroll"));
            // set free delta action expected control type to set free scroll action expected control type to
            // the text "two-dimensional vector".
            freeDelta.action.expectedControlType = freeScroll.action.expectedControlType = "Vector2";
            // set free trigger to input action reference: create using map: add action using the text
            // "selected controller trigger", input action type button, the text "<mouse>/leftbutton".
            freeTrigger = InputActionReference.Create(map.AddAction("Selected controller trigger", InputActionType.Button, "<Mouse>/leftButton"));
            // set target keyboard x translate action to stationary axis.
            target.keyboardXTranslateAction = stationaryAxis;
            // set target keyboard y translate action to stationary axis.
            target.keyboardYTranslateAction = stationaryAxis;
            // set target keyboard z translate action to stationary axis.
            target.keyboardZTranslateAction = stationaryAxis;
            // set target axis2 d action to stationary vector.
            target.axis2DAction = stationaryVector;
            // set target resting hand axis2 d action to stationary vector.
            target.restingHandAxis2DAction = stationaryVector;
            // set target mouse delta action to delta.
            target.mouseDeltaAction = delta;
            // set target mouse scroll action to scroll.
            target.mouseScrollAction = scroll;
            // set target trigger action to trigger.
            target.triggerAction = trigger;
            // enable actions so it can receive input.
            actions.Enable();
        // finish the constructor desktop simulator input ownership block.
        }

        // method: set explicit controller mode.
        // switch between modifier-gated mouse posing and direct mouse control for the selected simulated hand.
        public void SetExplicitControllerMode(bool enabled)
        // begin the method set explicit controller mode block.
        {
            // set simulator mouse delta action to if enabled, use free delta, otherwise use delta.
            simulator.mouseDeltaAction = enabled ? freeDelta : delta;
            // set simulator mouse scroll action to if enabled, use free scroll, otherwise use scroll.
            simulator.mouseScrollAction = enabled ? freeScroll : scroll;
            // set simulator trigger action to if enabled, use free trigger, otherwise use trigger.
            simulator.triggerAction = enabled ? freeTrigger : trigger;
        // finish the method set explicit controller mode block.
        }

        // method: gated mouse.
        // create a mouse action that becomes active only while a controller-pose modifier is held.
        private static InputActionReference GatedMouse(InputActionMap map, string name, string binding, string controlType = "Vector2")
        // begin the method gated mouse block.
        {
            // store action as map: add action using name, input action type value.
            var action = map.AddAction(name, InputActionType.Value);
            // set action expected control type to control type.
            action.expectedControlType = controlType;
            // visit each modifier in a new array containing the listed entries.
            foreach (string modifier in new[] { "<Keyboard>/space", "<Keyboard>/leftShift" })
                // action: add composite binding using the text "onemodifier": with using the text "modifier",
                // modifier: with using the text "binding", binding.
                action.AddCompositeBinding("OneModifier").With("Modifier", modifier).With("Binding", binding);
            // return input action reference: create using action to the caller.
            return InputActionReference.Create(action);
        // finish the method gated mouse block.
        }

        // method: dispose.
        // restore every original simulator action, disable the temporary action asset, and destroy the runtime
        // references.
        public void Dispose()
        // begin the method dispose block.
        {
            // run the following statement only when simulator exists.
            if (simulator != null)
            // begin the if statement block.
            {
                // set simulator keyboard x translate action to the entry at 0 in original.
                simulator.keyboardXTranslateAction = original[0];
                // set simulator keyboard y translate action to the entry at 1 in original.
                simulator.keyboardYTranslateAction = original[1];
                // set simulator keyboard z translate action to the entry at 2 in original.
                simulator.keyboardZTranslateAction = original[2];
                // set simulator axis2 d action to the entry at 3 in original.
                simulator.axis2DAction = original[3];
                // set simulator resting hand axis2 d action to the entry at 4 in original.
                simulator.restingHandAxis2DAction = original[4];
                // set simulator mouse delta action to the entry at 5 in original.
                simulator.mouseDeltaAction = original[5];
                // set simulator mouse scroll action to the entry at 6 in original.
                simulator.mouseScrollAction = original[6];
                // set simulator trigger action to the entry at 7 in original.
                simulator.triggerAction = original[7];
            // finish the if statement block.
            }
            // disable actions.
            actions.Disable();
            // destroy owned using free delta.
            DestroyOwned(freeDelta); DestroyOwned(freeScroll); DestroyOwned(freeTrigger);
            // destroy owned using delta.
            DestroyOwned(delta); DestroyOwned(scroll); DestroyOwned(trigger); DestroyOwned(stationaryAxis); DestroyOwned(stationaryVector); DestroyOwned(actions);
        // finish the method dispose block.
        }

        // method: destroy owned.
        // release an object created by this drawing system. use delayed destruction in play mode and immediate
        // destruction in editor mode.
        private static void DestroyOwned(UnityEngine.Object value)
        // begin the method destroy owned block.
        {
            // run the following statement only when whether the application is running in play mode.
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            // handle the alternative when the preceding condition is false.
            else UnityEngine.Object.DestroyImmediate(value);
        // finish the method destroy owned block.
        }
    // finish the class desktop simulator input ownership block.
    }
// finish the namespace block.
}
