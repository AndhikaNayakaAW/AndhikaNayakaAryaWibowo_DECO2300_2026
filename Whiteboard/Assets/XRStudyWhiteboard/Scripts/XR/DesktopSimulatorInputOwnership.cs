using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace XRStudyWhiteboard
{
    /// <summary>Keeps desktop rig navigation separate from explicit virtual-controller posing.</summary>
    internal sealed class DesktopSimulatorInputOwnership : IDisposable
    {
        private readonly XRDeviceSimulator simulator;
        private readonly InputActionReference[] original;
        private readonly InputActionAsset actions;
        private readonly InputActionReference freeDelta;
        private readonly InputActionReference freeScroll;
        private readonly InputActionReference freeTrigger;
        private readonly InputActionReference delta;
        private readonly InputActionReference scroll;
        private readonly InputActionReference trigger;
        private readonly InputActionReference stationaryAxis;
        private readonly InputActionReference stationaryVector;

        public DesktopSimulatorInputOwnership(XRDeviceSimulator target)
        {
            simulator = target;
            original = new[] { target.keyboardXTranslateAction, target.keyboardYTranslateAction,
                target.keyboardZTranslateAction, target.axis2DAction, target.restingHandAxis2DAction,
                target.mouseDeltaAction, target.mouseScrollAction, target.triggerAction };
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.name = "Desktop controller posing (runtime)";
            var map = new InputActionMap("Explicit controller posing");
            actions.AddActionMap(map);
            stationaryAxis = InputActionReference.Create(map.AddAction("Rig owns keyboard translation", InputActionType.Value, processors: "scale(factor=0)"));
            stationaryAxis.action.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            stationaryVector = InputActionReference.Create(map.AddAction("Rig owns thumbstick movement", InputActionType.Value, processors: "scaleVector2(x=0,y=0)"));
            stationaryVector.action.expectedControlType = "Vector2";
            // The official simulator UI displays four key labels. Keep valid
            // controls while consuming their movement value in desktop mode.
            stationaryVector.action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            delta = GatedMouse(map, "Pose", "<Mouse>/delta");
            scroll = GatedMouse(map, "Depth", "<Mouse>/scroll");
            trigger = GatedMouse(map, "Trigger", "<Mouse>/leftButton", "Button");
            freeDelta = InputActionReference.Create(map.AddAction("Selected controller aim", InputActionType.Value, "<Mouse>/delta"));
            freeScroll = InputActionReference.Create(map.AddAction("Selected controller depth", InputActionType.Value, "<Mouse>/scroll"));
            freeDelta.action.expectedControlType = freeScroll.action.expectedControlType = "Vector2";
            freeTrigger = InputActionReference.Create(map.AddAction("Selected controller trigger", InputActionType.Button, "<Mouse>/leftButton"));
            target.keyboardXTranslateAction = stationaryAxis;
            target.keyboardYTranslateAction = stationaryAxis;
            target.keyboardZTranslateAction = stationaryAxis;
            target.axis2DAction = stationaryVector;
            target.restingHandAxis2DAction = stationaryVector;
            target.mouseDeltaAction = delta;
            target.mouseScrollAction = scroll;
            target.triggerAction = trigger;
            actions.Enable();
        }

        public void SetExplicitControllerMode(bool enabled)
        {
            simulator.mouseDeltaAction = enabled ? freeDelta : delta;
            simulator.mouseScrollAction = enabled ? freeScroll : scroll;
            simulator.triggerAction = enabled ? freeTrigger : trigger;
        }

        private static InputActionReference GatedMouse(InputActionMap map, string name, string binding, string controlType = "Vector2")
        {
            var action = map.AddAction(name, InputActionType.Value);
            action.expectedControlType = controlType;
            foreach (string modifier in new[] { "<Keyboard>/space", "<Keyboard>/leftShift" })
                action.AddCompositeBinding("OneModifier").With("Modifier", modifier).With("Binding", binding);
            return InputActionReference.Create(action);
        }

        public void Dispose()
        {
            if (simulator != null)
            {
                simulator.keyboardXTranslateAction = original[0];
                simulator.keyboardYTranslateAction = original[1];
                simulator.keyboardZTranslateAction = original[2];
                simulator.axis2DAction = original[3];
                simulator.restingHandAxis2DAction = original[4];
                simulator.mouseDeltaAction = original[5];
                simulator.mouseScrollAction = original[6];
                simulator.triggerAction = original[7];
            }
            actions.Disable();
            DestroyOwned(freeDelta); DestroyOwned(freeScroll); DestroyOwned(freeTrigger);
            DestroyOwned(delta); DestroyOwned(scroll); DestroyOwned(trigger); DestroyOwned(stationaryAxis); DestroyOwned(stationaryVector); DestroyOwned(actions);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
