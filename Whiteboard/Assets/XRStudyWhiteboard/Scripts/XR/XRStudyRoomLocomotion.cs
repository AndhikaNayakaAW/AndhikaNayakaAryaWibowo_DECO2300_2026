using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Provides the missing player movement for the study room.
    ///
    /// On a headset it reads the left thumbstick for movement and the right
    /// thumbstick for snap turning. When no XR device is available, it gives
    /// the Unity Game view a small desktop test mode so the scene and
    /// whiteboard can be tested without a headset.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class XRStudyRoomLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float fastMoveSpeed = 4.5f;
        [SerializeField] private float snapTurnDegrees = 30f;
        [SerializeField] private float snapTurnCooldown = 0.35f;
        [SerializeField] private float controllerDeadzone = 0.15f;
        [SerializeField] private float desktopLookSensitivity = 0.12f;
        [SerializeField] private float desktopWheelMoveScale = 0.0025f;
        [SerializeField] private float desktopVerticalMoveSpeed = 2.2f;
        [SerializeField] private float desktopSpawnHeight = 1.8f;
        [SerializeField] private float desktopSeatedHeight = 1.45f;
        [SerializeField] private float desktopSeatedPitch = 12f;
        [SerializeField] private float desktopSpawnDepth = 0f;
        [SerializeField] private float desktopMinimumHeight = 0.25f;
        [SerializeField] private float desktopMaximumHeight = 3.2f;
        [SerializeField] private float minimumX = -5.25f;
        [SerializeField] private float maximumX = 5.25f;
        [SerializeField] private float minimumZ = -5.85f;
        [SerializeField] private float maximumZ = 5.85f;

        // Desktop shortcuts are standing viewpoints, not seated camera
        // poses.  They make it possible to test the full room quickly when
        // a headset is not connected.  The physical headset uses the floor
        // TeleportationArea with the controller ray instead.
        private static readonly Vector3 WhiteboardPoint = new Vector3(0f, 0f, -2.4f);
        private static readonly Vector3 WhiteboardTarget = new Vector3(0f, 1.45f, -5.35f);
        private static readonly Vector3[] StudentPoints =
        {
            new Vector3(-3.15f, 0f, 1.25f),
            new Vector3(0f, 0f, 1.25f),
            new Vector3(3.15f, 0f, 1.25f),
            new Vector3(-3.15f, 0f, 3.45f),
            new Vector3(0f, 0f, 3.45f),
            new Vector3(3.15f, 0f, 3.45f)
        };
        private static readonly Vector3[] StudentTargets =
        {
            WhiteboardTarget,
            WhiteboardTarget,
            WhiteboardTarget,
            WhiteboardTarget,
            WhiteboardTarget,
            WhiteboardTarget
        };

        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private float desktopPitch;
        private float desktopHeight;
        private float snapTurnTimer;
        private float desktopTeleportCooldown;
        private bool desktopMode;
        private bool seatedDesktopView;
        private bool desktopNavigationPointerHeld;
        private XRInputDevice leftController;
        private XRInputDevice rightController;
        private readonly List<StudyTableTeleportPoint> tablePoints = new List<StudyTableTeleportPoint>();
        private readonly List<MonoBehaviour> simulatorResetTargets = new List<MonoBehaviour>();
        private readonly List<FieldInfo> simulatorResetFields = new List<FieldInfo>();

        private bool IsDesktopMode => desktopMode;
        public bool IsDesktopNavigationPointerHeld => desktopNavigationPointerHeld;

        public void SetOrigin(Transform origin)
        {
            xrOrigin = origin;
            FindCamera();
        }

        private void Awake()
        {
            if (xrOrigin == null)
                xrOrigin = FindChild(transform, "XR Origin");

            FindCamera();
            if (xrOrigin != null)
            {
                resetPosition = xrOrigin.localPosition;
                resetRotation = xrOrigin.localRotation;
            }

            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            desktopMode = DetermineDesktopMode();

            // The editor simulator can drive the HMD pose independently of
            // this desktop controller. Keep its controller input available,
            // but leave the Game view camera under this script's control so a
            // stale simulated HMD pose cannot rotate the player underground.
            TrackedPoseDriver cameraPoseDriver = cameraTransform != null
                ? cameraTransform.GetComponent<TrackedPoseDriver>()
                : null;
            if (cameraPoseDriver != null)
                cameraPoseDriver.enabled = !desktopMode;

            // The imported classroom has a different desk layout from the
            // original scene. Start desktop testing in the central aisle and
            // raise the origin so the camera is at standing height. A headset
            // still uses its real tracked floor height instead.
            if (desktopMode && xrOrigin != null)
            {
                desktopHeight = desktopSpawnHeight;
                resetPosition.x = 0f;
                resetPosition.y = desktopHeight;
                resetPosition.z = desktopSpawnDepth;
                xrOrigin.localPosition = resetPosition;
                SetDesktopCameraOffset();
            }

            // The template's Android hand-permission helper is useful on a
            // headset but produces a misleading warning in a desktop Game
            // view where no hand subsystem exists.
            Transform handPermissions = FindChild(transform, "Hands Permissions Manager");
            if (handPermissions != null)
                handPermissions.gameObject.SetActive(!desktopMode);

            XRInputModalityManager modalityManager = GetComponentInChildren<XRInputModalityManager>(true);
            if (modalityManager != null)
                // Keep Unity's built-in controller/hand modality active in
                // the editor. Disabling it hides the official controller
                // models and rays that the XR Device Simulator drives.
                modalityManager.enabled = true;

            // If the official simulator is added later, keep it editor-only.
            Transform deviceSimulator = FindChildContaining(transform, "XR Device Simulator");
            if (deviceSimulator == null)
                deviceSimulator = FindSceneChildContaining("XR Device Simulator");
            if (deviceSimulator != null)
            {
                deviceSimulator.gameObject.SetActive(desktopMode);
                if (desktopMode)
                {
                    Invoke(nameof(SelectBothControllersSimulator), 0.35f);
                    Invoke(nameof(SelectBothControllersSimulator), 1f);
                    Invoke(nameof(SelectBothControllersSimulator), 2f);
                    // The official simulator changes to HMD input when the
                    // Game view receives focus. Keep both virtual controllers
                    // selected so cursor testing cannot lose the controller
                    // ray after a normal focus click.
                    InvokeRepeating(nameof(SelectBothControllersSimulator), 0.5f, 0.5f);
                    Invoke(nameof(ForceDesktopStartView), 2.25f);
                }
            }
        }

        private void Update()
        {
            if (xrOrigin == null)
                return;

            if (desktopMode)
                HandleDesktopInput();

            RefreshTablePoints();

            HandleXRInput();
            snapTurnTimer -= Time.unscaledDeltaTime;
            desktopTeleportCooldown -= Time.unscaledDeltaTime;
        }

        private void LateUpdate()
        {
            if (!desktopMode || xrOrigin == null)
                return;

            SetDesktopCameraOffset();
            Vector3 position = xrOrigin.localPosition;
            if (Mathf.Abs(position.y - desktopHeight) > 0.01f)
            {
                // The editor simulator may apply a tracked floor pose when
                // the Game view receives focus. Keep the desktop test at a
                // standing height without blocking normal X/Z movement.
                position.y = desktopHeight;
                xrOrigin.localPosition = position;
            }
            if (position.y >= desktopMinimumHeight)
                return;

            // XR Origin and the editor simulator can apply their own tracking
            // pose after Awake. If that pose puts the player below the floor,
            // recover automatically without interfering with normal movement.
            position.x = 0f;
            position.y = desktopSpawnHeight;
            position.z = desktopSpawnDepth;
            xrOrigin.localPosition = position;
            xrOrigin.localRotation = resetRotation;

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
        }

        private void HandleDesktopInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            float speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? fastMoveSpeed
                : moveSpeed;
            MoveInViewDirection(input, speed * Time.unscaledDeltaTime);

            float vertical = 0f;
            // Space is reserved by the official XR Device Simulator for
            // manipulating the right virtual controller with the mouse.
            // Use Page Up/Page Down for desktop height so controller aiming
            // does not unexpectedly lift the player.
            if (keyboard.pageUpKey.isPressed)
                vertical += 1f;
            if (keyboard.pageDownKey.isPressed)
                vertical -= 1f;
            if (Mathf.Abs(vertical) > 0f)
            {
                Vector3 position = xrOrigin.localPosition;
                desktopHeight = Mathf.Clamp(
                    desktopHeight + vertical * desktopVerticalMoveSpeed * Time.unscaledDeltaTime,
                    desktopMinimumHeight,
                    desktopMaximumHeight);
                position.y = desktopHeight;
                xrOrigin.localPosition = position;
            }

            float turn = 0f;
            if (keyboard.leftArrowKey.isPressed || keyboard.qKey.isPressed) turn -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.eKey.isPressed) turn += 1f;
            if (Mathf.Abs(turn) > 0f)
                Turn(turn * 90f * Time.unscaledDeltaTime);

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    Turn(delta.x * desktopLookSensitivity);
                    desktopPitch = Mathf.Clamp(desktopPitch - delta.y * desktopLookSensitivity, -75f, 75f);
                    if (cameraTransform != null)
                        cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
                }

                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                    MoveInViewDirection(Vector2.up * Mathf.Sign(wheel), Mathf.Abs(wheel) * desktopWheelMoveScale);

                // Focusing the Game view can make the official simulator
                // switch back to HMD input. Re-select both virtual
                // controllers after that focus click so cursor/controller
                // testing continues without an extra recovery step.
                if (mouse.leftButton.wasPressedThisFrame)
                    Invoke(nameof(SelectBothControllersSimulator), 0.1f);
            }

            if (keyboard.rKey.wasPressedThisFrame)
                ResetView();

            if (keyboard.jKey.wasPressedThisFrame)
                StandFromSeat();

            HandleDesktopTeleportShortcuts(keyboard);
        }

        private void HandleDesktopTeleportShortcuts(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TeleportDesktop(WhiteboardPoint, WhiteboardTarget, desktopSpawnHeight, false);
                return;
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
                TryTeleportToTable(0);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                TryTeleportToTable(1);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                TryTeleportToTable(2);
            else if (keyboard.digit5Key.wasPressedThisFrame)
                TryTeleportToTable(3);
            else if (keyboard.digit6Key.wasPressedThisFrame)
                TryTeleportToTable(4);
            else if (keyboard.digit7Key.wasPressedThisFrame)
                TryTeleportToTable(5);
            else if (keyboard.digit8Key.wasPressedThisFrame)
                TryTeleportToTable(6);
            else if (keyboard.digit9Key.wasPressedThisFrame)
                TryTeleportToTable(7);
        }

        private void RefreshTablePoints()
        {
            IReadOnlyList<StudyTableTeleportPoint> activePoints = StudyTableTeleportPoint.Points;
            if (tablePoints.Count == activePoints.Count)
                return;

            tablePoints.Clear();
            for (int i = 0; i < activePoints.Count; i++)
            {
                if (activePoints[i] != null)
                    tablePoints.Add(activePoints[i]);
            }
        }

        private void TryTeleportToTable(int index)
        {
            RefreshTablePoints();
            if (index >= 0 && index < tablePoints.Count)
            {
                StudyTableTeleportPoint table = tablePoints[index];
                TeleportDesktop(table.TeleportPosition, table.ViewTarget, desktopSeatedHeight, true, GetTableViewPitch(table));
                return;
            }

            // Keep the original fallback points available if a damaged or
            // older scene has not created runtime table anchors yet.
            if (index >= 0 && index < StudentPoints.Length)
                TeleportDesktop(StudentPoints[index], StudentTargets[index], desktopSeatedHeight, true);
        }

        private float GetTableViewPitch(StudyTableTeleportPoint table)
        {
            Vector3 point = table.TeleportPosition;
            Vector3 target = table.ViewTarget;
            float horizontalDistance = Vector2.Distance(
                new Vector2(point.x, point.z),
                new Vector2(target.x, target.z));
            float pitch = Mathf.Atan2(desktopSeatedHeight - target.y, Mathf.Max(0.1f, horizontalDistance)) * Mathf.Rad2Deg;
            // The paper is below the eye line. A deeper downward pitch keeps
            // the paper, its tool button, and the chair in one table view,
            // while the board remains visible in the upper part of the view.
            return Mathf.Clamp(pitch, 10f, 18f);
        }

        private void TeleportDesktop(Vector3 point, Vector3 target, float targetHeight, bool seated, float? seatedPitchOverride = null)
        {
            if (xrOrigin == null || desktopTeleportCooldown > 0f)
                return;

            desktopTeleportCooldown = 0.18f;
            desktopHeight = targetHeight;
            seatedDesktopView = seated;
            Vector3 position = point;
            position.y = desktopHeight;
            position.x = Mathf.Clamp(position.x, minimumX, maximumX);
            position.z = Mathf.Clamp(position.z, minimumZ, maximumZ);
            xrOrigin.position = position;

            Vector3 direction = target - position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                xrOrigin.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            desktopPitch = seated
                ? (seatedPitchOverride.HasValue ? seatedPitchOverride.Value : desktopSeatedPitch)
                : 0f;
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
        }

        private void StandFromSeat()
        {
            if (!desktopMode || xrOrigin == null)
                return;

            seatedDesktopView = false;
            desktopHeight = desktopSpawnHeight;
            desktopPitch = 0f;
            Vector3 position = xrOrigin.position;
            position.y = desktopHeight;
            xrOrigin.position = position;
        }

        private void HandleXRInput()
        {
            if (!leftController.isValid)
                leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!rightController.isValid)
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (leftController.isValid && leftController.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 move))
            {
                if (move.magnitude >= controllerDeadzone)
                {
                    move = Vector2.ClampMagnitude(move, 1f);
                    MoveInViewDirection(move, moveSpeed * Time.unscaledDeltaTime);
                }
            }

            if (rightController.isValid
                && rightController.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 turnAxis)
                && Mathf.Abs(turnAxis.x) >= 0.7f
                && snapTurnTimer <= 0f)
            {
                Turn(Mathf.Sign(turnAxis.x) * snapTurnDegrees);
                snapTurnTimer = snapTurnCooldown;
            }
        }

        private void MoveInViewDirection(Vector2 input, float distance)
        {
            if (input.sqrMagnitude <= 0.0001f)
                return;

            Transform view = cameraTransform != null ? cameraTransform : xrOrigin;
            Vector3 forward = view.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = xrOrigin.forward;
            forward.Normalize();

            // Cross(up, forward) gives the camera's real screen-right axis.
            // This keeps D moving right and A moving left.
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 movement = (forward * input.y + right * input.x) * distance;
            Vector3 position = xrOrigin.position + movement;
            position.x = Mathf.Clamp(position.x, minimumX, maximumX);
            position.z = Mathf.Clamp(position.z, minimumZ, maximumZ);
            xrOrigin.position = position;
        }

        private void Turn(float degrees)
        {
            xrOrigin.Rotate(Vector3.up, degrees, Space.World);
        }

        private void ResetView()
        {
            if (desktopMode)
            {
                desktopHeight = desktopSpawnHeight;
                seatedDesktopView = false;
            }
            xrOrigin.localPosition = resetPosition;
            xrOrigin.localRotation = resetRotation;
            if (desktopMode)
                SetDesktopCameraOffset();
            desktopPitch = 0f;
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.identity;
        }

        private void ForceDesktopStartView()
        {
            if (!desktopMode || xrOrigin == null)
                return;

            // XR Device Simulator can publish its initial simulated pose a
            // moment after scene Awake. Reapply the known standing position
            // once after that hand-off so Play never begins under a desk.
            ResetView();
        }

        private void FindCamera()
        {
            if (cameraTransform == null && xrOrigin != null)
            {
                Camera camera = xrOrigin.GetComponentInChildren<Camera>(true);
                if (camera != null)
                    cameraTransform = camera.transform;
            }
        }

        private void SetDesktopCameraOffset()
        {
            if (!desktopMode || cameraTransform == null || cameraTransform.parent == null)
                return;

            // In desktop mode the origin represents the player's standing
            // height. Keeping the XRI Camera Offset at zero prevents the
            // Device Simulator from applying a second, conflicting height.
            cameraTransform.parent.localPosition = Vector3.zero;
            cameraTransform.parent.localRotation = Quaternion.identity;

            // The editor simulator can still publish a stale tracked pose
            // even with its TrackedPoseDriver disabled. Pin the camera back
            // to the standing eye point so a controller test cannot place
            // the desktop view below a desk.
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);

            // Keep the XRI origin's own offset at zero as well. This prevents
            // XR Simulation from adding a second camera height on top of the
            // desktop standing-height origin.
            XROrigin origin = xrOrigin != null ? xrOrigin.GetComponent<XROrigin>() : null;
            if (origin != null)
                origin.CameraYOffset = 0f;
        }

        private void SelectBothControllersSimulator()
        {
            if (!desktopMode)
                return;

            // The simulator creates its UI canvas beside the simulator object
            // (often in the DontDestroyOnLoad scene), so it is not always a
            // child of the simulator root. Search loaded scene instances so
            // the editor starts in controller mode instead of falling back
            // to HMD mode after each Play.
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "XRDeviceSimulatorUI")
                    continue;
                if (!behaviour.gameObject.scene.IsValid())
                    continue;

                MethodInfo activateBothControllers = behaviour.GetType().GetMethod(
                    "OnActivateBothControllers",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (activateBothControllers != null)
                    activateBothControllers.Invoke(behaviour, null);

                return;
            }
        }

        private bool DetermineDesktopMode()
        {
#if UNITY_EDITOR
            // Keep the Unity Game view deterministic even when XR Simulation
            // exposes virtual controller devices in the editor. A physical
            // headset uses the Android build path below.
            return true;
#elif UNITY_ANDROID
            return false;
#else
            return !XRSettings.isDeviceActive;
#endif
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
                return null;
            if (root.name == childName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindChildContaining(Transform root, string text)
        {
            if (root == null)
                return null;
            if (root.name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildContaining(root.GetChild(i), text);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindSceneChildContaining(string text)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildContaining(roots[i].transform, text);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void OnGUI()
        {
            if (!IsDesktopMode)
                return;

            // IMGUI receives keyboard events even when the new Input System
            // has not yet re-established Game-view focus. Keep the same
            // shortcuts available through this path so chair/board changes
            // cannot silently fail after clicking the simulator panel.
            if (Event.current.type == EventType.KeyDown && Event.current.isKey)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Alpha1:
                        TeleportDesktop(WhiteboardPoint, WhiteboardTarget, desktopSpawnHeight, false);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha2:
                        TryTeleportToTable(0);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha3:
                        TryTeleportToTable(1);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha4:
                        TryTeleportToTable(2);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha5:
                        TryTeleportToTable(3);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha6:
                        TryTeleportToTable(4);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha7:
                        TryTeleportToTable(5);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha8:
                        TryTeleportToTable(6);
                        Event.current.Use();
                        break;
                    case KeyCode.Alpha9:
                        TryTeleportToTable(7);
                        Event.current.Use();
                        break;
                    case KeyCode.J:
                        StandFromSeat();
                        Event.current.Use();
                        break;
                    case KeyCode.C:
                        CenterView();
                        Event.current.Use();
                        break;
                }
            }

            GUI.Box(
                new Rect(16f, 16f, 520f, 172f),
                "DESKTOP TEST CONTROLS\n\nWASD: move   Q/E or arrows: turn\nJ: stand/jump from a table   C: center view\nRight-drag: look   Scroll: closer/farther   R: reset\n1: whiteboard   2-9: table views facing the paper\nL Mouse: draw/trigger   R Mouse: erase in desktop test\nTable menu: choose pencil, eraser, or clear paper\nBuilt-in XR Device Simulator: select Controller; hold Space to pose it\nVR: aim the built-in controller ray at the floor and press the trigger to teleport");

            // Clickable fallbacks are deliberately visible in the Game view.
            // They are especially useful when the simulator panel currently
            // owns keyboard focus, and they make the intended test route
            // obvious: sit, stand, walk to the board, and return to a desk.
            // Keep the navigation pad in the upper-right corner, away from
            // the whiteboard and its world-space tool panel. The official
            // simulator and help text occupy the left side of the Game view.
            RefreshTablePoints();
            int tableCount = tablePoints.Count > 0 ? tablePoints.Count : StudentPoints.Length;
            int columns = tableCount > 9 ? 4 : 3;
            float buttonWidth = tableCount > 9 ? 84f : 108f;
            float gap = tableCount > 9 ? 5f : 6f;
            float navigationWidth = columns * buttonWidth + (columns - 1) * gap;
            float buttonX = Mathf.Max(16f, Screen.width - navigationWidth - 16f);
            int tableRows = Mathf.CeilToInt(tableCount / (float)columns);
            float navigationHeight = 32f + gap + tableRows * 32f + Mathf.Max(0, tableRows - 1) * gap;
            // Anchor the complete navigation pad to the bottom edge. The old
            // fixed y-position placed the last table row below short Game
            // views, so TABLE 7-9 were visibly cut off.
            float buttonY = Mathf.Max(16f, Screen.height - navigationHeight - 16f);
            // The table grid becomes four columns in the imported room. Give
            // the three action buttons their own wider row so WHITEBOARD,
            // STAND / JUMP, and CENTER VIEW remain readable instead of being
            // clipped to the table-cell width.
            float actionGap = gap;
            float actionWidth = (navigationWidth - 2f * actionGap) / 3f;

            // Desktop navigation is drawn with IMGUI rather than the
            // world-space canvases. Latch its pointer here so a click on a
            // teleport/table button cannot also be interpreted as a drawing
            // press when the docked Game view reports a different coordinate
            // space to the Input System.
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && IsNavigationPointerInside(
                    Event.current.mousePosition,
                    tableCount,
                    columns,
                    buttonX,
                    buttonY,
                    buttonWidth,
                    gap,
                    actionWidth))
            {
                desktopNavigationPointerHeld = true;
            }
            else if (Event.current.type == EventType.MouseUp
                && Event.current.button == 0)
            {
                desktopNavigationPointerHeld = false;
            }

            if (Event.current.type == EventType.MouseUp
                && (Event.current.button == 0 || Event.current.button < 0))
            {
                Vector2 pointer = Event.current.mousePosition;
                Rect whiteboardRect = new Rect(buttonX, buttonY, actionWidth, 32f);
                Rect standRect = new Rect(buttonX + actionWidth + actionGap, buttonY, actionWidth, 32f);
                Rect centerRect = new Rect(buttonX + 2f * (actionWidth + actionGap), buttonY, actionWidth, 32f);
                if (whiteboardRect.Contains(pointer))
                {
                    TeleportDesktop(WhiteboardPoint, WhiteboardTarget, desktopSpawnHeight, false);
                    Event.current.Use();
                }
                else if (standRect.Contains(pointer))
                {
                    StandFromSeat();
                    Event.current.Use();
                }
                else if (centerRect.Contains(pointer))
                {
                    CenterView();
                    Event.current.Use();
                }
                else
                {
                    for (int i = 0; i < tableCount; i++)
                    {
                        float x = buttonX + (i % columns) * (buttonWidth + gap);
                        float y = buttonY + 32f + gap + (i / columns) * (32f + gap);
                        if (!new Rect(x, y, buttonWidth, 32f).Contains(pointer))
                            continue;

                        TryTeleportToTable(i);
                        Event.current.Use();
                        break;
                    }
                }
            }

            if (GUI.Button(new Rect(buttonX, buttonY, actionWidth, 32f), "WHITEBOARD"))
                TeleportDesktop(WhiteboardPoint, WhiteboardTarget, desktopSpawnHeight, false);
            if (GUI.Button(new Rect(buttonX + actionWidth + actionGap, buttonY, actionWidth, 32f), "STAND / JUMP"))
                StandFromSeat();
            if (GUI.Button(new Rect(buttonX + 2f * (actionWidth + actionGap), buttonY, actionWidth, 32f), "CENTER VIEW"))
                CenterView();

            buttonY += 32f + gap;
            for (int i = 0; i < tableCount; i++)
            {
                float x = buttonX + (i % columns) * (buttonWidth + gap);
                float y = buttonY + (i / columns) * (32f + gap);
                if (GUI.Button(new Rect(x, y, buttonWidth, 32f), "TABLE " + (i + 1).ToString()))
                    TryTeleportToTable(i);
            }

        }

        private static bool IsNavigationPointerInside(
            Vector2 pointer,
            int tableCount,
            int columns,
            float buttonX,
            float buttonY,
            float buttonWidth,
            float gap,
            float actionWidth)
        {
            if (new Rect(buttonX, buttonY, actionWidth, 32f).Contains(pointer)
                || new Rect(buttonX + actionWidth + gap, buttonY, actionWidth, 32f).Contains(pointer)
                || new Rect(buttonX + 2f * (actionWidth + gap), buttonY, actionWidth, 32f).Contains(pointer))
                return true;

            float tableTop = buttonY + 32f + gap;
            for (int i = 0; i < tableCount; i++)
            {
                float x = buttonX + (i % columns) * (buttonWidth + gap);
                float y = tableTop + (i / columns) * (32f + gap);
                if (new Rect(x, y, buttonWidth, 32f).Contains(pointer))
                    return true;
            }

            return false;
        }

        private void CenterView()
        {
            if (desktopMode)
            {
                ResetView();
                ResetSimulatorControllers();
                return;
            }

            // A real headset owns the controller poses; recentering should
            // realign the player's view without trying to move tracked hands
            // away from their physical positions.
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            for (int i = 0; i < subsystems.Count; i++)
            {
                if (subsystems[i] != null && subsystems[i].running)
                    subsystems[i].TryRecenter();
            }
        }

        private void ResetSimulatorControllers()
        {
            simulatorResetTargets.Clear();
            simulatorResetFields.Clear();

            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null
                    || !behaviour.gameObject.scene.IsValid()
                    || behaviour.GetType().Name.IndexOf("Simulator", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                FieldInfo resetField = behaviour.GetType().GetField(
                    "m_ResetInput",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (resetField == null || resetField.FieldType != typeof(bool))
                    continue;

                resetField.SetValue(behaviour, true);
                simulatorResetTargets.Add(behaviour);
                simulatorResetFields.Add(resetField);
            }

            if (simulatorResetTargets.Count > 0)
                StartCoroutine(ClearSimulatorResetFlags());
        }

        private IEnumerator ClearSimulatorResetFlags()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            for (int i = 0; i < simulatorResetTargets.Count && i < simulatorResetFields.Count; i++)
            {
                if (simulatorResetTargets[i] != null)
                    simulatorResetFields[i].SetValue(simulatorResetTargets[i], false);
            }
        }
    }
}
