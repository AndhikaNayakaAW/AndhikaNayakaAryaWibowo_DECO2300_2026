// script overview: this script handles movement and navigation around the classroom. physical left x goes to
// the whiteboard, left y cycles through tables, the left stick moves, and the right stick turns. teleportation
// aligns the actual camera with the destination by moving the xr origin. desktop controls include keyboard
// movement and t or y to select a simulated hand. it also keeps desktop camera and controller input ownership
// consistent.

// search: teleportation | left controller | x button | y button | movement | t key | y key | desktop
// simulator.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system.collections available to this script.
using System.Collections;
// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make the types in system.reflection available to this script.
using System.Reflection;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make keyboard, mouse, and input-action types available for controller and desktop input.
using UnityEngine.InputSystem;
// make the types in unity engine.input system.xr available to this script.
using UnityEngine.InputSystem.XR;
// make the types in unity engine.scene management available to this script.
using UnityEngine.SceneManagement;
// make the types in unity engine.xr available to this script.
using UnityEngine.XR;
// make the types in unity.xr.core utils available to this script.
using Unity.XR.CoreUtils;
// make the types in unity engine.xr.interaction.toolkit.inputs available to this script.
using UnityEngine.XR.Interaction.Toolkit.Inputs;
// make the types in unity engine.xr.interaction.toolkit.inputs.simulation available to this script.
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
// use xr common usages as the local name for unity engine.xr.common usages.
using XRCommonUsages = UnityEngine.XR.CommonUsages;
// use xr input device as the local name for unity engine.xr.input device.
using XRInputDevice = UnityEngine.XR.InputDevice;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // provides the missing player movement for the study room.
    // on a headset it reads the left thumbstick for movement and the right
    // thumbstick for snap turning. when no xr device is available, it gives
    // the unity game view a small desktop test mode so the scene and
    // whiteboard can be tested without a headset.
    // apply unity or c sharp metadata: default execution order(10000).
    [DefaultExecutionOrder(10000)]
    // declare the xr study room locomotion class based on unity behaviour.
    public sealed class XRStudyRoomLocomotion : MonoBehaviour
    // begin the class xr study room locomotion block.
    {
        // handle classroom movement, teleportation, snap turning, and desktop testing.
        // xr origin: the player rig root. moving it carries the tracked camera and controllers together. keep
        // xr origin as transform for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Transform xrOrigin;
        // camera transform: the actual player camera used for eye position, view direction, and teleport
        // alignment. keep camera transform as transform for later use. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private Transform cameraTransform;
        // whiteboard anchor: the fallback camera destination near the board. keep whiteboard anchor as seat
        // anchor for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private SeatAnchor whiteboardAnchor;
        // center anchor: the camera destination used to return to the centre of the room. keep center anchor
        // as seat anchor for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private SeatAnchor centerAnchor;
        // move speed: the normal movement speed in metres per second. keep move speed as float, initially 2.2.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private float moveSpeed = 2.2f;
        // fast move speed: the faster desktop movement speed used with the fast-move key. keep fast move speed
        // as float, initially 4.5. allow unity to save and configure this field in the inspector.
        [SerializeField] private float fastMoveSpeed = 4.5f;
        // snap turn degrees: the angle applied by one physical-controller snap turn. keep snap turn degrees as
        // float, initially 30. allow unity to save and configure this field in the inspector.
        [SerializeField] private float snapTurnDegrees = 30f;
        // snap turn cooldown: the minimum delay before another snap turn can be accepted. keep snap turn
        // cooldown as float, initially 0.35. allow unity to save and configure this field in the inspector.
        [SerializeField] private float snapTurnCooldown = 0.35f;
        // controller deadzone: the minimum stick movement needed before locomotion begins. keep controller
        // deadzone as float, initially 0.15. allow unity to save and configure this field in the inspector.
        [SerializeField] private float controllerDeadzone = 0.15f;
        // keep desktop look sensitivity as float, initially 0.12. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private float desktopLookSensitivity = 0.12f;
        // keep desktop wheel move scale as float, initially 0.0025. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private float desktopWheelMoveScale = 0.0025f;
        // keep desktop vertical move speed as float, initially 2.2. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private float desktopVerticalMoveSpeed = 2.2f;
        // keep desktop spawn height as float, initially 1.8. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private float desktopSpawnHeight = 1.8f;
        // keep desktop seated height as float, initially 1.45. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private float desktopSeatedHeight = 1.45f;
        // keep desktop seated pitch as float, initially 12. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private float desktopSeatedPitch = 12f;
        // keep desktop spawn depth as float, initially 0. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float desktopSpawnDepth = 0f;
        // keep desktop minimum height as float, initially 0.25. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private float desktopMinimumHeight = 0.25f;
        // keep desktop maximum height as float, initially 3.2. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private float desktopMaximumHeight = 3.2f;
        // keep minimum x as float, initially the negative of 5.25. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private float minimumX = -5.25f;
        // keep maximum x as float, initially 5.25. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float maximumX = 5.25f;
        // keep minimum z as float, initially the negative of 5.85. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private float minimumZ = -5.85f;
        // keep maximum z as float, initially 5.85. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private float maximumZ = 5.85f;

        // fallback room destinations for older scenes without desk bounds.
        // imported desks use explicit camera-height seatanchors instead.
        // keep whiteboard point as three-dimensional vector, initially a new three-dimensional vector using 0,
        // 0, the negative of 2.4.
        private static readonly Vector3 WhiteboardPoint = new Vector3(0f, 0f, -2.4f);
        // keep whiteboard target as three-dimensional vector, initially a new three-dimensional vector using
        // 0, 1.45, the negative of 5.35.
        private static readonly Vector3 WhiteboardTarget = new Vector3(0f, 1.45f, -5.35f);
        // keep student points as three-dimensional vector[], initially the listed initial values.
        private static readonly Vector3[] StudentPoints =
        // begin the array initializer expression values.
        {
            // a new three-dimensional vector using the negative of 3.15, 0, 1.25.
            new Vector3(-3.15f, 0f, 1.25f),
            // a new three-dimensional vector using 0, 0, 1.25.
            new Vector3(0f, 0f, 1.25f),
            // a new three-dimensional vector using 3.15, 0, 1.25.
            new Vector3(3.15f, 0f, 1.25f),
            // a new three-dimensional vector using the negative of 3.15, 0, 3.45.
            new Vector3(-3.15f, 0f, 3.45f),
            // a new three-dimensional vector using 0, 0, 3.45.
            new Vector3(0f, 0f, 3.45f),
            // a new three-dimensional vector using 3.15, 0, 3.45.
            new Vector3(3.15f, 0f, 3.45f)
        // finish the array initializer expression values.
        };
        // keep student targets as three-dimensional vector[], initially the listed initial values.
        private static readonly Vector3[] StudentTargets =
        // begin the array initializer expression values.
        {
            // whiteboard target.
            WhiteboardTarget,
            // whiteboard target.
            WhiteboardTarget,
            // whiteboard target.
            WhiteboardTarget,
            // whiteboard target.
            WhiteboardTarget,
            // whiteboard target.
            WhiteboardTarget,
            // whiteboard target.
            WhiteboardTarget
        // finish the array initializer expression values.
        };

        // keep reset position as three-dimensional vector for later use.
        private Vector3 resetPosition;
        // keep reset rotation as quaternion for later use.
        private Quaternion resetRotation;
        // keep desktop pitch as float for later use.
        private float desktopPitch;
        // keep desktop height as float for later use.
        private float desktopHeight;
        // keep snap turn timer as float for later use.
        private float snapTurnTimer;
        // keep desktop teleport cooldown as float for later use.
        private float desktopTeleportCooldown;
        // keep desktop mode as bool for later use.
        private bool desktopMode;
        // keep seated desktop view as bool for later use.
        private bool seatedDesktopView;
        // keep desktop navigation pointer held as bool for later use.
        private bool desktopNavigationPointerHeld;
        // keep desktop help expanded as bool for later use.
        private bool desktopHelpExpanded;
        // left primary was pressed: the previous left-x state used to trigger whiteboard teleportation only
        // once per press. keep left primary was pressed as bool for later use.
        private bool leftPrimaryWasPressed;
        // left secondary was pressed: the previous left-y state used to advance tables only once per press.
        // keep left secondary was pressed as bool for later use.
        private bool leftSecondaryWasPressed;
        // next quest table index: the index that the next left-y teleport will visit. keep next quest table
        // index as int for later use.
        private int nextQuestTableIndex;
        // keep left controller as xr input device for later use.
        private XRInputDevice leftController;
        // keep right controller as xr input device for later use.
        private XRInputDevice rightController;
        // keep table points as list<study table teleport point>, initially a new list<study table teleport
        // point>.
        private readonly List<StudyTableTeleportPoint> tablePoints = new List<StudyTableTeleportPoint>();
        // keep simulator reset targets as list<unity behaviour>, initially a new list<unity behaviour>.
        private readonly List<MonoBehaviour> simulatorResetTargets = new List<MonoBehaviour>();
        // keep simulator reset fields as list<field info>, initially a new list<field info>.
        private readonly List<FieldInfo> simulatorResetFields = new List<FieldInfo>();

        // keep simulator input ownership as desktop simulator input ownership for later use.
        private DesktopSimulatorInputOwnership simulatorInputOwnership;
        // expose the desktop controller hand property for reading while keeping writes inside this class.
        public int DesktopControllerHand { get; private set; } = -1;
        // expose is desktop controller mode as desktop mode and desktop controller hand is at least 0.
        public bool IsDesktopControllerMode => desktopMode && DesktopControllerHand >= 0;
        // expose is desktop mode as desktop mode.
        private bool IsDesktopMode => desktopMode;
        // expose is desktop navigation pointer held as desktop navigation pointer held.
        public bool IsDesktopNavigationPointerHeld => desktopNavigationPointerHeld;

        // method: set origin.
        // connect the root transform that moves the complete player rig and locate its camera.
        public void SetOrigin(Transform origin)
        // begin the method set origin block.
        {
            // set xr origin to origin.
            xrOrigin = origin;
            // call find camera: locate the camera below the xr origin if no camera reference has been
            // assigned.
            FindCamera();
        // finish the method set origin block.
        }

        // method: awake.
        // find the rig and camera, choose headset or desktop mode, configure the guide, and prevent competing
        // desktop pose drivers.
        private void Awake()
        // begin the method awake block.
        {
            // run the following statement only when xr origin is missing.
            if (xrOrigin == null)
                // set xr origin to find child using transform, the text "xr origin".
                xrOrigin = FindChild(transform, "XR Origin");

            // call find camera: locate the camera below the xr origin if no camera reference has been
            // assigned.
            FindCamera();
            // run the following statement only when xr origin exists.
            if (xrOrigin != null)
            // begin the if statement block.
            {
                // set reset position to xr origin local position.
                resetPosition = xrOrigin.localPosition;
                // set reset rotation to xr origin local rotation.
                resetRotation = xrOrigin.localRotation;
            // finish the if statement block.
            }

            // set left controller to input devices: get device at xr node using xr node left hand.
            leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            // set right controller to input devices: get device at xr node using xr node right hand.
            rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            // set desktop mode to determine desktop mode.
            desktopMode = DetermineDesktopMode();

            // run the following statement only when not (desktop mode) and camera transform exists.
            if (!desktopMode && cameraTransform != null)
            // begin the if statement block.
            {
                // store guide as the vr controller guide component attached to this object.
                VRControllerGuide guide = GetComponent<VRControllerGuide>();
                // run the following statement only when guide is missing.
                if (guide == null)
                    // set guide to a newly attached add component<vr controller guide> component on game
                    // object.
                    guide = gameObject.AddComponent<VRControllerGuide>();
                // guide: initialize using camera transform.
                guide.Initialize(cameraTransform);
            // finish the if statement block.
            }

            // the editor simulator can drive the hmd pose independently of
            // this desktop controller. keep its controller input available,
            // but leave the game view camera under this script's control so a
            // stale simulated hmd pose cannot rotate the player underground.
            // store camera pose driver as if camera transform exists, use the the tracked pose driver
            // component attached to this object component on camera transform, otherwise use no object.
            TrackedPoseDriver cameraPoseDriver = cameraTransform != null
                // if camera transform exists, use the the tracked pose driver component attached to this
                // object component on camera transform, otherwise use no object.
                ? cameraTransform.GetComponent<TrackedPoseDriver>()
                // if camera transform exists, use the the tracked pose driver component attached to this
                // object component on camera transform, otherwise use no object.
                : null;
            // run the following statement only when camera pose driver exists.
            if (cameraPoseDriver != null)
                // set whether camera pose driver is enabled to not (desktop mode).
                cameraPoseDriver.enabled = !desktopMode;

            // the imported classroom has a different desk layout from the
            // original scene. start desktop testing in the central aisle and
            // raise the origin so the camera is at standing height. a headset
            // still uses its real tracked floor height instead.
            // run the following statement only when desktop mode and xr origin exists.
            if (desktopMode && xrOrigin != null)
            // begin the if statement block.
            {
                // set desktop height to desktop spawn height.
                desktopHeight = desktopSpawnHeight;
                // set reset position x to 0.
                resetPosition.x = 0f;
                // set reset position y to desktop height.
                resetPosition.y = desktopHeight;
                // set reset position z to desktop spawn depth.
                resetPosition.z = desktopSpawnDepth;
                // set xr origin local position to reset position.
                xrOrigin.localPosition = resetPosition;
                // call set desktop camera offset: remove competing desktop height offsets and apply the
                // desktop pitch. this method leaves physical headset tracking to the headset path.
                SetDesktopCameraOffset();
            // finish the if statement block.
            }

            // the template's android hand-permission helper is useful on a
            // headset but produces a misleading warning in a desktop game
            // view where no hand subsystem exists.
            // store hand permissions as find child using transform, the text "hands permissions manager".
            Transform handPermissions = FindChild(transform, "Hands Permissions Manager");
            // run the following statement only when hand permissions exists.
            if (handPermissions != null)
                // set the active state of hand permissions game object using not (desktop mode).
                handPermissions.gameObject.SetActive(!desktopMode);

            // store modality manager as the xr input modality manager component found below this object using
            // true.
            XRInputModalityManager modalityManager = GetComponentInChildren<XRInputModalityManager>(true);
            // run the following statement only when modality manager exists.
            if (modalityManager != null)
                // keep unity's built-in controller/hand modality active in
                // the editor. disabling it hides the official controller
                // models and rays that the xr device simulator drives.
                // set whether modality manager is enabled to true.
                modalityManager.enabled = true;

            // if the official simulator is added later, keep it editor-only.
            // store device simulator as find child containing using transform, the text "xr device simulator".
            Transform deviceSimulator = FindChildContaining(transform, "XR Device Simulator");
            // run the following statement only when device simulator is missing.
            if (deviceSimulator == null)
                // set device simulator to find scene child containing using the text "xr device simulator".
                deviceSimulator = FindSceneChildContaining("XR Device Simulator");
            // run the following statement only when device simulator exists.
            if (deviceSimulator != null)
            // begin the if statement block.
            {
                // set the active state of device simulator game object using desktop mode.
                deviceSimulator.gameObject.SetActive(desktopMode);
                // run the following statement only when desktop mode.
                if (desktopMode)
                // begin the if statement block.
                {
                    // store simulator as the the xr device simulator component attached to this object
                    // component on device simulator.
                    XRDeviceSimulator simulator = deviceSimulator.GetComponent<XRDeviceSimulator>();
                    // run the following statement only when simulator exists.
                    if (simulator != null) simulatorInputOwnership = (DesktopSimulatorInputOwnership)ConfigureDesktopSimulatorInput(simulator);
                    // store camera as if camera transform exists, use the the camera component attached to
                    // this object component on camera transform, otherwise use no object.
                    Camera camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
                    // run the following statement only when camera exists.
                    if (camera != null) camera.fieldOfView = 75f;
                    // schedule nameof using select both controllers simulator after 0.35 seconds.
                    Invoke(nameof(SelectBothControllersSimulator), 0.35f);
                    // schedule nameof using force desktop start view after 2.25 seconds.
                    Invoke(nameof(ForceDesktopStartView), 2.25f);
                // finish the if statement block.
                }
            // finish the if statement block.
            }
        // finish the method awake block.
        }

        // method: update.
        // unity calls this once per frame while the component is enabled. process the current input or state
        // described below.
        private void Update()
        // begin the method update block.
        {
            // run the following statement only when xr origin is missing.
            if (xrOrigin == null)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when desktop mode.
            if (desktopMode)
                // call handle desktop input: read keyboard movement, look, height, and shortcuts. t selects
                // the left simulated controller and y selects the right. these are keyboard keys, not quest
                // face buttons.
                HandleDesktopInput();

            // call refresh table points: collect enabled desk destinations and sort them by table number for
            // consistent navigation.
            RefreshTablePoints();

            // run the following statement only when not (desktop mode).
            if (!desktopMode) HandleXRInput();
            // subtract seconds since the previous frame, ignoring the game time scale from snap turn timer.
            snapTurnTimer -= Time.unscaledDeltaTime;
            // subtract seconds since the previous frame, ignoring the game time scale from desktop teleport
            // cooldown.
            desktopTeleportCooldown -= Time.unscaledDeltaTime;
        // finish the method update block.
        }

        // method: start.
        // unity calls this before the first normal update. finish setup that depends on other components being
        // ready.
        private void Start()
        // begin the method start block.
        {
            // call ensure navigation anchors: create fallback centre and whiteboard eye-position anchors when
            // the scene does not already provide them.
            EnsureNavigationAnchors();
        // finish the method start block.
        }

        // method: ensure navigation anchors.
        // create fallback centre and whiteboard eye-position anchors when the scene does not already provide
        // them.
        private void EnsureNavigationAnchors()
        // begin the method ensure navigation anchors block.
        {
            // run the following statement only when center anchor is missing.
            if (centerAnchor == null)
            // begin the if statement block.
            {
                // store forward as if xr origin exists, use xr origin forward, otherwise use the backward unit
                // vector.
                Vector3 forward = xrOrigin != null ? xrOrigin.forward : Vector3.back;
                // set center anchor to seat anchor: find or create using transform root, the text
                // "centeranchor", a new three-dimensional vector using 0, desktop spawn height, desktop spawn
                // depth, forward.
                centerAnchor = SeatAnchor.FindOrCreate(transform.root, "CenterAnchor",
                    // supply a new three-dimensional vector using 0, desktop spawn height, desktop spawn depth
                    // to the enclosing call.
                    new Vector3(0f, desktopSpawnHeight, desktopSpawnDepth), forward);
            // finish the if statement block.
            }

            // run the following statement only when whiteboard anchor is missing.
            if (whiteboardAnchor == null)
            // begin the if statement block.
            {
                // store head position as whiteboard point plus the upward unit vector multiplied by desktop
                // spawn height.
                Vector3 headPosition = WhiteboardPoint + Vector3.up * desktopSpawnHeight;
                // set whiteboard anchor to seat anchor: find or create using transform root, the text
                // "whiteboardanchor", head position, whiteboard target minus head position.
                whiteboardAnchor = SeatAnchor.FindOrCreate(transform.root, "WhiteboardAnchor",
                    // supply head position to the enclosing call.
                    headPosition, WhiteboardTarget - headPosition);
            // finish the if statement block.
            }
        // finish the method ensure navigation anchors block.
        }

        // method: late update.
        // stabilise the desktop camera height after simulator updates without overriding real headset
        // tracking.
        private void LateUpdate()
        // begin the method late update block.
        {
            // run the following statement only when not (desktop mode) or xr origin is missing.
            if (!desktopMode || xrOrigin == null)
                // leave this method now without running the remaining statements.
                return;

            // call set desktop camera offset: remove competing desktop height offsets and apply the desktop
            // pitch. this method leaves physical headset tracking to the headset path.
            SetDesktopCameraOffset();
            // store position as xr origin position.
            Vector3 position = xrOrigin.position;
            // store camera height as if camera transform exists, use camera transform position y, otherwise
            // use position y.
            float cameraHeight = cameraTransform != null ? cameraTransform.position.y : position.y;
            // run the following statement only when the absolute value of camera height minus desktop height
            // is greater than 0.01.
            if (Mathf.Abs(cameraHeight - desktopHeight) > 0.01f)
            // begin the if statement block.
            {
                // the editor simulator may apply a tracked floor pose when
                // the game view receives focus. keep the desktop test at a
                // standing height without blocking normal x/z movement.
                // add desktop height minus camera height to position y.
                position.y += desktopHeight - cameraHeight;
                // set xr origin position to position.
                xrOrigin.position = position;
            // finish the if statement block.
            }
            // run the following statement only when desktop height is at least desktop minimum height.
            if (desktopHeight >= desktopMinimumHeight)
                // leave this method now without running the remaining statements.
                return;

            // xr origin and the editor simulator can apply their own tracking
            // pose after awake. if that pose puts the player below the floor,
            // recover automatically without interfering with normal movement.
            // set position x to 0.
            position.x = 0f;
            // set position y to desktop spawn height.
            position.y = desktopSpawnHeight;
            // set position z to desktop spawn depth.
            position.z = desktopSpawnDepth;
            // set xr origin position to position.
            xrOrigin.position = position;
            // set xr origin local rotation to reset rotation.
            xrOrigin.localRotation = resetRotation;

            // run the following statement only when camera transform exists.
            if (cameraTransform != null)
                // set camera transform local rotation to quaternion: euler using desktop pitch, 0, 0.
                cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
        // finish the method late update block.
        }

        // method: handle desktop input.
        // read keyboard movement, look, height, and shortcuts. t selects the left simulated controller and y
        // selects the right. these are keyboard keys, not quest face buttons.
        private void HandleDesktopInput()
        // begin the method handle desktop input block.
        {
            // store keyboard as keyboard current.
            Keyboard keyboard = Keyboard.current;
            // run the following statement only when keyboard is missing.
            if (keyboard == null)
                // leave this method now without running the remaining statements.
                return;

            // desktop simulator: when keyboard t is newly pressed, select simulated hand zero, which is the
            // left controller.
            if (keyboard.tKey.wasPressedThisFrame) SelectDesktopController(0);
            // desktop simulator: when keyboard y is newly pressed, select simulated hand one, which is the
            // right controller.
            if (keyboard.yKey.wasPressedThisFrame) SelectDesktopController(1);
            // desktop simulator: when escape is newly pressed, leave explicit controller posing and restore
            // the normal desktop cursor.
            if (keyboard.escapeKey.wasPressedThisFrame) SelectDesktopController(-1);

            // controller posing and navigation have distinct input owners.
            // run the following statement only when whether keyboard space key is held down or whether
            // keyboard left shift key is held down.
            if (keyboard.spaceKey.isPressed || keyboard.leftShiftKey.isPressed)
                // leave this method now without running the remaining statements.
                return;
            // store input as the zero two-dimensional vector.
            Vector2 input = Vector2.zero;
            // run the following statement only when whether keyboard w key is held down.
            if (keyboard.wKey.isPressed) input.y += 1f;
            // run the following statement only when whether keyboard s key is held down.
            if (keyboard.sKey.isPressed) input.y -= 1f;
            // run the following statement only when whether keyboard d key is held down.
            if (keyboard.dKey.isPressed) input.x += 1f;
            // run the following statement only when whether keyboard a key is held down.
            if (keyboard.aKey.isPressed) input.x -= 1f;

            // run the following statement only when the squared length of input is greater than 1.
            if (input.sqrMagnitude > 1f)
                // make input a unit-length direction.
                input.Normalize();

            // store speed as if whether keyboard right shift key is held down, use fast move speed, otherwise
            // use move speed.
            float speed = keyboard.rightShiftKey.isPressed
                // if whether keyboard right shift key is held down, use fast move speed, otherwise use move
                // speed.
                ? fastMoveSpeed
                // if whether keyboard right shift key is held down, use fast move speed, otherwise use move
                // speed.
                : moveSpeed;
            // move in view direction using input, speed multiplied by seconds since the previous frame,
            // ignoring the game time scale.
            MoveInViewDirection(input, speed * Time.unscaledDeltaTime);

            // store vertical as 0.
            float vertical = 0f;
            // space is reserved by the official xr device simulator for
            // manipulating the right virtual controller with the mouse.
            // use page up/page down for desktop height so controller aiming
            // does not unexpectedly lift the player.
            // run the following statement only when whether keyboard page up key is held down.
            if (keyboard.pageUpKey.isPressed)
                // add 1 to vertical.
                vertical += 1f;
            // run the following statement only when whether keyboard page down key is held down.
            if (keyboard.pageDownKey.isPressed)
                // subtract 1 from vertical.
                vertical -= 1f;
            // run the following statement only when the absolute value of vertical is greater than 0.
            if (Mathf.Abs(vertical) > 0f)
            // begin the if statement block.
            {
                // store position as xr origin position.
                Vector3 position = xrOrigin.position;
                // store previous height as desktop height.
                float previousHeight = desktopHeight;
                // set desktop height to desktop height plus vertical multiplied by desktop vertical move speed
                // multiplied by seconds since the previous frame, ignoring the game time scale clamped between
                // desktop minimum height and desktop maximum height.
                desktopHeight = Mathf.Clamp(
                    // supply desktop height plus vertical multiplied by desktop vertical move speed multiplied
                    // by seconds since the previous frame, ignoring the game time scale to the enclosing call.
                    desktopHeight + vertical * desktopVerticalMoveSpeed * Time.unscaledDeltaTime,
                    // supply desktop minimum height to the enclosing call.
                    desktopMinimumHeight,
                    // supply desktop maximum height to the enclosing call.
                    desktopMaximumHeight);
                // add desktop height minus previous height to position y.
                position.y += desktopHeight - previousHeight;
                // set xr origin position to position.
                xrOrigin.position = position;
            // finish the if statement block.
            }

            // store turn as 0.
            float turn = 0f;
            // run the following statement only when whether keyboard left arrow key is held down or whether
            // keyboard q key is held down.
            if (keyboard.leftArrowKey.isPressed || keyboard.qKey.isPressed) turn -= 1f;
            // run the following statement only when whether keyboard right arrow key is held down or whether
            // keyboard e key is held down.
            if (keyboard.rightArrowKey.isPressed || keyboard.eKey.isPressed) turn += 1f;
            // run the following statement only when the absolute value of turn is greater than 0.
            if (Mathf.Abs(turn) > 0f)
                // turn using turn multiplied by 90 multiplied by seconds since the previous frame, ignoring
                // the game time scale.
                Turn(turn * 90f * Time.unscaledDeltaTime);

            // store mouse as mouse current.
            Mouse mouse = Mouse.current;
            // run the following statement only when mouse exists and not (is desktop controller mode).
            if (mouse != null && !IsDesktopControllerMode)
            // begin the if statement block.
            {
                // run the following statement only when whether mouse right button is held down.
                if (mouse.rightButton.isPressed)
                // begin the if statement block.
                {
                    // store delta as the current mouse.delta.read value input value.
                    Vector2 delta = mouse.delta.ReadValue();
                    // turn using delta x multiplied by desktop look sensitivity.
                    Turn(delta.x * desktopLookSensitivity);
                    // set desktop pitch to desktop pitch minus delta y multiplied by desktop look sensitivity
                    // clamped between the negative of 75 and 75.
                    desktopPitch = Mathf.Clamp(desktopPitch - delta.y * desktopLookSensitivity, -75f, 75f);
                    // run the following statement only when camera transform exists.
                    if (cameraTransform != null)
                        // set camera transform local rotation to quaternion: euler using desktop pitch, 0, 0.
                        cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);
                // finish the if statement block.
                }

                // store wheel as the current mouse.scroll.read value input value y.
                float wheel = mouse.scroll.ReadValue().y;
                // run the following statement only when the absolute value of wheel is greater than 0.01.
                if (Mathf.Abs(wheel) > 0.01f)
                    // move in view direction using the vector two up value multiplied by mathf: sign using
                    // wheel, the absolute value of wheel multiplied by desktop wheel move scale.
                    MoveInViewDirection(Vector2.up * Mathf.Sign(wheel), Mathf.Abs(wheel) * desktopWheelMoveScale);

            // finish the if statement block.
            }

            // run the following statement only when whether keyboard r key was newly pressed this frame.
            if (keyboard.rKey.wasPressedThisFrame)
                // call reset view: return the camera to the centre anchor.
                ResetView();

            // run the following statement only when whether keyboard j key was newly pressed this frame.
            if (keyboard.jKey.wasPressedThisFrame)
                // call stand from seat: restore standing eye height and remove desktop seated tilt without
                // forcing a physical headset rotation.
                StandFromSeat();

            // handle desktop teleport shortcuts using keyboard.
            HandleDesktopTeleportShortcuts(keyboard);
        // finish the method handle desktop input block.
        }

        // method: handle desktop teleport shortcuts.
        // map desktop number keys to the whiteboard and the available table destinations.
        private void HandleDesktopTeleportShortcuts(Keyboard keyboard)
        // begin the method handle desktop teleport shortcuts block.
        {
            // run the following statement only when whether keyboard digit1 key was newly pressed this frame.
            if (keyboard.digit1Key.wasPressedThisFrame)
            // begin the if statement block.
            {
                // call navigate to whiteboard: choose a position on the classroom side of the board at a
                // readable standing height, then face the board.
                NavigateToWhiteboard();
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when whether keyboard digit2 key was newly pressed this frame.
            if (keyboard.digit2Key.wasPressedThisFrame)
                // try teleport to table using 0.
                TryTeleportToTable(0);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit3Key.wasPressedThisFrame)
                // try teleport to table using 1.
                TryTeleportToTable(1);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit4Key.wasPressedThisFrame)
                // try teleport to table using 2.
                TryTeleportToTable(2);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit5Key.wasPressedThisFrame)
                // try teleport to table using 3.
                TryTeleportToTable(3);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit6Key.wasPressedThisFrame)
                // try teleport to table using 4.
                TryTeleportToTable(4);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit7Key.wasPressedThisFrame)
                // try teleport to table using 5.
                TryTeleportToTable(5);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit8Key.wasPressedThisFrame)
                // try teleport to table using 6.
                TryTeleportToTable(6);
            // handle the alternative when the preceding condition is false.
            else if (keyboard.digit9Key.wasPressedThisFrame)
                // try teleport to table using 7.
                TryTeleportToTable(7);
        // finish the method handle desktop teleport shortcuts block.
        }

        // method: refresh table points.
        // collect enabled desk destinations and sort them by table number for consistent navigation.
        private void RefreshTablePoints()
        // begin the method refresh table points block.
        {
            // store active points as study table teleport point points.
            IReadOnlyList<StudyTableTeleportPoint> activePoints = StudyTableTeleportPoint.Points;
            // store unchanged as the number of items in table points equals the number of items in active
            // points.
            bool unchanged = tablePoints.Count == activePoints.Count;
            // repeat this loop while unchanged and i is less than the number of items in table points.
            // increase i by one after each pass.
            for (int i = 0; unchanged && i < tablePoints.Count; i++)
                // set unchanged to the entry at i in table points exists and the entry at i in table points is
                // active and enabled.
                unchanged = tablePoints[i] != null && tablePoints[i].isActiveAndEnabled;
            // run the following statement only when unchanged.
            if (unchanged)
                // leave this method now without running the remaining statements.
                return;

            // empty table points.
            tablePoints.Clear();
            // repeat this loop while i is less than the number of items in active points. increase i by one
            // after each pass.
            for (int i = 0; i < activePoints.Count; i++)
            // begin the for statement block.
            {
                // run the following statement only when the entry at i in active points exists.
                if (activePoints[i] != null)
                    // append the entry at i in active points to table points.
                    tablePoints.Add(activePoints[i]);
            // finish the for statement block.
            }
            // table points: sort using a callback that left table index: compare to using right table index.
            tablePoints.Sort((left, right) => left.TableIndex.CompareTo(right.TableIndex));
        // finish the method refresh table points block.
        }

        // method: try teleport to table.
        // use the requested table camera anchor, or an older fallback destination if no runtime anchor is
        // available.
        private void TryTeleportToTable(int index)
        // begin the method try teleport to table block.
        {
            // call refresh table points: collect enabled desk destinations and sort them by table number for
            // consistent navigation.
            RefreshTablePoints();
            // run the following statement only when index is at least 0 and index is less than the number of
            // items in table points.
            if (index >= 0 && index < tablePoints.Count)
            // begin the if statement block.
            {
                // store table as the entry at index in table points.
                StudyTableTeleportPoint table = tablePoints[index];
                // navigate to anchor using table seat anchor, true, get table view pitch using table.
                NavigateToAnchor(table.SeatAnchor, true, GetTableViewPitch(table));
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // keep the original fallback points available if a damaged or
            // older scene has not created runtime table anchors yet.
            // run the following statement only when index is at least 0 and index is less than the number of
            // items in student points.
            if (index >= 0 && index < StudentPoints.Length)
                // teleport desktop using the entry at index in student points, the entry at index in student
                // targets, desktop seated height, true.
                TeleportDesktop(StudentPoints[index], StudentTargets[index], desktopSeatedHeight, true);
        // finish the method try teleport to table block.
        }

        // method: get table view pitch.
        // return the desktop view tilt used to include the paper and teaching board. real headset pitch
        // remains controlled by the user.
        private float GetTableViewPitch(StudyTableTeleportPoint table)
        // begin the method get table view pitch block.
        {
            // the desktop overview includes both the page and teacher's board.
            // real hmd pitch remains under the user's control.
            // return 25 to the caller.
            return 25f;
        // finish the method get table view pitch block.
        }

        // method: teleport desktop.
        // build the requested camera position and facing direction before using the shared navigation path.
        private void TeleportDesktop(Vector3 point, Vector3 target, float targetHeight, bool seated, float? seatedPitchOverride = null)
        // begin the method teleport desktop block.
        {
            // store position as point.
            Vector3 position = point;
            // set position y to target height.
            position.y = targetHeight;
            // navigate to camera pose using position, target minus position, seated, seated pitch override or,
            // if missing, desktop seated pitch.
            NavigateToCameraPose(position, target - position, seated, seatedPitchOverride ?? desktopSeatedPitch);
        // finish the method teleport desktop block.
        }

        // method: navigate to table.
        // convert a visible table number into the zero-based index used internally.
        public void NavigateToTable(int tableNumber)
        // begin the method navigate to table block.
        {
            // try teleport to table using table number minus 1.
            TryTeleportToTable(tableNumber - 1);
        // finish the method navigate to table block.
        }

        // method: navigate to whiteboard.
        // choose a position on the classroom side of the board at a readable standing height, then face the
        // board.
        public void NavigateToWhiteboard()
        // begin the method navigate to whiteboard block.
        {
            // store board as find first object by type<whiteboard canvas>.
            WhiteboardCanvas board = FindFirstObjectByType<WhiteboardCanvas>();
            // store board renderer as if board exists, use the the renderer component attached to this object
            // component on board, otherwise use no object.
            Renderer boardRenderer = board != null ? board.GetComponent<Renderer>() : null;
            // run the following statement only when board renderer exists.
            if (boardRenderer != null)
            // begin the if statement block.
            {
                // store classroom side as board transform forward projected onto the plane perpendicular to
                // the upward unit vector.
                Vector3 classroomSide = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up);
                // store toward room center as the negative of board renderer bounds center projected onto the
                // plane perpendicular to the upward unit vector.
                Vector3 towardRoomCenter = Vector3.ProjectOnPlane(-boardRenderer.bounds.center, Vector3.up);
                // run the following statement only when the squared length of classroom side is less than
                // 0.001.
                if (classroomSide.sqrMagnitude < 0.001f)
                    // set classroom side to toward room center.
                    classroomSide = towardRoomCenter;
                // run the following statement only when the squared length of toward room center is greater
                // than 0.001 and the dot product of classroom side and toward room center is less than 0.
                if (towardRoomCenter.sqrMagnitude > 0.001f
                    // the squared length of toward room center is greater than 0.001 and the dot product of
                    // classroom side and toward room center is less than 0.
                    && Vector3.Dot(classroomSide, towardRoomCenter) < 0f)
                    // set classroom side to the negative of classroom side.
                    classroomSide = -classroomSide;

                // store camera position as calculate whiteboard camera position using board renderer bounds,
                // classroom side.
                Vector3 cameraPosition = CalculateWhiteboardCameraPosition(boardRenderer.bounds, classroomSide);
                // navigate to camera pose using camera position, board renderer bounds center minus camera
                // position, false, 0.
                NavigateToCameraPose(cameraPosition, boardRenderer.bounds.center - cameraPosition, false, 0f);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // call ensure navigation anchors: create fallback centre and whiteboard eye-position anchors when
            // the scene does not already provide them.
            EnsureNavigationAnchors();
            // navigate to anchor using whiteboard anchor, false.
            NavigateToAnchor(whiteboardAnchor, false);
        // finish the method navigate to whiteboard block.
        }

        // method: calculate whiteboard camera position.
        // place the eyes about one point seven five metres in front of the board and clamp eye height to the
        // standing range.
        public static Vector3 CalculateWhiteboardCameraPosition(Bounds boardBounds, Vector3 classroomSide)
        // begin the method calculate whiteboard camera position block.
        {
            // store normal as classroom side projected onto the plane perpendicular to the upward unit vector.
            Vector3 normal = Vector3.ProjectOnPlane(classroomSide, Vector3.up);
            // run the following statement only when the squared length of normal is less than 0.001.
            if (normal.sqrMagnitude < 0.001f)
                // set normal to the forward unit vector.
                normal = Vector3.forward;
            // make normal a unit-length direction.
            normal.Normalize();

            // store position as board bounds center plus normal multiplied by 1.75.
            Vector3 position = boardBounds.center + normal * 1.75f;
            // align the tracked eyes to a stable adult standing height. this
            // avoids inheriting a low guardian/floor offset and keeps the full
            // board and its tool panel visible and reachable.
            // set position y to board bounds center y clamped between 1.55 and 1.75.
            position.y = Mathf.Clamp(boardBounds.center.y, 1.55f, 1.75f);
            // return position to the caller.
            return position;
        // finish the method calculate whiteboard camera position block.
        }

        // method: navigate to anchor.
        // request the camera position and horizontal facing direction stored in a seat anchor.
        public void NavigateToAnchor(SeatAnchor anchor, bool seated = false, float desktopViewPitch = 0f)
        // begin the method navigate to anchor block.
        {
            // run the following statement only when anchor exists.
            if (anchor != null)
                // navigate to camera pose using anchor camera position, anchor facing direction, seated,
                // desktop view pitch.
                NavigateToCameraPose(anchor.CameraPosition, anchor.FacingDirection, seated, desktopViewPitch);
        // finish the method navigate to anchor block.
        }

        // method: navigate to camera pose.
        // apply the teleport cooldown, prepare desktop view settings if needed, and align the camera through
        // the rig.
        private void NavigateToCameraPose(Vector3 cameraPosition, Vector3 facingDirection, bool seated, float desktopViewPitch)
        // begin the method navigate to camera pose block.
        {
            // run the following statement only when xr origin is missing or camera transform is missing or
            // desktop teleport cooldown is greater than 0.
            if (xrOrigin == null || cameraTransform == null || desktopTeleportCooldown > 0f)
                // leave this method now without running the remaining statements.
                return;

            // set desktop teleport cooldown to 0.18.
            desktopTeleportCooldown = 0.18f;
            // set seated desktop view to seated.
            seatedDesktopView = seated;
            // run the following statement only when desktop mode.
            if (desktopMode)
            // begin the if statement block.
            {
                // store desktop camera as the the camera component attached to this object component on camera
                // transform.
                Camera desktopCamera = cameraTransform.GetComponent<Camera>();
                // run the following statement only when desktop camera exists.
                if (desktopCamera != null) desktopCamera.fieldOfView = seated ? 100f : 75f;
                // set desktop pitch to if seated, use desktop view pitch, otherwise use 0.
                desktopPitch = seated ? desktopViewPitch : 0f;
                // call set desktop camera offset: remove competing desktop height offsets and apply the
                // desktop pitch. this method leaves physical headset tracking to the headset path.
                SetDesktopCameraOffset();
                // set desktop height to camera position y.
                desktopHeight = cameraPosition.y;
            // finish the if statement block.
            }

            // align camera to pose using xr origin, camera transform, camera position, facing direction.
            AlignCameraToPose(xrOrigin, cameraTransform, cameraPosition, facingDirection);
        // finish the method navigate to camera pose block.
        }

        // aligns the actual camera, including any tracked room-scale offset.
        // method: align camera to pose.
        // turn the rig around the actual camera and then translate it so tracked eyes reach the destination.
        // preserve headset pitch, roll, and local tracking pose.
        public static void AlignCameraToPose(Transform origin, Transform camera, Vector3 cameraPosition, Vector3 facingDirection)
        // begin the method align camera to pose block.
        {
            // run the following statement only when origin is missing or camera is missing.
            if (origin == null || camera == null)
                // leave this method now without running the remaining statements.
                return;

            // store current forward as camera forward projected onto the plane perpendicular to the upward
            // unit vector.
            Vector3 currentForward = Vector3.ProjectOnPlane(camera.forward, Vector3.up);
            // store target forward as facing direction projected onto the plane perpendicular to the upward
            // unit vector.
            Vector3 targetForward = Vector3.ProjectOnPlane(facingDirection, Vector3.up);
            // run the following statement only when the squared length of current forward is less than 0.001.
            if (currentForward.sqrMagnitude < 0.001f)
                // set current forward to origin forward projected onto the plane perpendicular to the upward
                // unit vector.
                currentForward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
            // run the following statement only when the squared length of current forward is greater than
            // 0.001 and the squared length of target forward is greater than 0.001.
            if (currentForward.sqrMagnitude > 0.001f && targetForward.sqrMagnitude > 0.001f)
            // begin the if statement block.
            {
                // store yaw delta as three-dimensional vector: signed angle using current forward, target
                // forward, the upward unit vector.
                float yawDelta = Vector3.SignedAngle(currentForward, targetForward, Vector3.up);
                // rotate origin around camera position using the supplied axis and angle.
                origin.RotateAround(camera.position, Vector3.up, yawDelta);
            // finish the if statement block.
            }

            // translate after yaw so camera x/y/z exactly match the anchor.
            // do not overwrite tracked camera pitch, roll, local pose or rig scale.
            // add camera position minus camera position to origin position.
            origin.position += cameraPosition - camera.position;
        // finish the method align camera to pose block.
        }

        // method: stand from seat.
        // restore standing eye height and remove desktop seated tilt without forcing a physical headset
        // rotation.
        private void StandFromSeat()
        // begin the method stand from seat block.
        {
            // run the following statement only when xr origin is missing or camera transform is missing.
            if (xrOrigin == null || cameraTransform == null)
                // leave this method now without running the remaining statements.
                return;

            // set seated desktop view to false.
            seatedDesktopView = false;
            // set desktop height to desktop spawn height.
            desktopHeight = desktopSpawnHeight;
            // run the following statement only when desktop mode.
            if (desktopMode)
            // begin the if statement block.
            {
                // set desktop pitch to 0.
                desktopPitch = 0f;
                // call set desktop camera offset: remove competing desktop height offsets and apply the
                // desktop pitch. this method leaves physical headset tracking to the headset path.
                SetDesktopCameraOffset();
            // finish the if statement block.
            }
            // add the upward unit vector multiplied by desktop spawn height minus camera transform position y
            // to xr origin position.
            xrOrigin.position += Vector3.up * (desktopSpawnHeight - cameraTransform.position.y);
        // finish the method stand from seat block.
        }

        // method: handle xr input.
        // read left-stick movement and right-stick snap turning, then check the left-controller teleport
        // buttons.
        private void HandleXRInput()
        // begin the method handle xr input block.
        {
            // run the following statement only when not (whether left controller is a valid device).
            if (!leftController.isValid)
                // set left controller to input devices: get device at xr node using xr node left hand.
                leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            // run the following statement only when not (whether right controller is a valid device).
            if (!rightController.isValid)
                // set right controller to input devices: get device at xr node using xr node right hand.
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            // run the following statement only when whether left controller is a valid device and whether left
            // controller supplies xr common usages primary2 d axis, storing it in the output value move.
            if (leftController.isValid && leftController.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 move))
            // begin the if statement block.
            {
                // run the following statement only when the length of move is at least controller deadzone.
                if (move.magnitude >= controllerDeadzone)
                // begin the if statement block.
                {
                    // set move to two-dimensional vector: clamp magnitude using move, 1.
                    move = Vector2.ClampMagnitude(move, 1f);
                    // move in view direction using move, move speed multiplied by seconds since the previous
                    // frame, ignoring the game time scale.
                    MoveInViewDirection(move, moveSpeed * Time.unscaledDeltaTime);
                // finish the if statement block.
                }
            // finish the if statement block.
            }

            // run the following statement only when whether right controller is a valid device and whether
            // right controller supplies xr common usages primary2 d axis, storing it in the output value turn
            // axis and the absolute value of turn axis x is at least 0.7 and snap turn timer is at most 0.
            if (rightController.isValid
                // whether right controller is a valid device and whether right controller supplies xr common
                // usages primary2 d axis, storing it in the output value turn axis.
                && rightController.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 turnAxis)
                // whether right controller is a valid device and whether right controller supplies xr common
                // usages primary2 d axis, storing it in the output value turn axis and the absolute value of
                // turn axis x is at least 0.7.
                && Mathf.Abs(turnAxis.x) >= 0.7f
                // whether right controller is a valid device and whether right controller supplies xr common
                // usages primary2 d axis, storing it in the output value turn axis and the absolute value of
                // turn axis x is at least 0.7 and snap turn timer is at most 0.
                && snapTurnTimer <= 0f)
            // begin the if statement block.
            {
                // turn using mathf: sign using turn axis x multiplied by snap turn degrees.
                Turn(Mathf.Sign(turnAxis.x) * snapTurnDegrees);
                // set snap turn timer to snap turn cooldown.
                snapTurnTimer = snapTurnCooldown;
            // finish the if statement block.
            }

            // call handle quest navigation buttons: left x goes to the whiteboard and left y advances to the
            // next table. compare the current and previous button state so one press causes one action.
            HandleQuestNavigationButtons();
        // finish the method handle xr input block.
        }

        // method: handle quest navigation buttons.
        // left x goes to the whiteboard and left y advances to the next table. compare the current and
        // previous button state so one press causes one action.
        private void HandleQuestNavigationButtons()
        // begin the method handle quest navigation buttons block.
        {
            // store left primary as read button using left controller, xr common usages primary button.
            bool leftPrimary = ReadButton(leftController, XRCommonUsages.primaryButton);
            // store left secondary as read button using left controller, xr common usages secondary button.
            bool leftSecondary = ReadButton(leftController, XRCommonUsages.secondaryButton);

            // x is a reliable whiteboard shortcut. y advances through every
            // table. keep the right controller free for tools and drawing.
            // run the following statement only when left primary and not (left primary was pressed).
            if (leftPrimary && !leftPrimaryWasPressed)
                // call navigate to whiteboard: choose a position on the classroom side of the board at a
                // readable standing height, then face the board.
                NavigateToWhiteboard();
            // run the following statement only when left secondary and not (left secondary was pressed).
            if (leftSecondary && !leftSecondaryWasPressed)
                // call navigate to next table: cycle through all registered tables and wrap back to the first
                // after the final table.
                NavigateToNextTable();

            // set left primary was pressed to left primary.
            leftPrimaryWasPressed = leftPrimary;
            // set left secondary was pressed to left secondary.
            leftSecondaryWasPressed = leftSecondary;
        // finish the method handle quest navigation buttons block.
        }

        // method: navigate to next table.
        // cycle through all registered tables and wrap back to the first after the final table.
        private void NavigateToNextTable()
        // begin the method navigate to next table block.
        {
            // call refresh table points: collect enabled desk destinations and sort them by table number for
            // consistent navigation.
            RefreshTablePoints();
            // store table count as if the number of items in table points is greater than 0, use the number of
            // items in table points, otherwise use the number of items in student points.
            int tableCount = tablePoints.Count > 0 ? tablePoints.Count : StudentPoints.Length;
            // run the following statement only when table count is at most 0.
            if (tableCount <= 0)
                // leave this method now without running the remaining statements.
                return;

            // try teleport to table using next quest table index modulo table count.
            TryTeleportToTable(nextQuestTableIndex % tableCount);
            // set next quest table index to next quest table index plus 1 modulo table count.
            nextQuestTableIndex = (nextQuestTableIndex + 1) % tableCount;
        // finish the method navigate to next table block.
        }

        // method: read button.
        // read a requested button only when its controller device is valid.
        private static bool ReadButton(XRInputDevice device, InputFeatureUsage<bool> usage)
        // begin the method read button block.
        {
            // return whether device is a valid device and whether device supplies usage, storing it in the
            // output value pressed and pressed to the caller.
            return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
        // finish the method read button block.
        }

        // method: move in view direction.
        // move the entire rig along the horizontal view direction and clamp it to the room boundaries.
        private void MoveInViewDirection(Vector2 input, float distance)
        // begin the method move in view direction block.
        {
            // run the following statement only when the squared length of input is at most 0.0001.
            if (input.sqrMagnitude <= 0.0001f)
                // leave this method now without running the remaining statements.
                return;

            // store view as if camera transform exists, use camera transform, otherwise use xr origin.
            Transform view = cameraTransform != null ? cameraTransform : xrOrigin;
            // store forward as view forward.
            Vector3 forward = view.forward;
            // set forward y to 0.
            forward.y = 0f;
            // run the following statement only when the squared length of forward is less than 0.0001.
            if (forward.sqrMagnitude < 0.0001f)
                // set forward to xr origin forward.
                forward = xrOrigin.forward;
            // make forward a unit-length direction.
            forward.Normalize();

            // cross(up, forward) gives the camera's real screen-right axis.
            // this keeps d moving right and a moving left.
            // store right as the unit direction of three-dimensional vector: cross using the upward unit
            // vector, forward.
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            // store movement as forward multiplied by input y plus right multiplied by input x multiplied by
            // distance.
            Vector3 movement = (forward * input.y + right * input.x) * distance;
            // store position as xr origin position plus movement.
            Vector3 position = xrOrigin.position + movement;
            // set position x to position x clamped between minimum x and maximum x.
            position.x = Mathf.Clamp(position.x, minimumX, maximumX);
            // set position z to position z clamped between minimum z and maximum z.
            position.z = Mathf.Clamp(position.z, minimumZ, maximumZ);
            // set xr origin position to position.
            xrOrigin.position = position;
        // finish the method move in view direction block.
        }

        // method: turn.
        // rotate the rig around the camera so turning does not orbit the player around a distant rig origin.
        private void Turn(float degrees)
        // begin the method turn block.
        {
            // run the following statement only when camera transform exists.
            if (cameraTransform != null)
                // rotate xr origin around camera transform position using the supplied axis and angle.
                xrOrigin.RotateAround(cameraTransform.position, Vector3.up, degrees);
            // handle the alternative when the preceding condition is false.
            else
                // xr origin: rotate using the upward unit vector, degrees, unity engine space world.
                xrOrigin.Rotate(Vector3.up, degrees, UnityEngine.Space.World);
        // finish the method turn block.
        }

        // method: reset view.
        // return the camera to the centre anchor.
        private void ResetView()
        // begin the method reset view block.
        {
            // call ensure navigation anchors: create fallback centre and whiteboard eye-position anchors when
            // the scene does not already provide them.
            EnsureNavigationAnchors();
            // navigate to anchor using center anchor, false.
            NavigateToAnchor(centerAnchor, false);
        // finish the method reset view block.
        }

        // method: force desktop start view.
        // reapply the desktop standing destination after the simulator has had time to publish its initial
        // pose.
        private void ForceDesktopStartView()
        // begin the method force desktop start view block.
        {
            // run the following statement only when not (desktop mode) or xr origin is missing.
            if (!desktopMode || xrOrigin == null)
                // leave this method now without running the remaining statements.
                return;

            // xr device simulator can publish its initial simulated pose a
            // moment after scene awake. reapply the known standing position
            // once after that hand-off so play never begins under a desk.
            // call reset view: return the camera to the centre anchor.
            ResetView();
        // finish the method force desktop start view block.
        }

        // method: find camera.
        // locate the camera below the xr origin if no camera reference has been assigned.
        private void FindCamera()
        // begin the method find camera block.
        {
            // run the following statement only when camera transform is missing and xr origin exists.
            if (cameraTransform == null && xrOrigin != null)
            // begin the if statement block.
            {
                // store camera as the requested component in the children of xr origin.
                Camera camera = xrOrigin.GetComponentInChildren<Camera>(true);
                // run the following statement only when camera exists.
                if (camera != null)
                    // set camera transform to camera transform.
                    cameraTransform = camera.transform;
            // finish the if statement block.
            }
        // finish the method find camera block.
        }

        // method: set desktop camera offset.
        // remove competing desktop height offsets and apply the desktop pitch. this method leaves physical
        // headset tracking to the headset path.
        private void SetDesktopCameraOffset()
        // begin the method set desktop camera offset block.
        {
            // run the following statement only when not (desktop mode) or camera transform is missing or
            // camera transform parent is missing.
            if (!desktopMode || cameraTransform == null || cameraTransform.parent == null)
                // leave this method now without running the remaining statements.
                return;

            // in desktop mode the origin represents the player's standing
            // height. keeping the xri camera offset at zero prevents the
            // device simulator from applying a second, conflicting height.
            // set camera transform parent local position to the zero vector.
            cameraTransform.parent.localPosition = Vector3.zero;
            // set camera transform parent local rotation to quaternion identity.
            cameraTransform.parent.localRotation = Quaternion.identity;

            // the editor simulator can still publish a stale tracked pose
            // even with its trackedposedriver disabled. pin the camera back
            // to the standing eye point so a controller test cannot place
            // the desktop view below a desk.
            // set camera transform local position to the zero vector.
            cameraTransform.localPosition = Vector3.zero;
            // set camera transform local rotation to quaternion: euler using desktop pitch, 0, 0.
            cameraTransform.localRotation = Quaternion.Euler(desktopPitch, 0f, 0f);

            // keep the xri origin's own offset at zero as well. this prevents
            // xr simulation from adding a second camera height on top of the
            // desktop standing-height origin.
            // store origin as if xr origin exists, use the the xr origin component attached to this object
            // component on xr origin, otherwise use no object.
            XROrigin origin = xrOrigin != null ? xrOrigin.GetComponent<XROrigin>() : null;
            // run the following statement only when origin exists.
            if (origin != null)
                // set origin camera y offset to 0.
                origin.CameraYOffset = 0f;
        // finish the method set desktop camera offset block.
        }

        // method: configure desktop simulator input.
        // create the helper that separates rig keyboard movement from simulated controller posing.
        public static System.IDisposable ConfigureDesktopSimulatorInput(XRDeviceSimulator simulator)
        // begin the method configure desktop simulator input block.
        {
            // return a new desktop simulator input ownership using simulator to the caller.
            return new DesktopSimulatorInputOwnership(simulator);
        // finish the method configure desktop simulator input block.
        }

        // method: on destroy.
        // release owned resources or event listeners when unity destroys this component.
        private void OnDestroy()
        // begin the method on destroy block.
        {
            // if simulator input ownership exists, dispose.
            simulatorInputOwnership?.Dispose();
        // finish the method on destroy block.
        }

        // t selects left, y selects right; escape restores the desktop cursor.
        // method: select desktop controller.
        // choose the left or right simulated controller for explicit desktop posing. a negative selection
        // restores the ordinary desktop pointer.
        public void SelectDesktopController(int hand)
        // begin the method select desktop controller block.
        {
            // run the following statement only when not (desktop mode) or hand is less than the negative of 1
            // or hand is greater than 1.
            if (!desktopMode || hand < -1 || hand > 1)
                // leave this method now without running the remaining statements.
                return;
            // set desktop controller hand to hand.
            DesktopControllerHand = hand;
            // if simulator input ownership exists, set explicit controller mode using hand is at least 0.
            simulatorInputOwnership?.SetExplicitControllerMode(hand >= 0);
            // run the following statement only when hand is less than 0.
            if (hand < 0) return;
            // store simulator as find first object by type<xr device simulator>.
            XRDeviceSimulator simulator = FindFirstObjectByType<XRDeviceSimulator>();
            // run the following statement only when simulator exists.
            if (simulator != null)
            // begin the if statement block.
            {
                // xri 3.4 exposes read-only selection state. its ui methods
                // update presentation only, so set the simulator's desktop
                // target as well. this does not change headset tracking.
                // store target as the type information for xr device simulator: get property using the text
                // "targeteddeviceinput", binding flags instance | binding flags non public.
                PropertyInfo target = typeof(XRDeviceSimulator).GetProperty("targetedDeviceInput", BindingFlags.Instance | BindingFlags.NonPublic);
                // run the following statement only when target exists.
                if (target != null)
                    // target: set value using simulator, system enum: parse using target property type, if
                    // hand equals 0, use the text "leftdevice", otherwise use the text "rightdevice".
                    target.SetValue(simulator, System.Enum.Parse(target.PropertyType, hand == 0 ? "LeftDevice" : "RightDevice"));
            // finish the if statement block.
            }
            // store method name as if hand equals 0, use the text "onactivateleftcontroller", otherwise use
            // the text "onactivaterightcontroller".
            string methodName = hand == 0 ? "OnActivateLeftController" : "OnActivateRightController";
            // visit each behaviour in resources: find objects of type all.
            foreach (MonoBehaviour behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            // begin the for each statement block.
            {
                // run the following statement only when behaviour is missing or behaviour: get type name
                // differs from the text "xrdevicesimulatorui" or not (behaviour game object scene: is valid).
                if (behaviour == null || behaviour.GetType().Name != "XRDeviceSimulatorUI"
                    // behaviour is missing or behaviour: get type name differs from the text
                    // "xrdevicesimulatorui" or not (behaviour game object scene: is valid).
                    || !behaviour.gameObject.scene.IsValid()) continue;
                // if behaviour: get type: get method using method name, binding flags instance | binding flags
                // non public | binding flags public exists, schedule behaviour after no object seconds.
                behaviour.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(behaviour, null);
                // leave the nearest loop or switch after finding the required result.
                break;
            // finish the for each statement block.
            }
        // finish the method select desktop controller block.
        }

        // method: select both controllers simulator.
        // find the simulator interface and initialise its controller presentation for game-view testing.
        private void SelectBothControllersSimulator()
        // begin the method select both controllers simulator block.
        {
            // run the following statement only when not (desktop mode).
            if (!desktopMode)
                // leave this method now without running the remaining statements.
                return;

            // the simulator creates its ui canvas beside the simulator object
            // (often in the dontdestroyonload scene), so it is not always a
            // child of the simulator root. search loaded scene instances so
            // the editor starts in controller mode instead of falling back
            // to hmd mode after each play.
            // store behaviours as resources: find objects of type all.
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            // repeat this loop while i is less than the number of items in behaviours. increase i by one after
            // each pass.
            for (int i = 0; i < behaviours.Length; i++)
            // begin the for statement block.
            {
                // store behaviour as the entry at i in behaviours.
                MonoBehaviour behaviour = behaviours[i];
                // run the following statement only when behaviour is missing or behaviour: get type name
                // differs from the text "xrdevicesimulatorui".
                if (behaviour == null || behaviour.GetType().Name != "XRDeviceSimulatorUI")
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // run the following statement only when not (behaviour game object scene: is valid).
                if (!behaviour.gameObject.scene.IsValid())
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store activate both controllers as behaviour: get type: get method using the text
                // "onactivatebothcontrollers", binding flags instance | binding flags non public | binding
                // flags public.
                MethodInfo activateBothControllers = behaviour.GetType().GetMethod(
                    // supply the text "onactivatebothcontrollers" to the enclosing call.
                    "OnActivateBothControllers",
                    // supply binding flags instance | binding flags non public | binding flags public to the
                    // enclosing call.
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                // run the following statement only when activate both controllers exists.
                if (activateBothControllers != null)
                    // invoke activate both controllers with behaviour, no object.
                    activateBothControllers.Invoke(behaviour, null);

                // leave this method now without running the remaining statements.
                return;
            // finish the for statement block.
            }
        // finish the method select both controllers simulator block.
        }

        // method: determine desktop mode.
        // use desktop mode in the editor, headset mode on android, and device availability on other platforms.
        private bool DetermineDesktopMode()
        // begin the method determine desktop mode block.
        {
// compile the next branch only for the unity editor.
#if UNITY_EDITOR
            // keep the unity game view deterministic even when xr simulation
            // exposes virtual controller devices in the editor. a physical
            // headset uses the android build path below.
            // return true to the caller.
            return true;
// otherwise compile this branch for an android player such as the quest build.
#elif UNITY_ANDROID
            // return false to the caller.
            return false;
// compile the following fallback for other platforms.
#else
            // return not (xr settings is device active) to the caller.
            return !XRSettings.isDeviceActive;
// finish the platform-specific compilation branches.
#endif
        // finish the method determine desktop mode block.
        }

        // method: find child.
        // search descendants for an exact object name so setup can locate an existing scene object.
        private static Transform FindChild(Transform root, string childName)
        // begin the method find child block.
        {
            // run the following statement only when root is missing.
            if (root == null)
                // return no object to the caller.
                return null;
            // run the following statement only when root name equals child name.
            if (root.name == childName)
                // return root to the caller.
                return root;

            // repeat this loop while i is less than root child count. increase i by one after each pass.
            for (int i = 0; i < root.childCount; i++)
            // begin the for statement block.
            {
                // store found as find child using root: get child using i, child name.
                Transform found = FindChild(root.GetChild(i), childName);
                // run the following statement only when found exists.
                if (found != null)
                    // return found to the caller.
                    return found;
            // finish the for statement block.
            }

            // return no object to the caller.
            return null;
        // finish the method find child block.
        }

        // method: find child containing.
        // search descendants for an object whose name contains the requested text.
        private static Transform FindChildContaining(Transform root, string text)
        // begin the method find child containing block.
        {
            // run the following statement only when root is missing.
            if (root == null)
                // return no object to the caller.
                return null;
            // run the following statement only when root name: index of using text, system string comparison
            // ordinal ignore case is at least 0.
            if (root.name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0)
                // return root to the caller.
                return root;

            // repeat this loop while i is less than root child count. increase i by one after each pass.
            for (int i = 0; i < root.childCount; i++)
            // begin the for statement block.
            {
                // store found as find child containing using root: get child using i, text.
                Transform found = FindChildContaining(root.GetChild(i), text);
                // run the following statement only when found exists.
                if (found != null)
                    // return found to the caller.
                    return found;
            // finish the for statement block.
            }

            // return no object to the caller.
            return null;
        // finish the method find child containing block.
        }

        // method: find scene child containing.
        // search loaded scene roots for a named simulator object that may not be below this rig.
        private static Transform FindSceneChildContaining(string text)
        // begin the method find scene child containing block.
        {
            // store scene as scene manager: get active scene.
            Scene scene = SceneManager.GetActiveScene();
            // run the following statement only when not (scene: is valid).
            if (!scene.IsValid())
                // return no object to the caller.
                return null;

            // store roots as scene: get root game objects.
            GameObject[] roots = scene.GetRootGameObjects();
            // repeat this loop while i is less than the number of items in roots. increase i by one after each
            // pass.
            for (int i = 0; i < roots.Length; i++)
            // begin the for statement block.
            {
                // store found as find child containing using the entry at i in roots transform, text.
                Transform found = FindChildContaining(roots[i].transform, text);
                // run the following statement only when found exists.
                if (found != null)
                    // return found to the caller.
                    return found;
            // finish the for statement block.
            }

            // return no object to the caller.
            return null;
        // finish the method find scene child containing block.
        }

        // method: on gui.
        // handle unity game-view interface events. this desktop path is separate from the headset world-space
        // controls.
        private void OnGUI()
        // begin the method on gui block.
        {
            // run the following statement only when not (is desktop mode).
            if (!IsDesktopMode)
                // leave this method now without running the remaining statements.
                return;

            // release before gui.button can consume the event, including
            // clicks on the collapsible help control.
            // run the following statement only when event current raw type equals event type mouse up and
            // event current button equals 0.
            if (Event.current.rawType == EventType.MouseUp && Event.current.button == 0)
                // set desktop navigation pointer held to false.
                desktopNavigationPointerHeld = false;

            // imgui receives keyboard events even when the new input system
            // has not yet re-established game-view focus. keep the same
            // shortcuts available through this path so chair/board changes
            // cannot silently fail after clicking the simulator panel.
            // run the following statement only when event current type equals event type key down and event
            // current is key.
            if (Event.current.type == EventType.KeyDown && Event.current.isKey)
            // begin the if statement block.
            {
                // choose a branch using event current key code.
                switch (Event.current.keyCode)
                // begin the switch statement block.
                {
                    // handle the key code alpha1 choice.
                    case KeyCode.Alpha1:
                        // call navigate to whiteboard: choose a position on the classroom side of the board at
                        // a readable standing height, then face the board.
                        NavigateToWhiteboard();
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha2 choice.
                    case KeyCode.Alpha2:
                        // try teleport to table using 0.
                        TryTeleportToTable(0);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha3 choice.
                    case KeyCode.Alpha3:
                        // try teleport to table using 1.
                        TryTeleportToTable(1);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha4 choice.
                    case KeyCode.Alpha4:
                        // try teleport to table using 2.
                        TryTeleportToTable(2);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha5 choice.
                    case KeyCode.Alpha5:
                        // try teleport to table using 3.
                        TryTeleportToTable(3);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha6 choice.
                    case KeyCode.Alpha6:
                        // try teleport to table using 4.
                        TryTeleportToTable(4);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha7 choice.
                    case KeyCode.Alpha7:
                        // try teleport to table using 5.
                        TryTeleportToTable(5);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha8 choice.
                    case KeyCode.Alpha8:
                        // try teleport to table using 6.
                        TryTeleportToTable(6);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code alpha9 choice.
                    case KeyCode.Alpha9:
                        // try teleport to table using 7.
                        TryTeleportToTable(7);
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code j choice.
                    case KeyCode.J:
                        // call stand from seat: restore standing eye height and remove desktop seated tilt
                        // without forcing a physical headset rotation.
                        StandFromSeat();
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the key code c choice.
                    case KeyCode.C:
                        // call center view: return to the centre and also reset simulated controllers in
                        // desktop mode.
                        CenterView();
                        // event current: use.
                        Event.current.Use();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                // finish the switch statement block.
                }
            // finish the if statement block.
            }

            // store help button as a new rect using 16, 16, 130, 28.
            Rect helpButton = new Rect(16f, 16f, 130f, 28f);
            // store help panel as a new rect using 16, 50, 510, 165.
            Rect helpPanel = new Rect(16f, 50f, 510f, 165f);
            // run the following statement only when event current type equals event type mouse down and event
            // current button equals 0 and whether help button contains event current mouse position or desktop
            // help expanded and whether help panel contains event current mouse position.
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                // event current type equals event type mouse down and event current button equals 0 and
                // whether help button contains event current mouse position or desktop help expanded and
                // whether help panel contains event current mouse position.
                && (helpButton.Contains(Event.current.mousePosition)
                    // whether help button contains event current mouse position or desktop help expanded and
                    // whether help panel contains event current mouse position.
                    || (desktopHelpExpanded && helpPanel.Contains(Event.current.mousePosition))))
                // set desktop navigation pointer held to true.
                desktopNavigationPointerHeld = true;
            // run the following statement only when gui: button using help button, if desktop help expanded,
            // use the text "close help", otherwise use the text "controls ?".
            if (GUI.Button(helpButton, desktopHelpExpanded ? "CLOSE HELP" : "CONTROLS  ?"))
                // set desktop help expanded to not (desktop help expanded).
                desktopHelpExpanded = !desktopHelpExpanded;
            // run the following statement only when desktop help expanded.
            if (desktopHelpExpanded)
            // begin the if statement block.
            {
                // gui: box using help panel, the text "desktop controls / / wasd: move q / e: turn right-drag:
                // look / scroll: closer / farther page up / down: height / left mouse: draw / select right
                // mouse: erase / 1: whiteboard 2-9: tables j: stand c: center / t: left controller y: right
                // controller esc: cursor / headset: use controller rays to select tools and draw".
                GUI.Box(helpPanel,
                    // supply the text "desktop controls / / wasd: move q / e: turn right-drag: look / scroll:
                    // closer / farther page up / down: height / left mouse: draw / select right mouse: erase /
                    // 1: whiteboard 2-9: tables j: stand c: center / t: left controller y: right controller
                    // esc: cursor / headset: use controller rays to select tools and draw" to the enclosing
                    // call.
                    "DESKTOP CONTROLS\n\nWASD: move   Q / E: turn   Right-drag: look\nScroll: closer / farther   Page Up / Down: height\nLeft mouse: draw / select   Right mouse: erase\n1: whiteboard   2-9: tables   J: stand   C: center\nT: left controller   Y: right controller   Esc: cursor\nHeadset: use controller rays to select tools and draw");
            // finish the if statement block.
            }

            // run the following statement only when is desktop controller mode.
            if (IsDesktopControllerMode)
                // gui: label using a new rect using 160, 16, 580, 28, if desktop controller hand equals 0, use
                // the text "left", otherwise use the text "right" plus the text " controller | mouse: aim
                // click: trigger g: grab esc: cursor".
                GUI.Label(new Rect(160f, 16f, 580f, 28f),
                    // supply if desktop controller hand equals 0, use the text "left", otherwise use the text
                    // "right" plus the text " controller | mouse: aim click: trigger g: grab esc: cursor" to
                    // the enclosing call.
                    (DesktopControllerHand == 0 ? "LEFT" : "RIGHT") + " CONTROLLER  |  Mouse: aim  Click: trigger  G: grab  Esc: cursor");

            // clickable fallbacks are deliberately visible in the game view.
            // they are especially useful when the simulator panel currently
            // owns keyboard focus, and they make the intended test route
            // obvious: sit, stand, walk to the board, and return to a desk.
            // keep navigation below the classroom view and above the
            // collapsed simulator footer, away from the paper and board.
            // call refresh table points: collect enabled desk destinations and sort them by table number for
            // consistent navigation.
            RefreshTablePoints();
            // store table count as if the number of items in table points is greater than 0, use the number of
            // items in table points, otherwise use the number of items in student points.
            int tableCount = tablePoints.Count > 0 ? tablePoints.Count : StudentPoints.Length;
            // store columns as if table count is greater than 9, use 4, otherwise use 3.
            int columns = tableCount > 9 ? 4 : 3;
            // store button width as if table count is greater than 9, use 84, otherwise use 108.
            float buttonWidth = tableCount > 9 ? 84f : 108f;
            // store gap as if table count is greater than 9, use 5, otherwise use 6.
            float gap = tableCount > 9 ? 5f : 6f;
            // store navigation width as columns multiplied by button width plus columns minus 1 multiplied by
            // gap.
            float navigationWidth = columns * buttonWidth + (columns - 1) * gap;
            // store button x as 16.
            float buttonX = 16f;
            // store table rows as table count divided by columns converted to float rounded upward to an
            // integer.
            int tableRows = Mathf.CeilToInt(tableCount / (float)columns);
            // store navigation height as 32 plus gap plus table rows multiplied by 32 plus the larger of 0 and
            // table rows minus 1 multiplied by gap.
            float navigationHeight = 32f + gap + tableRows * 32f + Mathf.Max(0, tableRows - 1) * gap;
            // anchor the complete navigation pad to the bottom edge. the old
            // fixed y-position placed the last table row below short game
            // views, so table 7-9 were visibly cut off.
            // store button y as the larger of 16 and screen height minus navigation height minus 64.
            float buttonY = Mathf.Max(16f, Screen.height - navigationHeight - 64f);
            // the table grid becomes four columns in the imported room. give
            // the three action buttons their own wider row so whiteboard,
            // stand / jump, and center view remain readable instead of being
            // clipped to the table-cell width.
            // store action gap as gap.
            float actionGap = gap;
            // store action width as navigation width minus 2 multiplied by action gap divided by 3.
            float actionWidth = (navigationWidth - 2f * actionGap) / 3f;

            // desktop navigation is drawn with imgui rather than the
            // world-space canvases. latch its pointer here so a click on a
            // teleport/table button cannot also be interpreted as a drawing
            // press when the docked game view reports a different coordinate
            // space to the input system.
            // run the following statement only when event current type equals event type mouse down and event
            // current button equals 0 and is navigation pointer inside using event current mouse position,
            // table count, columns, button x, button y, button width, gap, action width.
            if (Event.current.type == EventType.MouseDown
                // event current type equals event type mouse down and event current button equals 0.
                && Event.current.button == 0
                // event current type equals event type mouse down and event current button equals 0 and is
                // navigation pointer inside using event current mouse position, table count, columns, button
                // x, button y, button width, gap, action width.
                && IsNavigationPointerInside(
                    // supply event current mouse position to the enclosing call.
                    Event.current.mousePosition,
                    // supply table count to the enclosing call.
                    tableCount,
                    // supply columns to the enclosing call.
                    columns,
                    // supply button x to the enclosing call.
                    buttonX,
                    // supply button y to the enclosing call.
                    buttonY,
                    // supply button width to the enclosing call.
                    buttonWidth,
                    // supply gap to the enclosing call.
                    gap,
                    // supply action width to the enclosing call.
                    actionWidth))
            // begin the if statement block.
            {
                // set desktop navigation pointer held to true.
                desktopNavigationPointerHeld = true;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (Event.current.type == EventType.MouseUp
                // event current type equals event type mouse up and event current button equals 0.
                && Event.current.button == 0)
            // begin the if statement block.
            {
                // set desktop navigation pointer held to false.
                desktopNavigationPointerHeld = false;
            // finish the if statement block.
            }

            // let gui.button process mouseup itself so it releases hotcontrol.
            // consuming that event in a second click handler swallowed the
            // first drawing drag after every navigation button press.
            // run the following statement only when gui: button using a new rect using button x, button y,
            // action width, 32, the text "whiteboard".
            if (GUI.Button(new Rect(buttonX, buttonY, actionWidth, 32f), "WHITEBOARD"))
                // call navigate to whiteboard: choose a position on the classroom side of the board at a
                // readable standing height, then face the board.
                NavigateToWhiteboard();
            // run the following statement only when gui: button using a new rect using button x plus action
            // width plus action gap, button y, action width, 32, the text "stand / jump".
            if (GUI.Button(new Rect(buttonX + actionWidth + actionGap, buttonY, actionWidth, 32f), "STAND / JUMP"))
                // call stand from seat: restore standing eye height and remove desktop seated tilt without
                // forcing a physical headset rotation.
                StandFromSeat();
            // run the following statement only when gui: button using a new rect using button x plus 2
            // multiplied by action width plus action gap, button y, action width, 32, the text "center view".
            if (GUI.Button(new Rect(buttonX + 2f * (actionWidth + actionGap), buttonY, actionWidth, 32f), "CENTER VIEW"))
                // call center view: return to the centre and also reset simulated controllers in desktop mode.
                CenterView();

            // add 32 plus gap to button y.
            buttonY += 32f + gap;
            // repeat this loop while i is less than table count. increase i by one after each pass.
            for (int i = 0; i < tableCount; i++)
            // begin the for statement block.
            {
                // store x as button x plus i modulo columns multiplied by button width plus gap.
                float x = buttonX + (i % columns) * (buttonWidth + gap);
                // store y as button y plus i divided by columns multiplied by 32 plus gap.
                float y = buttonY + (i / columns) * (32f + gap);
                // run the following statement only when gui: button using a new rect using x, y, button width,
                // 32, the text "table " plus i plus 1: to string.
                if (GUI.Button(new Rect(x, y, buttonWidth, 32f), "TABLE " + (i + 1).ToString()))
                    // try teleport to table using i.
                    TryTeleportToTable(i);
            // finish the for statement block.
            }

        // finish the method on gui block.
        }

        // method: is navigation pointer inside.
        // check the desktop navigation button rectangles so a navigation click is not passed through as a
        // drawing press.
        private static bool IsNavigationPointerInside(
            // two-dimensional vector.
            Vector2 pointer,
            // accept table count as a int parameter for is navigation pointer inside.
            int tableCount,
            // accept columns as a int parameter for is navigation pointer inside.
            int columns,
            // accept button x as a float parameter for is navigation pointer inside.
            float buttonX,
            // accept button y as a float parameter for is navigation pointer inside.
            float buttonY,
            // accept button width as a float parameter for is navigation pointer inside.
            float buttonWidth,
            // accept gap as a float parameter for is navigation pointer inside.
            float gap,
            // accept action width as a float parameter for is navigation pointer inside.
            float actionWidth)
        // begin the method is navigation pointer inside block.
        {
            // run the following statement only when whether a new rect using button x, button y, action width,
            // 32 contains pointer or whether a new rect using button x plus action width plus gap, button y,
            // action width, 32 contains pointer or whether a new rect using button x plus 2 multiplied by
            // action width plus gap, button y, action width, 32 contains pointer.
            if (new Rect(buttonX, buttonY, actionWidth, 32f).Contains(pointer)
                // whether a new rect using button x, button y, action width, 32 contains pointer or whether a
                // new rect using button x plus action width plus gap, button y, action width, 32 contains
                // pointer.
                || new Rect(buttonX + actionWidth + gap, buttonY, actionWidth, 32f).Contains(pointer)
                // whether a new rect using button x, button y, action width, 32 contains pointer or whether a
                // new rect using button x plus action width plus gap, button y, action width, 32 contains
                // pointer or whether a new rect using button x plus 2 multiplied by action width plus gap,
                // button y, action width, 32 contains pointer.
                || new Rect(buttonX + 2f * (actionWidth + gap), buttonY, actionWidth, 32f).Contains(pointer))
                // return true to the caller.
                return true;

            // store table top as button y plus 32 plus gap.
            float tableTop = buttonY + 32f + gap;
            // repeat this loop while i is less than table count. increase i by one after each pass.
            for (int i = 0; i < tableCount; i++)
            // begin the for statement block.
            {
                // store x as button x plus i modulo columns multiplied by button width plus gap.
                float x = buttonX + (i % columns) * (buttonWidth + gap);
                // store y as table top plus i divided by columns multiplied by 32 plus gap.
                float y = tableTop + (i / columns) * (32f + gap);
                // run the following statement only when whether a new rect using x, y, button width, 32
                // contains pointer.
                if (new Rect(x, y, buttonWidth, 32f).Contains(pointer))
                    // return true to the caller.
                    return true;
            // finish the for statement block.
            }

            // return false to the caller.
            return false;
        // finish the method is navigation pointer inside block.
        }

        // method: center view.
        // return to the centre and also reset simulated controllers in desktop mode.
        private void CenterView()
        // begin the method center view block.
        {
            // call reset view: return the camera to the centre anchor.
            ResetView();
            // run the following statement only when desktop mode.
            if (desktopMode)
                // call reset simulator controllers: request a reset on loaded simulator components and
                // remember which reset flags need clearing.
                ResetSimulatorControllers();
        // finish the method center view block.
        }

        // method: reset simulator controllers.
        // request a reset on loaded simulator components and remember which reset flags need clearing.
        private void ResetSimulatorControllers()
        // begin the method reset simulator controllers block.
        {
            // empty simulator reset targets.
            simulatorResetTargets.Clear();
            // empty simulator reset fields.
            simulatorResetFields.Clear();

            // store behaviours as resources: find objects of type all.
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            // repeat this loop while i is less than the number of items in behaviours. increase i by one after
            // each pass.
            for (int i = 0; i < behaviours.Length; i++)
            // begin the for statement block.
            {
                // store behaviour as the entry at i in behaviours.
                MonoBehaviour behaviour = behaviours[i];
                // run the following statement only when behaviour is missing or not (behaviour game object
                // scene: is valid) or behaviour: get type name: index of using the text "simulator", system
                // string comparison ordinal ignore case is less than 0.
                if (behaviour == null
                    // behaviour is missing or not (behaviour game object scene: is valid).
                    || !behaviour.gameObject.scene.IsValid()
                    // behaviour is missing or not (behaviour game object scene: is valid) or behaviour: get
                    // type name: index of using the text "simulator", system string comparison ordinal ignore
                    // case is less than 0.
                    || behaviour.GetType().Name.IndexOf("Simulator", System.StringComparison.OrdinalIgnoreCase) < 0)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store reset field as behaviour: get type: get field using the text "m_resetinput", binding
                // flags instance | binding flags non public.
                FieldInfo resetField = behaviour.GetType().GetField(
                    // supply the text "m_resetinput" to the enclosing call.
                    "m_ResetInput",
                    // supply binding flags instance | binding flags non public to the enclosing call.
                    BindingFlags.Instance | BindingFlags.NonPublic);
                // run the following statement only when reset field is missing or reset field field type
                // differs from the type information for bool.
                if (resetField == null || resetField.FieldType != typeof(bool))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // reset field: set value using behaviour, true.
                resetField.SetValue(behaviour, true);
                // append behaviour to simulator reset targets.
                simulatorResetTargets.Add(behaviour);
                // append reset field to simulator reset fields.
                simulatorResetFields.Add(resetField);
            // finish the for statement block.
            }

            // run the following statement only when the number of items in simulator reset targets is greater
            // than 0.
            if (simulatorResetTargets.Count > 0)
                // start coroutine using clear simulator reset flags.
                StartCoroutine(ClearSimulatorResetFlags());
        // finish the method reset simulator controllers block.
        }

        // method: clear simulator reset flags.
        // wait briefly in real time, then clear only the simulator reset flags this component set.
        private IEnumerator ClearSimulatorResetFlags()
        // begin the method clear simulator reset flags block.
        {
            // pause this coroutine and yield a new wait for seconds realtime using 0.1.
            yield return new WaitForSecondsRealtime(0.1f);
            // repeat this loop while i is less than the number of items in simulator reset targets and i is
            // less than the number of items in simulator reset fields. increase i by one after each pass.
            for (int i = 0; i < simulatorResetTargets.Count && i < simulatorResetFields.Count; i++)
            // begin the for statement block.
            {
                // run the following statement only when the entry at i in simulator reset targets exists.
                if (simulatorResetTargets[i] != null)
                    // the entry at i in simulator reset fields: set value using the entry at i in simulator
                    // reset targets, false.
                    simulatorResetFields[i].SetValue(simulatorResetTargets[i], false);
            // finish the for statement block.
            }
        // finish the method clear simulator reset flags block.
        }
    // finish the class xr study room locomotion block.
    }
// finish the namespace block.
}
