using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Priority-one controller interaction. The existing XRI ray remains the
    /// visible interaction ray; this component performs a focused physics ray
    /// only against the whiteboard collider.
    /// </summary>
    public sealed class XRWhiteboardInteractor : MonoBehaviour
    {
        [SerializeField] private WhiteboardCanvas canvas;
        [SerializeField] private WhiteboardDrawer drawer;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float maxRayDistance = 12f;
        [SerializeField] private bool useMouseFallbackInEditor = true;
        [SerializeField] private InputActionAsset controllerInputActions;
        [SerializeField] private GraphicRaycaster desktopUiRaycaster;
        // The line renderer already interpolates between UV samples. Keep the
        // editor pointer responsive instead of adding a visible trailing lag.
        [SerializeField, Range(0.1f, 1f)] private float desktopPointerSmoothing = 1f;
        [SerializeField] private float desktopReleaseGraceSeconds = 0.12f;
        [SerializeField] private float controllerReleaseGraceSeconds = 0.08f;

        private XRInputDevice rightController;
        private InputAction controllerTriggerAction;
        private XRStudyWhiteboardManager manager;
        private bool wasPressed;
        private PaperNoteCanvas activePaper;
        private EventSystem desktopEventSystem;
        private PointerEventData desktopPointerEventData;
        private readonly List<RaycastResult> desktopUiResults = new List<RaycastResult>();
        private readonly List<Button> desktopButtons = new List<Button>();
        private bool desktopUiClickHeld;
        private Camera gameplayCamera;
        private Vector2 desktopGameMousePosition;
        private bool hasDesktopGameMousePosition;
        private bool desktopMouseButtonHeld;
        private bool desktopRightMouseButtonHeld;
        private float desktopPressGraceTimer;
        private float controllerPressGraceTimer;
        private bool desktopEraseWasActive;
        private WhiteboardTool toolBeforeDesktopErase;
        private bool usingDesktopCursor;
        private Vector2 smoothedDesktopUv;
        private bool hasSmoothedDesktopUv;

        private void Awake()
        {
            if (rayOrigin == null)
                rayOrigin = transform;

            manager = FindFirstObjectByType<XRStudyWhiteboardManager>();
            ResolveDesktopUiRaycaster();
            ResolveControllerTriggerAction();
        }

        public void SetReferences(WhiteboardCanvas whiteboardCanvas, WhiteboardDrawer whiteboardDrawer, Transform origin)
        {
            canvas = whiteboardCanvas;
            drawer = whiteboardDrawer;
            rayOrigin = origin != null ? origin : transform;
        }

        public void ConfigureInputActions(InputActionAsset actions)
        {
            controllerInputActions = actions;
            ResolveControllerTriggerAction();
        }

        private void ResolveControllerTriggerAction()
        {
            if (controllerTriggerAction != null)
                return;

            if (controllerInputActions != null)
            {
                controllerTriggerAction = controllerInputActions.FindAction(
                    "XRI Right Interaction/Activate",
                    false);
            }

            // The scene builder serializes the shared XRI asset, but retain a
            // runtime fallback for scenes opened before that reference was
            // added. This also makes the script resilient to prefab changes.
            if (controllerTriggerAction == null)
            {
                InputActionAsset[] loadedAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                for (int i = 0; i < loadedAssets.Length; i++)
                {
                    InputAction candidate = loadedAssets[i].FindAction(
                        "XRI Right Interaction/Activate",
                        false);
                    if (candidate == null)
                        continue;

                    controllerInputActions = loadedAssets[i];
                    controllerTriggerAction = candidate;
                    break;
                }
            }

            if (controllerTriggerAction != null && !controllerTriggerAction.enabled)
                controllerTriggerAction.Enable();
        }

        private void Update()
        {
            if (canvas == null || drawer == null)
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                desktopUiClickHeld = false;

            Ray ray;
            bool pressed;
            bool desktopTesting = Application.isEditor
                || (Application.platform != RuntimePlatform.Android && !XRSettings.isDeviceActive);

            // Prefer the XR controller whenever the Device Simulator or a
            // real headset provides one. L Mouse in the Device Simulator then
            // follows the same trigger path as a physical controller.
            bool hasController = TryGetControllerInput(out pressed, out ray);
            if (hasController && !usingDesktopCursor)
            {
                if (pressed)
                    controllerPressGraceTimer = controllerReleaseGraceSeconds;
                else if (controllerPressGraceTimer > 0f)
                    controllerPressGraceTimer -= Time.unscaledDeltaTime;

                pressed |= controllerPressGraceTimer > 0f;
            }
            bool editorMouseAvailable = useMouseFallbackInEditor
                && desktopTesting
                && Mouse.current != null
                && Camera.main != null;

            // The shared XRI action asset can exist in the editor before the
            // simulator has published a usable virtual controller. In that
            // state TryGetControllerInput reports the action but the ray is
            // still at its initial pose, so a normal cursor click appears to
            // do nothing. Use the Game-view cursor while the virtual trigger
            // is not active; once the simulator/Quest trigger is active, keep
            // the controller ray as the source of truth.
            bool virtualControllerAim = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            if (editorMouseAvailable && !virtualControllerAim)
            {
                usingDesktopCursor = true;
                // Prefer the Game view's own IMGUI pointer coordinates. The
                // Input System position is display-global on macOS, while a
                // docked Game view needs coordinates relative to its viewport.
                // Using the wrong origin is what made cursor drawing jump and
                // miss samples around the board and paper.
                Vector2 mousePosition = hasDesktopGameMousePosition
                    ? new Vector2(
                        desktopGameMousePosition.x,
                        Screen.height - desktopGameMousePosition.y)
                    : Mouse.current.position.ReadValue();

                if (!hasDesktopGameMousePosition)
                    mousePosition.y = Screen.height - mousePosition.y;

                // XRUIInputModule's tracked-device raycaster is the correct
                // path for a real controller and the Device Simulator.  A
                // normal desktop cursor is a separate pointer, however, so
                // explicitly deliver its click to the world-space buttons.
                // Without this bridge the cursor can draw on the board but
                // colour/marker/eraser buttons appear inert.
                if (TryDispatchDesktopUiClick(mousePosition))
                {
                    EndDrawing();
                    wasPressed = false;
                    return;
                }

                Camera camera = GetGameplayCamera();
                if (camera == null)
                    return;
                ray = camera.ScreenPointToRay(mousePosition);
                bool desktopErase = desktopRightMouseButtonHeld
                    || (Mouse.current != null && Mouse.current.rightButton.isPressed);
                XRStudyWhiteboardManager desktopManager = manager != null
                    ? manager
                    : FindFirstObjectByType<XRStudyWhiteboardManager>();
                if (desktopManager != null)
                {
                    if (desktopErase && !desktopEraseWasActive)
                    {
                        toolBeforeDesktopErase = desktopManager.CurrentTool;
                        desktopManager.SetTool(WhiteboardTool.Eraser);
                    }
                    else if (!desktopErase && desktopEraseWasActive)
                    {
                        desktopManager.SetTool(toolBeforeDesktopErase);
                    }
                }
                desktopEraseWasActive = desktopErase;
                // macOS can report a short false gap in the Input System
                // button state while a CGEvent drag is still held. The
                // Game-view IMGUI latch is the reliable source for editor
                // drawing, so keep the stroke alive until MouseUp arrives.
                bool desktopPointerPressed = desktopMouseButtonHeld || Mouse.current.leftButton.isPressed;
                if (desktopPointerPressed)
                    desktopPressGraceTimer = desktopReleaseGraceSeconds;
                else if (desktopPressGraceTimer > 0f)
                    desktopPressGraceTimer -= Time.unscaledDeltaTime;

                // A docked Game view can deliver MouseUp one editor frame
                // before the Input System updates. Hold the same stroke for a
                // short grace period so that a circle does not become a row
                // of restarted dots.
                pressed = desktopPointerPressed
                    || desktopPressGraceTimer > 0f
                    || desktopErase;
            }
            else
            {
                usingDesktopCursor = false;
                hasSmoothedDesktopUv = false;
                desktopEraseWasActive = false;
            }

            if (PaperNoteCanvas.TryGetNearest(ray, maxRayDistance, out PaperNoteCanvas paper, out Vector2 paperUv))
            {
                paperUv = SmoothDesktopPoint(paperUv, pressed && wasPressed);
                paper.UpdateCursor(paperUv);
                if (activePaper != paper)
                {
                    if (activePaper != null)
                        activePaper.EndStroke();
                    activePaper = paper;
                }

                bool desktopPaperErase = desktopRightMouseButtonHeld
                    || (Mouse.current != null && Mouse.current.rightButton.isPressed);
                bool erasingPaper = desktopPaperErase || PaperTool.IsEraserActive;
                bool canWriteOnPaper = desktopPaperErase || PaperTool.IsPencilActive || PaperTool.IsEraserActive;
                if (pressed && canWriteOnPaper)
                    paper.DrawAtUV(paperUv, erasingPaper);
                else
                    paper.EndStroke();

                if (pressed && !wasPressed)
                    ControllerHaptics.PulseRightController();

                wasPressed = pressed;
                return;
            }

            if (activePaper != null)
            {
                activePaper.EndStroke();
                activePaper = null;
            }

            if (!canvas.TryGetUV(ray, maxRayDistance, out Vector2 uv))
            {
                if (wasPressed || !pressed)
                    drawer.EndStroke();
                wasPressed = pressed;
                return;
            }

            uv = SmoothDesktopPoint(uv, pressed && wasPressed);
            canvas.UpdateCursor(uv);
            if (pressed)
                drawer.DrawAtUV(uv);
            else
                drawer.EndStroke();

            if (pressed && !wasPressed)
                ControllerHaptics.PulseRightController();

            wasPressed = pressed;
        }

        private Vector2 SmoothDesktopPoint(Vector2 point, bool continueStroke)
        {
            if (!usingDesktopCursor)
            {
                hasSmoothedDesktopUv = false;
                return point;
            }

            if (!hasSmoothedDesktopUv || !continueStroke)
            {
                smoothedDesktopUv = point;
                hasSmoothedDesktopUv = true;
                return point;
            }

            if (desktopPointerSmoothing >= 0.999f)
            {
                smoothedDesktopUv = point;
                return point;
            }

            smoothedDesktopUv = Vector2.Lerp(smoothedDesktopUv, point, desktopPointerSmoothing);
            return smoothedDesktopUv;
        }

        private void OnGUI()
        {
            if (!Application.isEditor || Event.current == null)
                return;

            Event current = Event.current;
            if (current.type == EventType.MouseMove
                || current.type == EventType.MouseDown
                || current.type == EventType.MouseDrag
                || current.type == EventType.MouseUp)
            {
                desktopGameMousePosition = current.mousePosition;
                hasDesktopGameMousePosition = true;
            }

            if (current.type == EventType.MouseDown && current.button == 0)
                desktopMouseButtonHeld = true;
            else if (current.type == EventType.MouseUp && current.button == 0)
                desktopMouseButtonHeld = false;
            else if (current.type == EventType.MouseDown && current.button == 1)
                desktopRightMouseButtonHeld = true;
            else if (current.type == EventType.MouseUp && current.button == 1)
                desktopRightMouseButtonHeld = false;

            if (current.type != EventType.MouseDown || current.button != 0)
                return;

            ResolveDesktopUiRaycaster();
            if (desktopUiRaycaster == null)
                return;

            // OnGUI reports coordinates relative to the docked Game view.
            // Convert its top-left origin to the bottom-left origin used by
            // Camera.WorldToScreenPoint and the world-space canvas.
            Vector2 gameViewPoint = new Vector2(
                current.mousePosition.x,
                Screen.height - current.mousePosition.y);

            Button[] candidates = FindObjectsByType<Button>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Button bestButton = null;
            for (int i = 0; i < candidates.Length; i++)
            {
                Button candidate = candidates[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;
                Canvas canvas = candidate.transform.GetComponentInParent<Canvas>();
                if (!IsProjectWorldCanvas(canvas))
                    continue;
                RectTransform rect = candidate.transform as RectTransform;
                if (rect == null || !IsScreenPointInside(rect, gameViewPoint))
                    continue;

                if (bestButton == null || rect.rect.size.sqrMagnitude < (bestButton.transform as RectTransform).rect.size.sqrMagnitude)
                    bestButton = candidate;
            }

            if (bestButton == null)
                return;

            bestButton.onClick.Invoke();
            EndDrawing();
            current.Use();
        }

        private void EndDrawing()
        {
            if (activePaper != null)
            {
                activePaper.EndStroke();
                activePaper = null;
            }

            if (drawer != null)
                drawer.EndStroke();
        }

        private void ResolveDesktopUiRaycaster()
        {
            if (desktopUiRaycaster != null)
                return;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas candidate = canvases[i];
                if (candidate == null || candidate.renderMode != RenderMode.WorldSpace)
                    continue;
                if (!candidate.name.Equals("WhiteboardUI", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                desktopUiRaycaster = candidate.GetComponent<GraphicRaycaster>();
                if (desktopUiRaycaster == null)
                    desktopUiRaycaster = candidate.gameObject.AddComponent<GraphicRaycaster>();
                Camera camera = GetGameplayCamera();
                if (camera != null)
                    candidate.worldCamera = camera;

                Image panelImage = candidate.transform.Find("ToolPanel")?.GetComponent<Image>();
                if (panelImage != null)
                    panelImage.raycastTarget = false;
                Image confirmationOverlay = candidate.transform.Find("ToolPanel/ClearConfirmation")?.GetComponent<Image>();
                if (confirmationOverlay != null)
                    confirmationOverlay.raycastTarget = false;
                break;
            }

            desktopEventSystem = EventSystem.current;
        }

        private bool TryDispatchDesktopUiClick(Vector2 screenPosition)
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                return false;

            ResolveDesktopUiRaycaster();
            if (desktopUiRaycaster == null)
                return false;

            if (desktopEventSystem == null)
                desktopEventSystem = EventSystem.current;
            if (desktopEventSystem == null)
                return false;

            Camera camera = GetGameplayCamera();
            if (camera == null)
                return false;

            if (desktopPointerEventData == null)
                desktopPointerEventData = new PointerEventData(desktopEventSystem);
            desktopPointerEventData.Reset();
            desktopPointerEventData.position = screenPosition;
            desktopPointerEventData.button = PointerEventData.InputButton.Left;
            desktopPointerEventData.pointerId = -1;
            desktopUiResults.Clear();
            desktopUiRaycaster.Raycast(desktopPointerEventData, desktopUiResults);
            Button button = null;
            if (desktopUiResults.Count > 0)
            {
                GameObject hit = desktopUiResults[0].gameObject;
                button = hit != null ? hit.GetComponentInParent<Button>() : null;
            }

            // Some Unity editor versions do not feed a world-space
            // GraphicRaycaster the docked Game-view pointer coordinates even
            // when the camera ray uses them correctly.  Check the button
            // rectangles directly as a deterministic fallback for that
            // editor-only case.
            if (button == null && camera != null)
            {
                desktopButtons.Clear();
                Button[] candidates = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < candidates.Length; i++)
                {
                    Button candidate = candidates[i];
                    if (candidate == null || !candidate.isActiveAndEnabled)
                        continue;
                    Canvas canvas = candidate.transform.GetComponentInParent<Canvas>();
                    if (!IsProjectWorldCanvas(canvas))
                        continue;
                    RectTransform rect = candidate.transform as RectTransform;
                    if (rect == null)
                        continue;

                    if (!IsScreenPointInside(rect, screenPosition))
                        continue;

                    desktopButtons.Add(candidate);
                }

                if (desktopButtons.Count > 0)
                {
                    // The smallest containing rectangle is the most specific
                    // hit when a confirmation overlay is visible.
                    button = desktopButtons[0];
                    for (int i = 1; i < desktopButtons.Count; i++)
                    {
                        RectTransform current = desktopButtons[i].transform as RectTransform;
                        RectTransform best = button.transform as RectTransform;
                        if (current != null && best != null && current.rect.size.sqrMagnitude < best.rect.size.sqrMagnitude)
                            button = desktopButtons[i];
                    }
                }
            }

            if (button == null)
            {
                return false;
            }

            // Invoke only on the press edge. The explicit latch is more
            // reliable than wasPressedThisFrame when a CGEvent click enters
            // a docked Game view between editor frames.
            if (!desktopUiClickHeld)
            {
                button.onClick.Invoke();
                desktopUiClickHeld = true;
            }

            return true;
        }

        private bool IsScreenPointInside(RectTransform rect, Vector2 screenPosition)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(GetGameplayCamera(), corners[i]);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            return screenPosition.x >= minX
                && screenPosition.x <= maxX
                && screenPosition.y >= minY
                && screenPosition.y <= maxY;
        }

        private Camera GetGameplayCamera()
        {
            if (gameplayCamera != null)
                return gameplayCamera;

            if (rayOrigin != null)
                gameplayCamera = rayOrigin.root.GetComponentInChildren<Camera>(true);
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
            return gameplayCamera;
        }

        private static bool IsProjectWorldCanvas(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                return false;

            return canvas.name.Equals("WhiteboardUI", System.StringComparison.OrdinalIgnoreCase)
                || canvas.name.Equals("TableToolMenu", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetControllerInput(out bool pressed, out Ray ray)
        {
            pressed = false;
            Transform origin = rayOrigin != null ? rayOrigin : transform;
            ray = new Ray(origin.position, origin.forward);

            // The XR Device Simulator and the controller rig publish input
            // through the official XRI Input System action asset. Reading the
            // same Activate action keeps cursor-driven editor testing and a
            // physical Quest trigger on one path.
            ResolveControllerTriggerAction();
            if (controllerTriggerAction != null)
            {
                if (!controllerTriggerAction.enabled)
                    controllerTriggerAction.Enable();
                pressed = controllerTriggerAction.IsPressed();
                return true;
            }

            if (!rightController.isValid)
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightController.isValid)
                return false;

            if (!rightController.TryGetFeatureValue(XRCommonUsages.triggerButton, out pressed))
            {
                if (rightController.TryGetFeatureValue(XRCommonUsages.trigger, out float triggerValue))
                    pressed = triggerValue > 0.55f;
            }

            return true;
        }
    }
}
