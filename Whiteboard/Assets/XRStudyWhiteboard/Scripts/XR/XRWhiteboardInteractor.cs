// script overview: this script routes controller or desktop pointer input to interface controls, desk paper,
// or the whiteboard. interface hits are handled before drawing, and switching surfaces ends the previous
// stroke. physical-controller tool interaction is assigned to the right hand, while the left hand remains
// available for navigation. the script resolves the configured select action, with a direct device-trigger
// fallback if no action is found. the physical button used by select depends on the input action binding, not
// its name.

// search: right controller | whiteboard writing | paper writing | colour | marker | eraser | ui input.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.event systems available to this script.
using UnityEngine.EventSystems;
// make keyboard, mouse, and input-action types available for controller and desktop input.
using UnityEngine.InputSystem;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;
// make the types in unity engine.xr available to this script.
using UnityEngine.XR;
// make the types in unity engine.xr.interaction.toolkit available to this script.
using UnityEngine.XR.Interaction.Toolkit;
// make the types in unity engine.xr.interaction.toolkit.interactors available to this script.
using UnityEngine.XR.Interaction.Toolkit.Interactors;
// use xr common usages as the local name for unity engine.xr.common usages.
using XRCommonUsages = UnityEngine.XR.CommonUsages;
// use xr input device as the local name for unity engine.xr.input device.
using XRInputDevice = UnityEngine.XR.InputDevice;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // priority-one controller interaction. the existing xri ray remains the
    // visible interaction ray; this component performs a focused physics ray
    // only against the whiteboard collider.
    // apply unity or c sharp metadata: default execution order(11000).
    [DefaultExecutionOrder(11000)]
    // declare the xr whiteboard interactor class based on unity behaviour.
    public sealed class XRWhiteboardInteractor : MonoBehaviour
    // begin the class xr whiteboard interactor block.
    {
        // canvas: the main board pixel surface that receives drawing operations. keep canvas as whiteboard
        // canvas for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardCanvas canvas;
        // drawer: the shared bridge that starts, continues, and ends whiteboard strokes. keep drawer as
        // whiteboard drawer for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardDrawer drawer;
        // ray origin: the transform providing the position and direction of the drawing ray. keep ray origin
        // as transform for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Transform rayOrigin;
        // max ray distance: the maximum permitted ray contact distance in metres. keep max ray distance as
        // float, initially 12. allow unity to save and configure this field in the inspector.
        [SerializeField] private float maxRayDistance = 12f;
        // keep use mouse fallback in editor as bool, initially true. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private bool useMouseFallbackInEditor = true;
        // keep controller input actions as input action asset for later use. allow unity to save and configure
        // this field in the inspector.
        [SerializeField] private InputActionAsset controllerInputActions;
        // keep desktop ui raycaster as graphic raycaster for later use. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private GraphicRaycaster desktopUiRaycaster;
        // the canvases interpolate between uv samples. a small amount of
        // bounded desktop smoothing removes cursor quantisation without
        // creating a visible trailing line behind the pointer.
        // keep desktop pointer smoothing as float, initially 0.35. allow unity to save and configure this
        // field in the inspector.
        [SerializeField, Range(0.1f, 1f)] private float desktopPointerSmoothing = 0.35f;
        // keep maximum desktop uv jump as float, initially 0.04. allow unity to save and configure this field
        // in the inspector.
        [SerializeField, Range(0.02f, 1f)] private float maximumDesktopUvJump = 0.04f;

        // keep right controller as xr input device for later use.
        private XRInputDevice rightController;
        // controller trigger action: the resolved drawing press action. its actual physical button comes from
        // the input binding. keep controller trigger action as input action for later use.
        private InputAction controllerTriggerAction;
        // keep resolved trigger hand as int, initially the negative of 2.
        private int resolvedTriggerHand = -2;
        // keep resolved ray hand as int, initially the negative of 2.
        private int resolvedRayHand = -2;
        // expose active desktop hand as if desktop locomotion exists and desktop locomotion is desktop
        // controller mode, use desktop locomotion desktop controller hand, otherwise use the negative of 1.
        private int ActiveDesktopHand => desktopLocomotion != null && desktopLocomotion.IsDesktopControllerMode ? desktopLocomotion.DesktopControllerHand : -1;
        // expose using simulator pointer as active desktop hand is at least 0 or keyboard current exists and
        // whether keyboard current space key is held down or whether keyboard current left shift key is held
        // down.
        private bool UsingSimulatorPointer => ActiveDesktopHand >= 0 || (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.leftShiftKey.isPressed));
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use.
        private XRStudyWhiteboardManager manager;
        // was pressed: the previous drawing press state used to recognise a new press. keep was pressed as
        // bool for later use.
        private bool wasPressed;
        // active paper: the sheet that currently owns the paper stroke, if any. keep active paper as paper
        // note canvas for later use.
        private PaperNoteCanvas activePaper;
        // keep desktop event system as event system for later use.
        private EventSystem desktopEventSystem;
        // keep desktop pointer event data as pointer event data for later use.
        private PointerEventData desktopPointerEventData;
        // keep desktop ui results as list<raycast result>, initially a new list<raycast result>.
        private readonly List<RaycastResult> desktopUiResults = new List<RaycastResult>();
        // keep desktop buttons as list<button>, initially a new list<button>.
        private readonly List<Button> desktopButtons = new List<Button>();
        // keep whiteboard ui canvas as canvas for later use.
        private Canvas whiteboardUiCanvas;
        // keep xr ray interactor as xr ray interactor for later use.
        private XRRayInteractor xrRayInteractor;
        // keep desktop locomotion as xr study room locomotion for later use.
        private XRStudyRoomLocomotion desktopLocomotion;
        // keep controller ui click held as bool for later use.
        private bool controllerUiClickHeld;
        // keep desktop hovered button as button for later use.
        private Button desktopHoveredButton;
        // keep desktop ui click held as bool for later use.
        private bool desktopUiClickHeld;
        // keep gameplay camera as camera for later use.
        private Camera gameplayCamera;
        // keep desktop mouse button held as bool for later use.
        private bool desktopMouseButtonHeld;
        // keep desktop right mouse button held as bool for later use.
        private bool desktopRightMouseButtonHeld;
        // keep desktop erase was active as bool for later use.
        private bool desktopEraseWasActive;
        // keep tool before desktop erase as whiteboard tool for later use.
        private WhiteboardTool toolBeforeDesktopErase;
        // keep using desktop cursor as bool for later use.
        private bool usingDesktopCursor;
        // keep smoothed desktop uv as two-dimensional vector for later use.
        private Vector2 smoothedDesktopUv;
        // keep has smoothed desktop uv as bool for later use.
        private bool hasSmoothedDesktopUv;
        // keep desktop gui screen position as two-dimensional vector for later use.
        private Vector2 desktopGuiScreenPosition;
        // keep has desktop gui screen position as bool for later use.
        private bool hasDesktopGuiScreenPosition;
        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // run the following statement only when ray origin is missing.
            if (rayOrigin == null)
                // set ray origin to transform.
                rayOrigin = transform;

            // set manager to find first object by type<xr study whiteboard manager>.
            manager = FindFirstObjectByType<XRStudyWhiteboardManager>();
            // set desktop locomotion to find first object by type<xr study room locomotion>.
            desktopLocomotion = FindFirstObjectByType<XRStudyRoomLocomotion>();
            // call restrict tool ui to right controller: disable left-hand tool ui participation while keeping
            // its navigation interactors available.
            RestrictToolUiToRightController();
            // call resolve xr ray origin: use the selected simulator hand or physical right-hand ray origin so
            // the drawing ray matches the intended controller.
            ResolveXrRayOrigin();
            // call resolve desktop ui raycaster: find the board world-space canvas and its graphic raycaster,
            // then assign the gameplay camera.
            ResolveDesktopUiRaycaster();
            // call resolve controller trigger action: find the selected hand interaction select action in the
            // configured or loaded input asset. select is an action name, so inspect its binding to identify
            // the physical button.
            ResolveControllerTriggerAction();
        // finish the method awake block.
        }

        // method: restrict tool ui to right controller.
        // disable left-hand tool ui participation while keeping its navigation interactors available.
        private static void RestrictToolUiToRightController()
        // begin the method restrict tool ui to right controller block.
        {
            // navigation belongs to the left controller. disable only its ui
            // participation so it cannot select marker, eraser, colour or
            // paper tools; its movement and teleport interactors still work.
            // visit each interactor in find objects by type<xr ray interactor> using find objects inactive
            // include, find objects sort mode none.
            foreach (XRRayInteractor interactor in FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            // begin the for each statement block.
            {
                // run the following statement only when interactor handedness equals interactor handedness
                // left.
                if (interactor.handedness == InteractorHandedness.Left)
                    // set interactor enable ui interaction to false.
                    interactor.enableUIInteraction = false;
            // finish the for each statement block.
            }
            // visit each interactor in find objects by type<near far interactor> using find objects inactive
            // include, find objects sort mode none.
            foreach (NearFarInteractor interactor in FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            // begin the for each statement block.
            {
                // run the following statement only when interactor handedness equals interactor handedness
                // left.
                if (interactor.handedness == InteractorHandedness.Left)
                    // set interactor enable ui interaction to false.
                    interactor.enableUIInteraction = false;
            // finish the for each statement block.
            }
            // visit each interactor in find objects by type<xr poke interactor> using find objects inactive
            // include, find objects sort mode none.
            foreach (XRPokeInteractor interactor in FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            // begin the for each statement block.
            {
                // run the following statement only when interactor handedness equals interactor handedness
                // left.
                if (interactor.handedness == InteractorHandedness.Left)
                    // set interactor enable ui interaction to false.
                    interactor.enableUIInteraction = false;
            // finish the for each statement block.
            }
        // finish the method restrict tool ui to right controller block.
        }

        // method: set references.
        // connect the existing scene objects used by this component rather than creating another copy of those
        // systems.
        public void SetReferences(WhiteboardCanvas whiteboardCanvas, WhiteboardDrawer whiteboardDrawer, Transform origin)
        // begin the method set references block.
        {
            // set canvas to whiteboard canvas.
            canvas = whiteboardCanvas;
            // set drawer to whiteboard drawer.
            drawer = whiteboardDrawer;
            // set ray origin to if origin exists, use origin, otherwise use transform.
            rayOrigin = origin != null ? origin : transform;
            // call resolve xr ray origin: use the selected simulator hand or physical right-hand ray origin so
            // the drawing ray matches the intended controller.
            ResolveXrRayOrigin();
        // finish the method set references block.
        }

        // method: configure input actions.
        // store the supplied action asset and resolve the action used for drawing presses.
        public void ConfigureInputActions(InputActionAsset actions)
        // begin the method configure input actions block.
        {
            // set controller input actions to actions.
            controllerInputActions = actions;
            // call resolve controller trigger action: find the selected hand interaction select action in the
            // configured or loaded input asset. select is an action name, so inspect its binding to identify
            // the physical button.
            ResolveControllerTriggerAction();
        // finish the method configure input actions block.
        }

        // method: resolve controller trigger action.
        // find the selected hand interaction select action in the configured or loaded input asset. select is
        // an action name, so inspect its binding to identify the physical button.
        private void ResolveControllerTriggerAction()
        // begin the method resolve controller trigger action block.
        {
            // store hand as active desktop hand.
            int hand = ActiveDesktopHand;
            // run the following statement only when controller trigger action exists and resolved trigger hand
            // equals hand.
            if (controllerTriggerAction != null && resolvedTriggerHand == hand)
                // leave this method now without running the remaining statements.
                return;
            // set resolved trigger hand to hand.
            resolvedTriggerHand = hand;
            // set controller trigger action to no object.
            controllerTriggerAction = null;
            // input binding: this method looks up the select action. the saved action asset binds controller
            // select to grip and activate to trigger. the direct device-trigger fallback below runs only when
            // no action was resolved. inspect the action asset rather than assuming select means the index
            // trigger.
            // choose the left interaction select action for the left simulated hand, otherwise use right
            // interaction select. the action binding determines the physical button.
            string actionName = hand == 0 ? "XRI Left Interaction/Select" : "XRI Right Interaction/Select";

            // run the following statement only when controller input actions exists.
            if (controllerInputActions != null)
            // begin the if statement block.
            {
                // set controller trigger action to controller input actions: find action using action name,
                // false.
                controllerTriggerAction = controllerInputActions.FindAction(
                    // supply action name to the enclosing call.
                    actionName,
                    // supply false to the enclosing call.
                    false);
            // finish the if statement block.
            }

            // the scene builder serializes the shared xri asset, but retain a
            // runtime fallback for scenes opened before that reference was
            // added. this also makes the script resilient to prefab changes.
            // run the following statement only when controller trigger action is missing.
            if (controllerTriggerAction == null)
            // begin the if statement block.
            {
                // store loaded assets as resources: find objects of type all.
                InputActionAsset[] loadedAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                // repeat this loop while i is less than the number of items in loaded assets. increase i by
                // one after each pass.
                for (int i = 0; i < loadedAssets.Length; i++)
                // begin the for statement block.
                {
                    // store candidate as the entry at i in loaded assets: find action using action name,
                    // false.
                    InputAction candidate = loadedAssets[i].FindAction(
                        // supply action name to the enclosing call.
                        actionName,
                        // supply false to the enclosing call.
                        false);
                    // run the following statement only when candidate is missing.
                    if (candidate == null)
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // set controller input actions to the entry at i in loaded assets.
                    controllerInputActions = loadedAssets[i];
                    // set controller trigger action to candidate.
                    controllerTriggerAction = candidate;
                    // leave the nearest loop or switch after finding the required result.
                    break;
                // finish the for statement block.
                }
            // finish the if statement block.
            }

            // run the following statement only when controller trigger action exists and not (whether
            // controller trigger action is enabled).
            if (controllerTriggerAction != null && !controllerTriggerAction.enabled)
                // enable controller trigger action so it can receive input.
                controllerTriggerAction.Enable();
        // finish the method resolve controller trigger action block.
        }

        // method: late update.
        // route input in order. first protect interface interactions, then try desk paper, then try the
        // whiteboard. finish strokes when contact or presses end.
        private void LateUpdate()
        // begin the method late update block.
        {
            // read the drawing press, then route input to controls, paper, or the board.
            // run the following statement only when canvas is missing or drawer is missing.
            if (canvas == null || drawer == null)
                // leave this method now without running the remaining statements.
                return;

            // the room navigation pad is imgui, so it is not represented by
            // the world-space graphicraycaster used for annotation controls.
            // consume its click state before resolving any board/paper ray;
            // otherwise a docked game-view coordinate mismatch can turn a
            // table or center click into a long accidental paper stroke.
            // run the following statement only when desktop locomotion exists and desktop locomotion is
            // desktop navigation pointer held.
            if (desktopLocomotion != null && desktopLocomotion.IsDesktopNavigationPointerHeld)
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // set was pressed to false.
                wasPressed = false;
                // set desktop mouse button held to false.
                desktopMouseButtonHeld = false;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when mouse current is missing or not (whether mouse current
            // left button is held down) and not (has desktop gui screen position).
            if ((Mouse.current == null || !Mouse.current.leftButton.isPressed)
                // mouse current is missing or not (whether mouse current left button is held down) and not
                // (has desktop gui screen position).
                && !hasDesktopGuiScreenPosition)
            // begin the if statement block.
            {
                // set desktop mouse button held to false.
                desktopMouseButtonHeld = false;
                // set has desktop gui screen position to false.
                hasDesktopGuiScreenPosition = false;
            // finish the if statement block.
            }

            // run the following statement only when mouse current is missing or not (whether mouse current
            // left button is held down) and not (desktop mouse button held).
            if (Mouse.current == null || (!Mouse.current.leftButton.isPressed && !desktopMouseButtonHeld))
            // begin the if statement block.
            {
                // set desktop ui click held to false.
                desktopUiClickHeld = false;
                // study table tool menu: end desktop pointer.
                StudyTableToolMenu.EndDesktopPointer();
            // finish the if statement block.
            }

            // store ray as a local ray value.
            Ray ray;
            // store pressed as a local bool value.
            bool pressed;
            // store desktop testing as whether this is the unity editor or the current runtime platform
            // differs from runtime platform android and not (xr settings is device active).
            bool desktopTesting = Application.isEditor
                // whether this is the unity editor or the current runtime platform differs from runtime
                // platform android and not (xr settings is device active).
                || (Application.platform != RuntimePlatform.Android && !XRSettings.isDeviceActive);

            // prefer the xr controller whenever the device simulator or a
            // real headset provides one. l mouse in the device simulator then
            // follows the same trigger path as a physical controller.
            // try get controller input using pressed, ray.
            TryGetControllerInput(out pressed, out ray);
            // store editor mouse available as use mouse fallback in editor and desktop testing and mouse
            // current exists and camera main exists.
            bool editorMouseAvailable = useMouseFallbackInEditor
                // use mouse fallback in editor and desktop testing.
                && desktopTesting
                // use mouse fallback in editor and desktop testing and mouse current exists.
                && Mouse.current != null
                // use mouse fallback in editor and desktop testing and mouse current exists and camera main
                // exists.
                && Camera.main != null;
            // store virtual controller aim as using simulator pointer.
            bool virtualControllerAim = UsingSimulatorPointer;
            // store desktop pointer active as mouse current exists and whether mouse current left button is
            // held down or desktop mouse button held and has desktop gui screen position.
            bool desktopPointerActive = Mouse.current != null
                // mouse current exists and whether mouse current left button is held down or desktop mouse
                // button held and has desktop gui screen position.
                && (Mouse.current.leftButton.isPressed
                    // whether mouse current left button is held down or desktop mouse button held and has
                    // desktop gui screen position.
                    || (desktopMouseButtonHeld && hasDesktopGuiScreenPosition));
            // the shared xri action asset can exist in the editor before the
            // simulator has published a usable virtual controller. in that
            // state trygetcontrollerinput reports the action but the ray is
            // still at its initial pose, so a normal cursor click appears to
            // do nothing. use the game-view cursor while the virtual trigger
            // is not active; once the simulator/quest trigger is active, keep
            // the controller ray as the source of truth.
            // run the following statement only when editor mouse available and not (virtual controller aim).
            if (editorMouseAvailable
                // editor mouse available and not (virtual controller aim).
                && !virtualControllerAim)
            // begin the if statement block.
            {
                // set using desktop cursor to true.
                usingDesktopCursor = true;
                // input system reports the live pointer in the docked
                // game-view render space on macos. use it directly so every
                // drag sample follows the actual cursor; caching imgui event
                // coordinates can freeze one axis and create vertical lines.
                // store mouse position as the current mouse.current.position.read value input value.
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                // run the following statement only when has desktop gui screen position.
                if (hasDesktopGuiScreenPosition)
                    // set mouse position to desktop gui screen position.
                    mousePosition = desktopGuiScreenPosition;

                // run the following statement only when desktop pointer active and not (has desktop gui screen
                // position).
                if (desktopPointerActive && !hasDesktopGuiScreenPosition)
                // begin the if statement block.
                {
                    // call end drawing: end both any active paper stroke and the whiteboard stroke before
                    // leaving a surface or changing input ownership.
                    EndDrawing();
                    // set was pressed to false.
                    wasPressed = false;
                    // leave this method now without running the remaining statements.
                    return;
                // finish the if statement block.
                }
                // xruiinputmodule's tracked-device raycaster is the correct
                // path for a real controller and the device simulator.  a
                // normal desktop cursor is a separate pointer, however, so
                // explicitly deliver its click to the world-space buttons.
                // without this bridge the cursor can draw on the board but
                // colour/marker/eraser buttons appear inert.
                // run the following statement only when try dispatch desktop ui click using mouse position.
                if (TryDispatchDesktopUiClick(mousePosition))
                // begin the if statement block.
                {
                    // call end drawing: end both any active paper stroke and the whiteboard stroke before
                    // leaving a surface or changing input ownership.
                    EndDrawing();
                    // set was pressed to false.
                    wasPressed = false;
                    // leave this method now without running the remaining statements.
                    return;
                // finish the if statement block.
                }

                // the official xr device simulator ui uses its own
                // tracked-device canvas. when mouse ui input is enabled, its
                // button can be pressed by the same desktop event that would
                // otherwise start a board stroke. consume any ui hit before
                // converting the pointer into a world ray so closing the
                // simulator cannot leave a vertical annotation behind.
                // run the following statement only when not (whether this is the unity editor and use mouse
                // fallback in editor) and is desktop pointer over ui using mouse position.
                if (!(Application.isEditor && useMouseFallbackInEditor)
                    // not (whether this is the unity editor and use mouse fallback in editor) and is desktop
                    // pointer over ui using mouse position.
                    && IsDesktopPointerOverUi(mousePosition))
                // begin the if statement block.
                {
                    // call end drawing: end both any active paper stroke and the whiteboard stroke before
                    // leaving a surface or changing input ownership.
                    EndDrawing();
                    // set was pressed to false.
                    wasPressed = false;
                    // leave this method now without running the remaining statements.
                    return;
                // finish the if statement block.
                }

                // store camera as get gameplay camera.
                Camera camera = GetGameplayCamera();
                // run the following statement only when camera is missing.
                if (camera == null)
                    // leave this method now without running the remaining statements.
                    return;
                // set ray to get desktop screen ray using camera, mouse position.
                ray = GetDesktopScreenRay(camera, mousePosition);
                // store desktop erase as desktop right mouse button held or mouse current exists and whether
                // mouse current right button is held down.
                bool desktopErase = desktopRightMouseButtonHeld
                    // desktop right mouse button held or mouse current exists and whether mouse current right
                    // button is held down.
                    || (Mouse.current != null && Mouse.current.rightButton.isPressed);
                // store desktop manager as if manager exists, use manager, otherwise use find first object by
                // type<xr study whiteboard manager>.
                XRStudyWhiteboardManager desktopManager = manager != null
                    // if manager exists, use manager, otherwise use find first object by type<xr study
                    // whiteboard manager>.
                    ? manager
                    // if manager exists, use manager, otherwise use find first object by type<xr study
                    // whiteboard manager>.
                    : FindFirstObjectByType<XRStudyWhiteboardManager>();
                // run the following statement only when desktop manager exists.
                if (desktopManager != null)
                // begin the if statement block.
                {
                    // run the following statement only when desktop erase and not (desktop erase was active).
                    if (desktopErase && !desktopEraseWasActive)
                    // begin the if statement block.
                    {
                        // set tool before desktop erase to desktop manager current tool.
                        toolBeforeDesktopErase = desktopManager.CurrentTool;
                        // desktop manager: set tool using whiteboard tool eraser.
                        desktopManager.SetTool(WhiteboardTool.Eraser);
                    // finish the if statement block.
                    }
                    // handle the alternative when the preceding condition is false.
                    else if (!desktopErase && desktopEraseWasActive)
                    // begin the if statement block.
                    {
                        // desktop manager: set tool using tool before desktop erase.
                        desktopManager.SetTool(toolBeforeDesktopErase);
                    // finish the if statement block.
                    }
                // finish the if statement block.
                }
                // set desktop erase was active to desktop erase.
                desktopEraseWasActive = desktopErase;
                // game-view press/release events are the cursor gesture's
                // source of truth; the simulator trigger has a separate owner.
                // set pressed to desktop mouse button held or desktop erase.
                pressed = desktopMouseButtonHeld || desktopErase;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // set using desktop cursor to false.
                usingDesktopCursor = false;
                // set has smoothed desktop uv to false.
                hasSmoothedDesktopUv = false;
                // set desktop erase was active to false.
                desktopEraseWasActive = false;
            // finish the else clause block.
            }

            // use the same ray fallback for the desktop cursor as for a real
            // controller. this avoids depending on docked game-view ui
            // coordinates when the table canvas is viewed at an angle.
            // run the following statement only when study table tool menu: contains any ray using ray.
            if (StudyTableToolMenu.ContainsAnyRay(ray))
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // remember the current press state so the next frame can distinguish a new press from a held
                // button.
                wasPressed = pressed;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // xruiinputmodule does not reliably deliver a click to a
            // world-space canvas when the built-in device simulator is the
            // active input source. resolve the whiteboard buttons directly
            // from the same ray used for drawing, and consume only an actual
            // button hit. empty panel space must remain transparent to the
            // board so it cannot create the “buttons block the board” bug.
            // run the following statement only when not (using desktop cursor) and try handle whiteboard ui
            // ray using ray, false.
            if (!usingDesktopCursor && TryHandleWhiteboardUiRay(ray, false))
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // remember the current press state so the next frame can distinguish a new press from a held
                // button.
                wasPressed = pressed;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store paper hit as paper note canvas: try get nearest using ray, max ray distance, the output
            // value paper, the output value paper uv.
            bool paperHit = PaperNoteCanvas.TryGetNearest(ray, maxRayDistance, out PaperNoteCanvas paper, out Vector2 paperUv);
            // run the following statement only when paper hit.
            if (paperHit)
            // begin the if statement block.
            {
                // a table paper and the main board use separate stroke
                // buffers. end a board stroke before switching surfaces so a
                // controller sweep cannot connect the two textures.
                // run the following statement only when active paper is missing.
                if (activePaper == null)
                    // end the current stroke on drawer so later hits do not join it.
                    drawer.EndStroke();

                // set paper uv to smooth desktop point using paper uv, pressed and was pressed.
                paperUv = SmoothDesktopPoint(paperUv, pressed && wasPressed);
                // paper: update cursor using paper uv.
                paper.UpdateCursor(paperUv);
                // run the following statement only when active paper differs from paper.
                if (activePaper != paper)
                // begin the if statement block.
                {
                    // run the following statement only when active paper exists.
                    if (activePaper != null)
                        // end the current stroke on active paper so later hits do not join it.
                        activePaper.EndStroke();
                    // set active paper to paper.
                    activePaper = paper;
                // finish the if statement block.
                }

                // store desktop paper erase as desktop right mouse button held or mouse current exists and
                // whether mouse current right button is held down.
                bool desktopPaperErase = desktopRightMouseButtonHeld
                    // desktop right mouse button held or mouse current exists and whether mouse current right
                    // button is held down.
                    || (Mouse.current != null && Mouse.current.rightButton.isPressed);
                // store erasing paper as desktop paper erase or paper tool is eraser active.
                bool erasingPaper = desktopPaperErase || PaperTool.IsEraserActive;
                // store can write on paper as desktop paper erase or paper tool is pencil active or paper tool
                // is eraser active.
                bool canWriteOnPaper = desktopPaperErase || PaperTool.IsPencilActive || PaperTool.IsEraserActive;
                // run the following statement only when pressed and can write on paper.
                if (pressed && canWriteOnPaper)
                    // send paper uv to paper for drawing.
                    paper.DrawAtUV(paperUv, erasingPaper, usingDesktopCursor);
                // handle the alternative when the preceding condition is false.
                else
                    // end the current stroke on paper so later hits do not join it.
                    paper.EndStroke();

                // run the following statement only when pressed and not (was pressed).
                if (pressed && !wasPressed)
                    // request a short right-controller vibration if the device supports it.
                    ControllerHaptics.PulseRightController();

                // remember the current press state so the next frame can distinguish a new press from a held
                // button.
                wasPressed = pressed;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when active paper exists.
            if (activePaper != null)
            // begin the if statement block.
            {
                // paper is a thin, angled surface. if its top-plane hit is
                // lost, ending the paper stroke immediately is safer than
                // joining the next hit across the miss. the old grace window
                // could turn a controller/cursor miss into a long vertical
                // connector on the note.
                // end the current stroke on active paper so later hits do not join it.
                activePaper.EndStroke();
                // set active paper to no object.
                activePaper = null;
            // finish the if statement block.
            }

            // run the following statement only when not (canvas: try get uv using ray, max ray distance, the
            // output value uv).
            if (!canvas.TryGetUV(ray, maxRayDistance, out Vector2 uv))
            // begin the if statement block.
            {
                // a new hit after leaving the surface begins a separate stroke.
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();

                // remember the current press state so the next frame can distinguish a new press from a held
                // button.
                wasPressed = pressed;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // set uv to smooth desktop point using uv, pressed and was pressed.
            uv = SmoothDesktopPoint(uv, pressed && wasPressed);
            // canvas: update cursor using uv.
            canvas.UpdateCursor(uv);
            // run the following statement only when pressed.
            if (pressed)
                // send uv to drawer for drawing.
                drawer.DrawAtUV(uv, usingDesktopCursor);
            // handle the alternative when the preceding condition is false.
            else
                // end the current stroke on drawer so later hits do not join it.
                drawer.EndStroke();

            // run the following statement only when pressed and not (was pressed).
            if (pressed && !wasPressed)
                // request a short right-controller vibration if the device supports it.
                ControllerHaptics.PulseRightController();

            // remember the current press state so the next frame can distinguish a new press from a held
            // button.
            wasPressed = pressed;
        // finish the method late update block.
        }

        // method: smooth desktop point.
        // apply bounded smoothing only to the desktop cursor. restart smoothing after a jump or a new stroke.
        private Vector2 SmoothDesktopPoint(Vector2 point, bool continueStroke)
        // begin the method smooth desktop point block.
        {
            // run the following statement only when not (using desktop cursor).
            if (!usingDesktopCursor)
            // begin the if statement block.
            {
                // set has smoothed desktop uv to false.
                hasSmoothedDesktopUv = false;
                // return point to the caller.
                return point;
            // finish the if statement block.
            }

            // run the following statement only when not (has smoothed desktop uv) or not (continue stroke).
            if (!hasSmoothedDesktopUv || !continueStroke)
            // begin the if statement block.
            {
                // set smoothed desktop uv to point.
                smoothedDesktopUv = point;
                // set has smoothed desktop uv to true.
                hasSmoothedDesktopUv = true;
                // return point to the caller.
                return point;
            // finish the if statement block.
            }

            // run the following statement only when the distance between smoothed desktop uv and point is
            // greater than maximum desktop uv jump.
            if (Vector2.Distance(smoothedDesktopUv, point) > maximumDesktopUvJump)
            // begin the if statement block.
            {
                // do not smooth across a surface re-aim or editor pointer
                // jump. returning the raw point lets the canvas reject the
                // discontinuity instead of drawing a long vertical segment.
                // set has smoothed desktop uv to false.
                hasSmoothedDesktopUv = false;
                // return point to the caller.
                return point;
            // finish the if statement block.
            }

            // run the following statement only when desktop pointer smoothing is at least 0.999.
            if (desktopPointerSmoothing >= 0.999f)
            // begin the if statement block.
            {
                // set smoothed desktop uv to point.
                smoothedDesktopUv = point;
                // return point to the caller.
                return point;
            // finish the if statement block.
            }

            // set smoothed desktop uv to an interpolated value from smoothed desktop uv toward point using
            // desktop pointer smoothing.
            smoothedDesktopUv = Vector2.Lerp(smoothedDesktopUv, point, desktopPointerSmoothing);
            // return smoothed desktop uv to the caller.
            return smoothedDesktopUv;
        // finish the method smooth desktop point block.
        }

        // method: on gui.
        // handle unity game-view interface events. this desktop path is separate from the headset world-space
        // controls.
        private void OnGUI()
        // begin the method on gui block.
        {
            // run the following statement only when not (whether this is the unity editor) and the current
            // runtime platform equals runtime platform android or xr settings is device active or event
            // current is missing or get gameplay camera is missing.
            if ((!Application.isEditor && (Application.platform == RuntimePlatform.Android || XRSettings.isDeviceActive))
                // not (whether this is the unity editor) and the current runtime platform equals runtime
                // platform android or xr settings is device active or event current is missing.
                || Event.current == null || GetGameplayCamera() == null)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when using simulator pointer.
            if (UsingSimulatorPointer)
                // leave this method now without running the remaining statements.
                return;
            // store current as event current.
            Event current = Event.current;

            // run the following statement only when current type equals event type mouse down or current type
            // equals event type mouse drag and current button equals 0.
            if ((current.type == EventType.MouseDown || current.type == EventType.MouseDrag) && current.button == 0)
            // begin the if statement block.
            {
                // run the following statement only when current type equals event type mouse down.
                if (current.type == EventType.MouseDown)
                // begin the if statement block.
                {
                    // call end drawing: end both any active paper stroke and the whiteboard stroke before
                    // leaving a surface or changing input ownership.
                    EndDrawing();
                    // set was pressed to false.
                    wasPressed = false;
                    // set has smoothed desktop uv to false.
                    hasSmoothedDesktopUv = false;
                // finish the if statement block.
                }
                // set desktop mouse button held to true.
                desktopMouseButtonHeld = true;
                // imgui supplies coordinates in the actual game render area.
                // set desktop gui screen position to a new two-dimensional vector using current mouse position
                // x, get gameplay camera pixel height minus current mouse position y.
                desktopGuiScreenPosition = new Vector2(current.mousePosition.x,
                    // supply get gameplay camera pixel height minus current mouse position y to the enclosing
                    // call.
                    GetGameplayCamera().pixelHeight - current.mousePosition.y);
                // set has desktop gui screen position to true.
                hasDesktopGuiScreenPosition = true;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (current.type == EventType.MouseUp && current.button == 0)
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // set was pressed to false.
                wasPressed = false;
                // set has smoothed desktop uv to false.
                hasSmoothedDesktopUv = false;
                // set desktop mouse button held to false.
                desktopMouseButtonHeld = false;
                // set has desktop gui screen position to false.
                hasDesktopGuiScreenPosition = false;
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (current.type == EventType.MouseDown && current.button == 1)
                // set desktop right mouse button held to true.
                desktopRightMouseButtonHeld = true;
            // handle the alternative when the preceding condition is false.
            else if (current.type == EventType.MouseUp && current.button == 1)
                // set desktop right mouse button held to false.
                desktopRightMouseButtonHeld = false;

            // run the following statement only when current type differs from event type mouse down or current
            // button differs from 0.
            if (current.type != EventType.MouseDown || current.button != 0)
                // leave this method now without running the remaining statements.
                return;

            // call resolve desktop ui raycaster: find the board world-space canvas and its graphic raycaster,
            // then assign the gameplay camera.
            ResolveDesktopUiRaycaster();
            // run the following statement only when desktop ui raycaster is missing.
            if (desktopUiRaycaster == null)
                // leave this method now without running the remaining statements.
                return;

            // ongui reports coordinates relative to the docked game view.
            // convert its top-left origin to the bottom-left origin used by
            // camera.worldtoscreenpoint and the world-space canvas.
            // store game view point as a new two-dimensional vector using current mouse position x, screen
            // height minus current mouse position y.
            Vector2 gameViewPoint = new Vector2(
                // supply current mouse position x to the enclosing call.
                current.mousePosition.x,
                // supply screen height minus current mouse position y to the enclosing call.
                Screen.height - current.mousePosition.y);
            // store camera as get gameplay camera.
            Camera camera = GetGameplayCamera();
            // run the following statement only when camera exists and study table tool menu: try handle
            // desktop screen point using game view point, camera, true.
            if (camera != null
                // camera exists and study table tool menu: try handle desktop screen point using game view
                // point, camera, true.
                && StudyTableToolMenu.TryHandleDesktopScreenPoint(gameViewPoint, camera, true))
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // current: use.
                current.Use();
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store candidates as find objects by type<button> using find objects inactive exclude, find
            // objects sort mode none.
            Button[] candidates = FindObjectsByType<Button>(
                // supply find objects inactive exclude to the enclosing call.
                FindObjectsInactive.Exclude,
                // supply find objects sort mode none to the enclosing call.
                FindObjectsSortMode.None);
            // store best button as no object.
            Button bestButton = null;
            // repeat this loop while i is less than the number of items in candidates. increase i by one after
            // each pass.
            for (int i = 0; i < candidates.Length; i++)
            // begin the for statement block.
            {
                // store candidate as the entry at i in candidates.
                Button candidate = candidates[i];
                // run the following statement only when not (is eligible project button using candidate).
                if (!IsEligibleProjectButton(candidate))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store canvas as candidate transform: get component in parent.
                Canvas canvas = candidate.transform.GetComponentInParent<Canvas>();
                // run the following statement only when not (is project world canvas using canvas).
                if (!IsProjectWorldCanvas(canvas))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // store rect as candidate transform treated as rect transform.
                RectTransform rect = candidate.transform as RectTransform;
                // run the following statement only when rect is missing.
                if (rect == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store inside as is screen point inside using rect, game view point.
                bool inside = IsScreenPointInside(rect, gameViewPoint);
                // run the following statement only when not (inside).
                if (!inside)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // run the following statement only when best button is missing or the squared length of rect
                // rect size is less than the squared length of best button transform treated as rect transform
                // rect size.
                if (bestButton == null || rect.rect.size.sqrMagnitude < (bestButton.transform as RectTransform).rect.size.sqrMagnitude)
                    // set best button to candidate.
                    bestButton = candidate;
            // finish the for statement block.
            }

            // run the following statement only when best button is missing.
            if (bestButton == null)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when best button: is interactable and not (desktop ui click
            // held).
            if (bestButton.IsInteractable() && !desktopUiClickHeld)
                // invoke best button on click.
                bestButton.onClick.Invoke();
            // set desktop ui click held to true.
            desktopUiClickHeld = true;
            // set desktop mouse button held to false.
            desktopMouseButtonHeld = false;
            // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving a
            // surface or changing input ownership.
            EndDrawing();
            // current: use.
            current.Use();
        // finish the method on gui block.
        }

        // method: end drawing.
        // end both any active paper stroke and the whiteboard stroke before leaving a surface or changing
        // input ownership.
        private void EndDrawing()
        // begin the method end drawing block.
        {
            // run the following statement only when active paper exists.
            if (activePaper != null)
            // begin the if statement block.
            {
                // end the current stroke on active paper so later hits do not join it.
                activePaper.EndStroke();
                // set active paper to no object.
                activePaper = null;
            // finish the if statement block.
            }

            // run the following statement only when drawer exists.
            if (drawer != null)
                // end the current stroke on drawer so later hits do not join it.
                drawer.EndStroke();
        // finish the method end drawing block.
        }

        // method: get desktop screen ray.
        // make a world ray from the current gameplay camera and screen position after navigation has updated
        // the view.
        private Ray GetDesktopScreenRay(Camera camera, Vector2 screenPosition)
        // begin the method get desktop screen ray block.
        {
            // resolve after locomotion's lateupdate: a cached pose can differ
            // from the rendered seated camera and offset every stroke.
            // return camera: screen point to ray using screen position to the caller.
            return camera.ScreenPointToRay(screenPosition);
        // finish the method get desktop screen ray block.
        }

        // method: resolve desktop ui raycaster.
        // find the board world-space canvas and its graphic raycaster, then assign the gameplay camera.
        private void ResolveDesktopUiRaycaster()
        // begin the method resolve desktop ui raycaster block.
        {
            // run the following statement only when desktop ui raycaster exists.
            if (desktopUiRaycaster != null)
            // begin the if statement block.
            {
                // run the following statement only when whiteboard ui canvas is missing.
                if (whiteboardUiCanvas == null)
                    // set whiteboard ui canvas to the the canvas component attached to this object component
                    // on desktop ui raycaster.
                    whiteboardUiCanvas = desktopUiRaycaster.GetComponent<Canvas>();
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store canvases as find objects by type<canvas> using find objects inactive include, find objects
            // sort mode none.
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // repeat this loop while i is less than the number of items in canvases. increase i by one after
            // each pass.
            for (int i = 0; i < canvases.Length; i++)
            // begin the for statement block.
            {
                // store candidate as the entry at i in canvases.
                Canvas candidate = canvases[i];
                // run the following statement only when candidate is missing or candidate render mode differs
                // from render mode world space.
                if (candidate == null || candidate.renderMode != RenderMode.WorldSpace)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // run the following statement only when not (candidate name: equals using the text
                // "whiteboardui", system string comparison ordinal ignore case).
                if (!candidate.name.Equals("WhiteboardUI", System.StringComparison.OrdinalIgnoreCase))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // set desktop ui raycaster to the the graphic raycaster component attached to this object
                // component on candidate.
                desktopUiRaycaster = candidate.GetComponent<GraphicRaycaster>();
                // run the following statement only when desktop ui raycaster is missing.
                if (desktopUiRaycaster == null)
                    // set desktop ui raycaster to a newly attached add component<graphic raycaster> component
                    // on candidate game object.
                    desktopUiRaycaster = candidate.gameObject.AddComponent<GraphicRaycaster>();
                // store camera as get gameplay camera.
                Camera camera = GetGameplayCamera();
                // run the following statement only when camera exists.
                if (camera != null)
                    // set candidate world camera to camera.
                    candidate.worldCamera = camera;
                // set whiteboard ui canvas to candidate.
                whiteboardUiCanvas = candidate;

                // store panel image as if the child or resource the text "toolpanel" found in candidate
                // transform exists, the .the image component attached to this object component on this object.
                Image panelImage = candidate.transform.Find("ToolPanel")?.GetComponent<Image>();
                // run the following statement only when panel image exists.
                if (panelImage != null)
                    // set panel image raycast target to false.
                    panelImage.raycastTarget = false;
                // store confirmation overlay as if the child or resource the text
                // "toolpanel/clearconfirmation" found in candidate transform exists, the .the image component
                // attached to this object component on this object.
                Image confirmationOverlay = candidate.transform.Find("ToolPanel/ClearConfirmation")?.GetComponent<Image>();
                // run the following statement only when confirmation overlay exists.
                if (confirmationOverlay != null)
                    // set confirmation overlay raycast target to false.
                    confirmationOverlay.raycastTarget = false;
                // leave the nearest loop or switch after finding the required result.
                break;
            // finish the for statement block.
            }

            // set desktop event system to event system current.
            desktopEventSystem = EventSystem.current;
        // finish the method resolve desktop ui raycaster block.
        }

        // method: resolve xr ray origin.
        // use the selected simulator hand or physical right-hand ray origin so the drawing ray matches the
        // intended controller.
        private void ResolveXrRayOrigin()
        // begin the method resolve xr ray origin block.
        {
            // store hand as active desktop hand.
            int hand = ActiveDesktopHand;
            // run the following statement only when resolved ray hand differs from hand.
            if (resolvedRayHand != hand)
            // begin the if statement block.
            {
                // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving
                // a surface or changing input ownership.
                EndDrawing();
                // set resolved ray hand to hand.
                resolvedRayHand = hand;
                // visit each candidate in find objects by type<xr ray interactor> using find objects inactive
                // exclude, find objects sort mode none.
                foreach (XRRayInteractor candidate in FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                // begin the for each statement block.
                {
                    // run the following statement only when candidate handedness: to string differs from if
                    // hand equals 0, use the text "left", otherwise use the text "right".
                    if (candidate.handedness.ToString() != (hand == 0 ? "Left" : "Right")) continue;
                    // set ray origin to if candidate ray origin transform exists, use candidate ray origin
                    // transform, otherwise use candidate transform.
                    rayOrigin = candidate.rayOriginTransform != null ? candidate.rayOriginTransform : candidate.transform;
                    // leave the nearest loop or switch after finding the required result.
                    break;
                // finish the for each statement block.
                }
            // finish the if statement block.
            }
            // run the following statement only when ray origin is missing.
            if (rayOrigin == null)
                // leave this method now without running the remaining statements.
                return;

            // set xr ray interactor to the the xr ray interactor component attached to this object component
            // on ray origin.
            xrRayInteractor = rayOrigin.GetComponent<XRRayInteractor>();
            // run the following statement only when xr ray interactor is missing.
            if (xrRayInteractor == null)
                // set xr ray interactor to ray origin: get component in parent.
                xrRayInteractor = rayOrigin.GetComponentInParent<XRRayInteractor>();
            // run the following statement only when xr ray interactor is missing.
            if (xrRayInteractor == null)
                // set xr ray interactor to the requested component in the children of ray origin.
                xrRayInteractor = rayOrigin.GetComponentInChildren<XRRayInteractor>(true);

            // run the following statement only when xr ray interactor exists and xr ray interactor ray origin
            // transform exists.
            if (xrRayInteractor != null && xrRayInteractor.rayOriginTransform != null)
                // set ray origin to xr ray interactor ray origin transform.
                rayOrigin = xrRayInteractor.rayOriginTransform;
        // finish the method resolve xr ray origin block.
        }

        // method: try handle whiteboard ui ray.
        // test the controller ray against eligible board buttons and consume a button hit before it can create
        // ink.
        private bool TryHandleWhiteboardUiRay(Ray ray, bool pressed)
        // begin the method try handle whiteboard ui ray block.
        {
            // run the following statement only when whiteboard ui canvas is missing.
            if (whiteboardUiCanvas == null)
                // call resolve desktop ui raycaster: find the board world-space canvas and its graphic
                // raycaster, then assign the gameplay camera.
                ResolveDesktopUiRaycaster();

            // run the following statement only when whiteboard ui canvas is missing.
            if (whiteboardUiCanvas == null)
            // begin the if statement block.
            {
                // set controller ui click held to false.
                controllerUiClickHeld = false;
                // return false to the caller.
                return false;
            // finish the if statement block.
            }

            // store canvas rect as whiteboard ui canvas transform treated as rect transform.
            RectTransform canvasRect = whiteboardUiCanvas.transform as RectTransform;
            // run the following statement only when canvas rect is missing.
            if (canvasRect == null)
            // begin the if statement block.
            {
                // set controller ui click held to false.
                controllerUiClickHeld = false;
                // return false to the caller.
                return false;
            // finish the if statement block.
            }

            // store canvas plane as a new plane using whiteboard ui canvas transform forward, whiteboard ui
            // canvas transform position.
            Plane canvasPlane = new Plane(whiteboardUiCanvas.transform.forward, whiteboardUiCanvas.transform.position);
            // run the following statement only when not (canvas plane: raycast using ray, the output value
            // distance) or distance is less than 0.
            if (!canvasPlane.Raycast(ray, out float distance) || distance < 0f)
            // begin the if statement block.
            {
                // set controller ui click held to false.
                controllerUiClickHeld = false;
                // return false to the caller.
                return false;
            // finish the if statement block.
            }

            // store world point as the point distance units along ray.
            Vector3 worldPoint = ray.GetPoint(distance);
            // store hit button as no object.
            Button hitButton = null;
            // store buttons as the matching components below whiteboard ui canvas.
            Button[] buttons = whiteboardUiCanvas.GetComponentsInChildren<Button>(false);
            // repeat this loop while i is less than the number of items in buttons. increase i by one after
            // each pass.
            for (int i = 0; i < buttons.Length; i++)
            // begin the for statement block.
            {
                // store button as the entry at i in buttons.
                Button button = buttons[i];
                // run the following statement only when not (is eligible project button using button).
                if (!IsEligibleProjectButton(button))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store button rect as button transform treated as rect transform.
                RectTransform buttonRect = button.transform as RectTransform;
                // run the following statement only when button rect is missing.
                if (buttonRect == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store local point as world point converted from world coordinates into button rect local
                // coordinates.
                Vector2 localPoint = buttonRect.InverseTransformPoint(worldPoint);
                // run the following statement only when whether button rect rect contains local point.
                if (buttonRect.rect.Contains(localPoint))
                // begin the if statement block.
                {
                    // set hit button to button.
                    hitButton = button;
                    // leave the nearest loop or switch after finding the required result.
                    break;
                // finish the if statement block.
                }
            // finish the for statement block.
            }

            // run the following statement only when hit button is missing.
            if (hitButton == null)
            // begin the if statement block.
            {
                // set controller ui click held to false.
                controllerUiClickHeld = false;
                // return false to the caller.
                return false;
            // finish the if statement block.
            }

            // run the following statement only when pressed and not (controller ui click held) and hit button:
            // is interactable.
            if (pressed && !controllerUiClickHeld && hitButton.IsInteractable())
                // invoke hit button on click.
                hitButton.onClick.Invoke();

            // set controller ui click held to pressed.
            controllerUiClickHeld = pressed;
            // return true to the caller.
            return true;
        // finish the method try handle whiteboard ui ray block.
        }

        // method: try dispatch desktop ui click.
        // deliver desktop pointer clicks to the project buttons and track the press state to avoid repeated
        // clicks.
        private bool TryDispatchDesktopUiClick(Vector2 screenPosition)
        // begin the method try dispatch desktop ui click block.
        {
            // run the following statement only when mouse current is missing.
            if (Mouse.current == null)
                // return false to the caller.
                return false;
            // store pointer pressed as whether mouse current left button is held down or desktop mouse button
            // held.
            bool pointerPressed = Mouse.current.leftButton.isPressed || desktopMouseButtonHeld;

            // call resolve desktop ui raycaster: find the board world-space canvas and its graphic raycaster,
            // then assign the gameplay camera.
            ResolveDesktopUiRaycaster();
            // store camera as get gameplay camera.
            Camera camera = GetGameplayCamera();
            // run the following statement only when camera is missing.
            if (camera == null)
                // return false to the caller.
                return false;

            // table menus are generated world-space canvases and are not part
            // of the whiteboard graphicraycaster. check their real button
            // rectangles first so a desktop click can open/select tools
            // without falling through to the paper surface.
            // run the following statement only when study table tool menu: try handle desktop screen point
            // using screen position, camera, pointer pressed.
            if (StudyTableToolMenu.TryHandleDesktopScreenPoint(screenPosition, camera, pointerPressed))
            // begin the if statement block.
            {
                // set desktop ui click held to pointer pressed.
                desktopUiClickHeld = pointerPressed;
                // set desktop mouse button held to false.
                desktopMouseButtonHeld = false;
                // update desktop button feedback using no object, false.
                UpdateDesktopButtonFeedback(null, false);
                // return true to the caller.
                return true;
            // finish the if statement block.
            }

            // run the following statement only when desktop ui raycaster is missing.
            if (desktopUiRaycaster == null)
                // return false to the caller.
                return false;

            // run the following statement only when desktop event system is missing.
            if (desktopEventSystem == null)
                // set desktop event system to event system current.
                desktopEventSystem = EventSystem.current;
            // run the following statement only when desktop event system is missing.
            if (desktopEventSystem == null)
                // return false to the caller.
                return false;

            // run the following statement only when desktop pointer event data is missing.
            if (desktopPointerEventData == null)
                // set desktop pointer event data to a new pointer event data using desktop event system.
                desktopPointerEventData = new PointerEventData(desktopEventSystem);
            // desktop pointer event data: reset.
            desktopPointerEventData.Reset();
            // set desktop pointer event data position to screen position.
            desktopPointerEventData.position = screenPosition;
            // set desktop pointer event data button to pointer event data input button left.
            desktopPointerEventData.button = PointerEventData.InputButton.Left;
            // set desktop pointer event data pointer id to the negative of 1.
            desktopPointerEventData.pointerId = -1;
            // empty desktop ui results.
            desktopUiResults.Clear();
            // desktop ui raycaster: raycast using desktop pointer event data, desktop ui results.
            desktopUiRaycaster.Raycast(desktopPointerEventData, desktopUiResults);
            // store button as no object.
            Button button = null;
            // run the following statement only when the number of items in desktop ui results is greater than
            // 0.
            if (desktopUiResults.Count > 0)
            // begin the if statement block.
            {
                // store hit as the entry at 0 in desktop ui results game object.
                GameObject hit = desktopUiResults[0].gameObject;
                // set button to if hit exists, use hit: get component in parent, otherwise use no object.
                button = hit != null ? hit.GetComponentInParent<Button>() : null;
            // finish the if statement block.
            }

            // some unity editor versions do not feed a world-space
            // graphicraycaster the docked game-view pointer coordinates even
            // when the camera ray uses them correctly.  check the button
            // rectangles directly as a deterministic fallback for that
            // editor-only case.
            // run the following statement only when button is missing and camera exists.
            if (button == null && camera != null)
            // begin the if statement block.
            {
                // empty desktop buttons.
                desktopButtons.Clear();
                // store candidates as find objects by type<button> using find objects inactive exclude, find
                // objects sort mode none.
                Button[] candidates = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                // repeat this loop while i is less than the number of items in candidates. increase i by one
                // after each pass.
                for (int i = 0; i < candidates.Length; i++)
                // begin the for statement block.
                {
                    // store candidate as the entry at i in candidates.
                    Button candidate = candidates[i];
                    // run the following statement only when not (is eligible project button using candidate).
                    if (!IsEligibleProjectButton(candidate))
                        // skip the rest of this loop pass and move to the next entry.
                        continue;
                    // store canvas as candidate transform: get component in parent.
                    Canvas canvas = candidate.transform.GetComponentInParent<Canvas>();
                    // run the following statement only when not (is project world canvas using canvas).
                    if (!IsProjectWorldCanvas(canvas))
                        // skip the rest of this loop pass and move to the next entry.
                        continue;
                    // store rect as candidate transform treated as rect transform.
                    RectTransform rect = candidate.transform as RectTransform;
                    // run the following statement only when rect is missing.
                    if (rect == null)
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // run the following statement only when not (is screen point inside using rect, screen
                    // position).
                    if (!IsScreenPointInside(rect, screenPosition))
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // append candidate to desktop buttons.
                    desktopButtons.Add(candidate);
                // finish the for statement block.
                }

                // run the following statement only when the number of items in desktop buttons is greater than
                // 0.
                if (desktopButtons.Count > 0)
                // begin the if statement block.
                {
                    // the smallest containing rectangle is the most specific
                    // hit when a confirmation overlay is visible.
                    // set button to the entry at 0 in desktop buttons.
                    button = desktopButtons[0];
                    // repeat this loop while i is less than the number of items in desktop buttons. increase i
                    // by one after each pass.
                    for (int i = 1; i < desktopButtons.Count; i++)
                    // begin the for statement block.
                    {
                        // store current as the entry at i in desktop buttons transform treated as rect
                        // transform.
                        RectTransform current = desktopButtons[i].transform as RectTransform;
                        // store best as button transform treated as rect transform.
                        RectTransform best = button.transform as RectTransform;
                        // run the following statement only when current exists and best exists and the squared
                        // length of current rect size is less than the squared length of best rect size.
                        if (current != null && best != null && current.rect.size.sqrMagnitude < best.rect.size.sqrMagnitude)
                            // set button to the entry at i in desktop buttons.
                            button = desktopButtons[i];
                    // finish the for statement block.
                    }
                // finish the if statement block.
                }
            // finish the if statement block.
            }

            // run the following statement only when not (is eligible project button using button).
            if (!IsEligibleProjectButton(button))
                // set button to no object.
                button = null;
            // update desktop button feedback using button, pointer pressed.
            UpdateDesktopButtonFeedback(button, pointerPressed);
            // run the following statement only when button is missing.
            if (button == null)
                // return false to the caller.
                return false;

            // invoke only on the press edge. the explicit latch is more
            // reliable than waspressedthisframe when a cgevent click enters
            // a docked game view between editor frames.
            // run the following statement only when pointer pressed and not (desktop ui click held).
            if (pointerPressed && !desktopUiClickHeld)
            // begin the if statement block.
            {
                // run the following statement only when button: is interactable.
                if (button.IsInteractable())
                    // invoke button on click.
                    button.onClick.Invoke();
                // set desktop ui click held to true.
                desktopUiClickHeld = true;
                // a navigation/tool click is a complete ui gesture. do not
                // carry its press latch into the next desktop drawing stroke
                // when the editor fails to deliver the matching mouseup.
                // set desktop mouse button held to false.
                desktopMouseButtonHeld = false;
            // finish the if statement block.
            }

            // return true to the caller.
            return true;
        // finish the method try dispatch desktop ui click block.
        }

        // method: is desktop pointer over ui.
        // check whether the desktop pointer is over interface content that should block a drawing stroke.
        private bool IsDesktopPointerOverUi(Vector2 screenPosition)
        // begin the method is desktop pointer over ui block.
        {
            // run the following statement only when desktop event system is missing.
            if (desktopEventSystem == null)
                // set desktop event system to event system current.
                desktopEventSystem = EventSystem.current;
            // run the following statement only when desktop event system is missing.
            if (desktopEventSystem == null)
                // return false to the caller.
                return false;

            // run the following statement only when desktop pointer event data is missing.
            if (desktopPointerEventData == null)
                // set desktop pointer event data to a new pointer event data using desktop event system.
                desktopPointerEventData = new PointerEventData(desktopEventSystem);
            // desktop pointer event data: reset.
            desktopPointerEventData.Reset();
            // set desktop pointer event data position to screen position.
            desktopPointerEventData.position = screenPosition;
            // set desktop pointer event data button to pointer event data input button left.
            desktopPointerEventData.button = PointerEventData.InputButton.Left;
            // set desktop pointer event data pointer id to the negative of 1.
            desktopPointerEventData.pointerId = -1;
            // empty desktop ui results.
            desktopUiResults.Clear();
            // desktop event system: raycast all using desktop pointer event data, desktop ui results.
            desktopEventSystem.RaycastAll(desktopPointerEventData, desktopUiResults);
            // return the number of items in desktop ui results is greater than 0 to the caller.
            return desktopUiResults.Count > 0;
        // finish the method is desktop pointer over ui block.
        }

        // method: is screen point inside.
        // test whether a screen position falls inside the projected rectangle of a world-space control.
        private bool IsScreenPointInside(RectTransform rect, Vector2 screenPosition)
        // begin the method is screen point inside block.
        {
            // store corners as a new three-dimensional vector[4] array.
            Vector3[] corners = new Vector3[4];
            // rect: get world corners using corners.
            rect.GetWorldCorners(corners);
            // store min x as float positive infinity.
            float minX = float.PositiveInfinity;
            // store max x as float negative infinity.
            float maxX = float.NegativeInfinity;
            // store min y as float positive infinity.
            float minY = float.PositiveInfinity;
            // store max y as float negative infinity.
            float maxY = float.NegativeInfinity;
            // repeat this loop while i is less than the number of items in corners. increase i by one after
            // each pass.
            for (int i = 0; i < corners.Length; i++)
            // begin the for statement block.
            {
                // store point as rect transform utility: world to screen point using get gameplay camera, the
                // entry at i in corners.
                Vector2 point = RectTransformUtility.WorldToScreenPoint(GetGameplayCamera(), corners[i]);
                // set min x to the smaller of min x and point x.
                minX = Mathf.Min(minX, point.x);
                // set max x to the larger of max x and point x.
                maxX = Mathf.Max(maxX, point.x);
                // set min y to the smaller of min y and point y.
                minY = Mathf.Min(minY, point.y);
                // set max y to the larger of max y and point y.
                maxY = Mathf.Max(maxY, point.y);
            // finish the for statement block.
            }

            // return screen position x is at least min x and screen position x is at most max x and screen
            // position y is at least min y and screen position y is at most max y to the caller.
            return screenPosition.x >= minX
                // screen position x is at least min x and screen position x is at most max x.
                && screenPosition.x <= maxX
                // screen position x is at least min x and screen position x is at most max x and screen
                // position y is at least min y.
                && screenPosition.y >= minY
                // screen position x is at least min x and screen position x is at most max x and screen
                // position y is at least min y and screen position y is at most max y.
                && screenPosition.y <= maxY;
        // finish the method is screen point inside block.
        }

        // method: get gameplay camera.
        // find and cache the camera that renders the classroom rather than a separate interface camera.
        private Camera GetGameplayCamera()
        // begin the method get gameplay camera block.
        {
            // run the following statement only when gameplay camera exists.
            if (gameplayCamera != null)
                // return gameplay camera to the caller.
                return gameplayCamera;

            // run the following statement only when ray origin exists.
            if (rayOrigin != null)
                // set gameplay camera to the requested component in the children of ray origin root.
                gameplayCamera = rayOrigin.root.GetComponentInChildren<Camera>(true);
            // run the following statement only when gameplay camera is missing.
            if (gameplayCamera == null)
                // set gameplay camera to camera main.
                gameplayCamera = Camera.main;
            // return gameplay camera to the caller.
            return gameplayCamera;
        // finish the method get gameplay camera block.
        }

        // method: is eligible project button.
        // accept only visible and interactable buttons that belong to the project controls.
        private static bool IsEligibleProjectButton(Button button)
        // begin the method is eligible project button block.
        {
            // run the following statement only when button is missing or not (button is active and enabled).
            if (button == null || !button.isActiveAndEnabled)
                // return false to the caller.
                return false;
            // store owner as button: get component in parent.
            Canvas owner = button.GetComponentInParent<Canvas>();
            // run the following statement only when not (is project world canvas using owner).
            if (!IsProjectWorldCanvas(owner))
                // return false to the caller.
                return false;
            // store confirmation as the child or resource the text "toolpanel/clearconfirmation" found in
            // owner transform.
            Transform confirmation = owner.transform.Find("ToolPanel/ClearConfirmation");
            // return confirmation is missing or not (confirmation game object active in hierarchy) or button
            // transform: is child of using confirmation to the caller.
            return confirmation == null || !confirmation.gameObject.activeInHierarchy
                // confirmation is missing or not (confirmation game object active in hierarchy) or button
                // transform: is child of using confirmation.
                || button.transform.IsChildOf(confirmation);
        // finish the method is eligible project button block.
        }

        // method: update desktop button feedback.
        // send pointer enter, exit, down, and up feedback so desktop buttons reflect the current hover and
        // press.
        private void UpdateDesktopButtonFeedback(Button button, bool pressed)
        // begin the method update desktop button feedback block.
        {
            // run the following statement only when desktop pointer event data is missing.
            if (desktopPointerEventData == null)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when desktop hovered button differs from button.
            if (desktopHoveredButton != button)
            // begin the if statement block.
            {
                // run the following statement only when desktop hovered button exists.
                if (desktopHoveredButton != null)
                    // desktop hovered button: on pointer exit using desktop pointer event data.
                    desktopHoveredButton.OnPointerExit(desktopPointerEventData);
                // set desktop hovered button to button.
                desktopHoveredButton = button;
                // run the following statement only when button exists.
                if (button != null)
                    // button: on pointer enter using desktop pointer event data.
                    button.OnPointerEnter(desktopPointerEventData);
            // finish the if statement block.
            }
            // run the following statement only when button exists.
            if (button != null)
            // begin the if statement block.
            {
                // run the following statement only when pressed.
                if (pressed) button.OnPointerDown(desktopPointerEventData);
                // handle the alternative when the preceding condition is false.
                else button.OnPointerUp(desktopPointerEventData);
            // finish the if statement block.
            }
        // finish the method update desktop button feedback block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // call end drawing: end both any active paper stroke and the whiteboard stroke before leaving a
            // surface or changing input ownership.
            EndDrawing();
            // set desktop mouse button held to set desktop right mouse button held to false.
            desktopMouseButtonHeld = desktopRightMouseButtonHeld = false;
            // set desktop ui click held to set was pressed to false.
            desktopUiClickHeld = wasPressed = false;
            // set has desktop gui screen position to false.
            hasDesktopGuiScreenPosition = false;
            // update desktop button feedback using no object, false.
            UpdateDesktopButtonFeedback(null, false);
        // finish the method on disable block.
        }

        // method: is project world canvas.
        // recognise the project world-space canvases that should receive this pointer handling.
        private static bool IsProjectWorldCanvas(Canvas canvas)
        // begin the method is project world canvas block.
        {
            // run the following statement only when canvas is missing or canvas render mode differs from
            // render mode world space.
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                // return false to the caller.
                return false;

            // return canvas name: equals using the text "whiteboardui", system string comparison ordinal
            // ignore case or canvas name: equals using the text "tabletoolmenu", system string comparison
            // ordinal ignore case to the caller.
            return canvas.name.Equals("WhiteboardUI", System.StringComparison.OrdinalIgnoreCase)
                // canvas name: equals using the text "whiteboardui", system string comparison ordinal ignore
                // case or canvas name: equals using the text "tabletoolmenu", system string comparison ordinal
                // ignore case.
                || canvas.name.Equals("TableToolMenu", System.StringComparison.OrdinalIgnoreCase);
        // finish the method is project world canvas block.
        }

        // method: try get controller input.
        // resolve the controller ray and configured select action. if no action is available, read the
        // physical right trigger directly.
        private bool TryGetControllerInput(out bool pressed, out Ray ray)
        // begin the method try get controller input block.
        {
            // set pressed to false.
            pressed = false;
            // call resolve xr ray origin: use the selected simulator hand or physical right-hand ray origin so
            // the drawing ray matches the intended controller.
            ResolveXrRayOrigin();
            // store origin as if ray origin exists, use ray origin, otherwise use transform.
            Transform origin = rayOrigin != null ? rayOrigin : transform;
            // set ray to a new ray using origin position, origin forward.
            ray = new Ray(origin.position, origin.forward);

            // the xr device simulator and the controller rig publish input
            // through the official xri input system action asset. reading the
            // same select action keeps cursor-driven editor testing and a
            // physical quest trigger on one path.
            // call resolve controller trigger action: find the selected hand interaction select action in the
            // configured or loaded input asset. select is an action name, so inspect its binding to identify
            // the physical button.
            ResolveControllerTriggerAction();
            // run the following statement only when controller trigger action exists.
            if (controllerTriggerAction != null)
            // begin the if statement block.
            {
                // run the following statement only when not (whether controller trigger action is enabled).
                if (!controllerTriggerAction.enabled)
                    // enable controller trigger action so it can receive input.
                    controllerTriggerAction.Enable();
                // set pressed to whether controller trigger action is currently pressed.
                pressed = controllerTriggerAction.IsPressed();
                // return true to the caller.
                return true;
            // finish the if statement block.
            }

            // run the following statement only when not (whether right controller is a valid device).
            if (!rightController.isValid)
                // set right controller to input devices: get device at xr node using xr node right hand.
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            // run the following statement only when not (whether right controller is a valid device).
            if (!rightController.isValid)
                // return false to the caller.
                return false;

            // run the following statement only when not (whether right controller supplies xr common usages
            // trigger button, storing it in pressed).
            if (!rightController.TryGetFeatureValue(XRCommonUsages.triggerButton, out pressed))
            // begin the if statement block.
            {
                // run the following statement only when whether right controller supplies xr common usages
                // trigger, storing it in the output value trigger value.
                if (rightController.TryGetFeatureValue(XRCommonUsages.trigger, out float triggerValue))
                    // set pressed to trigger value is greater than 0.55.
                    pressed = triggerValue > 0.55f;
            // finish the if statement block.
            }

            // return true to the caller.
            return true;
        // finish the method try get controller input block.
        }
    // finish the class xr whiteboard interactor block.
    }
// finish the namespace block.
}
