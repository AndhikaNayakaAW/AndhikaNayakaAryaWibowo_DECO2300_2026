using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace XRStudyWhiteboard.Editor
{
    /// <summary>Regression checks for camera-pose navigation with tracked HMD offsets.</summary>
    public static class XRStudyNavigationValidation
    {
        [MenuItem("Tools/XR Study Whiteboard/Validate Desktop Simulator Input Ownership", priority = 13)]
        public static void ValidateDesktopSimulatorInputOwnership()
        {
            Require(EditorApplication.isPlaying, "Run input event checks in Play Mode so Unity processes runtime action events");
            const string sampleRoot = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/";
            InputActionAsset source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(sampleRoot + "XR Device Simulator Controls.inputactions");
            InputActionAsset controlsSource = AssetDatabase.LoadAssetAtPath<InputActionAsset>(sampleRoot + "XR Device Controller Controls.inputactions");
            Require(source != null && controlsSource != null, "Official simulator actions are available");
            string originalBindings = source.ToJson();
            string originalControls = controlsSource.ToJson();
            InputActionAsset actions = UnityEngine.Object.Instantiate(source);
            InputActionAsset controls = UnityEngine.Object.Instantiate(controlsSource);
            GameObject testRoot = new GameObject("Simulator input ownership validation") { hideFlags = HideFlags.HideAndDontSave };
            testRoot.SetActive(false);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>("Navigation validation keyboard");
            Mouse mouse = InputSystem.AddDevice<Mouse>("Navigation validation mouse");
            IDisposable ownership = null;
            var references = new System.Collections.Generic.List<InputActionReference>();
            try
            {
                actions.devices = new InputDevice[] { keyboard, mouse };
                controls.devices = new InputDevice[] { keyboard, mouse };
                XRDeviceSimulator simulator = testRoot.AddComponent<XRDeviceSimulator>();
                InputActionReference Reference(InputActionAsset asset, string name)
                {
                    InputActionReference reference = InputActionReference.Create(asset.FindAction(name, true));
                    references.Add(reference);
                    return reference;
                }
                simulator.keyboardXTranslateAction = Reference(actions, "Keyboard X Translate");
                simulator.keyboardYTranslateAction = Reference(actions, "Keyboard Y Translate");
                simulator.keyboardZTranslateAction = Reference(actions, "Keyboard Z Translate");
                simulator.mouseDeltaAction = Reference(actions, "Mouse Delta");
                simulator.mouseScrollAction = Reference(actions, "Mouse Scroll");
                simulator.manipulateLeftAction = Reference(actions, "Manipulate Left");
                simulator.manipulateRightAction = Reference(actions, "Manipulate Right");
                simulator.rotateModeOverrideAction = Reference(actions, "Rotate Mode Override");
                simulator.triggerAction = Reference(controls, "Trigger");
                simulator.axis2DAction = Reference(controls, "Axis 2D");
                simulator.restingHandAxis2DAction = Reference(controls, "Resting Hand Axis 2D");
                actions.Enable();
                controls.Enable();

                // Before the fix, exercise the stock bindings themselves so
                // the red result demonstrates the actual duplicate input.
                MethodInfo configure = typeof(XRStudyRoomLocomotion).GetMethod("ConfigureDesktopSimulatorInput", BindingFlags.Public | BindingFlags.Static);
                if (configure != null)
                    ownership = (IDisposable)configure.Invoke(null, new object[] { simulator });

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A, Key.Q));
                InputSystem.Update();
                Require(Mathf.Abs(ReadAxis(simulator.keyboardZTranslateAction)) < 0.0001f
                    && Mathf.Abs(ReadAxis(simulator.keyboardXTranslateAction)) < 0.0001f
                    && Mathf.Abs(ReadAxis(simulator.keyboardYTranslateAction)) < 0.0001f,
                    "WASD/QE must move only the player, not translate controllers again");
                Require(ReadVector(simulator.axis2DAction) == Vector2.zero
                    && ReadVector(simulator.restingHandAxis2DAction) == Vector2.zero,
                    "Desktop keys must not also feed simulated thumbstick locomotion");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(16f, 9f), scroll = Vector2.up * 120f, buttons = 1 });
                InputSystem.Update();
                Require(ReadVector(simulator.mouseDeltaAction) == Vector2.zero
                    && ReadVector(simulator.mouseScrollAction) == Vector2.zero,
                    "Normal mouse aiming and scrolling must not drift controllers");
                Require(ReadAxis(simulator.triggerAction) == 0f, "Desktop clicks must not also fire the XR trigger");

                foreach (Key modifier in new[] { Key.Space, Key.LeftShift })
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifier));
                    InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(16f, 9f), scroll = Vector2.up * 120f, buttons = 1 });
                    InputSystem.Update();
                    Require(ReadVector(simulator.mouseDeltaAction).sqrMagnitude > 1f
                        && ReadVector(simulator.mouseScrollAction).sqrMagnitude > 1f,
                        modifier + " preserves explicit controller mouse and scroll posing");
                    Require(ReadAxis(simulator.triggerAction) > 0.5f, "Explicit simulator posing retains trigger interaction");
                }
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                bool triggerLatched = false;
                simulator.triggerAction.action.performed += _ => triggerLatched = true;
                simulator.triggerAction.action.canceled += _ => triggerLatched = false;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Require(!triggerLatched, "Posing without a mouse press must not perform the simulator trigger");
                InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
                InputSystem.Update();
                Require(triggerLatched, "Mouse press performs the simulator trigger");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Require(!triggerLatched, "Mouse release cancels the simulator trigger");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.LeftCtrl));
                InputSystem.Update();
                Require(simulator.manipulateRightAction.action.IsPressed()
                    && simulator.rotateModeOverrideAction.action.IsPressed(),
                    "Official controller selection and Ctrl rotation remain functional");
                Require(source.ToJson() == originalBindings && controlsSource.ToJson() == originalControls,
                    "Official sample input assets remain unchanged");
                Debug.Log("Desktop simulator ownership passed: WASD/QE and thumbsticks cannot duplicate player motion; unmodified mouse/scroll cannot drift controllers; Space/left Shift posing and Ctrl rotation remain active; sample assets unchanged.");
            }
            finally
            {
                ownership?.Dispose();
                actions.Disable();
                controls.Disable();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
                UnityEngine.Object.DestroyImmediate(testRoot);
                foreach (InputActionReference reference in references)
                    UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(actions);
                UnityEngine.Object.DestroyImmediate(controls);
            }
        }

        [MenuItem("Tools/XR Study Whiteboard/Validate Controller Selection", priority = 15)]
        public static void ValidateControllerSelection()
        {
            Require(EditorApplication.isPlaying, "Controller selection requires Play Mode");
            var locomotion = UnityEngine.Object.FindFirstObjectByType<XRStudyRoomLocomotion>();
            var simulator = UnityEngine.Object.FindFirstObjectByType<XRDeviceSimulator>();
            MethodInfo select = typeof(XRStudyRoomLocomotion).GetMethod("SelectDesktopController");
            Require(select != null, "T/Y controller selection entry point exists");
            try
            {
                foreach (int hand in new[] { 0, 0, 1, 1, 0 })
                {
                    select.Invoke(locomotion, new object[] { hand });
                    Require(simulator.manipulatingLeftController == (hand == 0)
                        && simulator.manipulatingRightController == (hand == 1),
                        "Selection is exclusive and repeating a key keeps that controller active");
                    Require(simulator.mouseDeltaAction.action.name == "Selected controller aim"
                        && simulator.triggerAction.action.name == "Selected controller trigger",
                        "Selected controller receives unmodified mouse aim and trigger input");
                }
                select.Invoke(locomotion, new object[] { -1 });
                Require(simulator.mouseDeltaAction.action.name == "Pose"
                    && simulator.triggerAction.action.name == "Trigger",
                    "Escape restores cursor drawing and gated simulator input");
                Debug.Log("Controller selection passed: left, right, repeated selection and switching.");
            }
            finally { select.Invoke(locomotion, new object[] { -1 }); }
        }

        private static float ReadAxis(InputActionReference reference) => reference != null ? reference.action.ReadValue<float>() : 0f;
        private static Vector2 ReadVector(InputActionReference reference) => reference != null ? reference.action.ReadValue<Vector2>() : Vector2.zero;

        [MenuItem("Tools/XR Study Whiteboard/Validate Camera Anchor Alignment", priority = 12)]
        public static void ValidateCameraAnchorAlignment()
        {
            GameObject testRoot = new GameObject("Camera anchor validation")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                Transform parent = testRoot.transform;
                parent.SetPositionAndRotation(new Vector3(4f, 0.3f, -6f), Quaternion.Euler(0f, 41f, 0f));
                parent.localScale = Vector3.one * 1.1f;

                Transform origin = CreateChild(parent, "XR Origin", new Vector3(1f, 0.2f, -0.8f));
                origin.localRotation = Quaternion.Euler(0f, -57f, 0f);
                origin.localScale = Vector3.one * 1.2f;
                Transform offset = CreateChild(origin, "Camera Offset", new Vector3(0.08f, 0.11f, 0.13f));
                offset.localRotation = Quaternion.Euler(0f, 9f, 0f);
                Transform camera = CreateChild(offset, "Tracked HMD", new Vector3(0.35f, 1.58f, -0.24f));
                camera.localRotation = Quaternion.Euler(18f, 27f, -6f);

                Vector3 localCameraPosition = camera.localPosition;
                Quaternion localCameraRotation = camera.localRotation;
                Vector3 originScale = origin.localScale;
                Vector3 parentPosition = parent.position;
                Quaternion parentRotation = parent.rotation;
                Vector3 offsetPosition = offset.localPosition;
                Quaternion offsetRotation = offset.localRotation;

                Vector3[] targets =
                {
                    new Vector3(-2f, 1.4f, 3f), new Vector3(3.2f, 1.25f, -1.5f),
                    new Vector3(-3.6f, 1.5f, 4.2f), new Vector3(0f, 1.8f, -2.4f),
                    new Vector3(0f, 1.8f, 0f)
                };
                float[] yaws = { 180f, 45f, -120f, 180f, 0f };
                for (int i = 0; i < targets.Length; i++)
                {
                    Vector3 forward = Quaternion.Euler(0f, yaws[i], 0f) * Vector3.forward;
                    // A supplied look target's vertical component must not tilt the rig.
                    XRStudyRoomLocomotion.AlignCameraToPose(origin, camera, targets[i], forward + Vector3.up * 0.5f);
                    Require(Vector3.Distance(camera.position, targets[i]) < 0.0001f, "Camera reaches world-space target " + i);
                    Require(Vector3.Angle(Vector3.ProjectOnPlane(camera.forward, Vector3.up), forward) < 0.02f,
                        "Camera yaw faces target " + i);
                    Require(Vector3.Distance(camera.localPosition, localCameraPosition) < 0.00001f, "Tracked head offset stays unchanged");
                    Require(Quaternion.Angle(camera.localRotation, localCameraRotation) < 0.02f, "Tracked head yaw/pitch/roll stay unchanged");
                    Require(Vector3.Distance(origin.localScale, originScale) < 0.00001f, "Origin scale stays unchanged");
                    Require(Vector3.Angle(origin.up, Vector3.up) < 0.02f, "Navigation introduces no origin pitch or roll");
                    Require(Vector3.Distance(parent.position, parentPosition) < 0.00001f
                        && Quaternion.Angle(parent.rotation, parentRotation) < 0.02f, "Scene parent stays unchanged");
                    Require(Vector3.Distance(offset.localPosition, offsetPosition) < 0.00001f
                        && Quaternion.Angle(offset.localRotation, offsetRotation) < 0.02f, "Camera offset hierarchy stays unchanged");
                }

                Vector3 settledPosition = origin.position;
                Quaternion settledRotation = origin.rotation;
                XRStudyRoomLocomotion.AlignCameraToPose(origin, camera, camera.position, camera.forward);
                Require(Vector3.Distance(origin.position, settledPosition) < 0.0001f
                    && Quaternion.Angle(origin.rotation, settledRotation) < 0.02f, "Repeated alignment causes no drift");

                // With no horizontal heading supplied, translation still works
                // while the current view direction is left intact.
                Vector3 finalTarget = new Vector3(0.4f, 1.35f, -0.2f);
                XRStudyRoomLocomotion.AlignCameraToPose(origin, camera, finalTarget, Vector3.up);
                Require(Vector3.Distance(camera.position, finalTarget) < 0.0001f
                    && Quaternion.Angle(origin.rotation, settledRotation) < 0.02f, "Vertical-only target preserves yaw");

                Debug.Log("Camera anchor alignment passed: five destinations with nested HMD offsets, translated/scaled parent, tracked head rotation, preserved rig scale, and no repeated-alignment drift.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testRoot);
            }
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException("Camera anchor validation failed: " + label);
        }
    }
}
