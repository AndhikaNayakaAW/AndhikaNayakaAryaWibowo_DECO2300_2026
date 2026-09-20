using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XRStudyWhiteboard
{
    /// <summary>Always-open drawing controls beside the currently occupied desk.</summary>
    public sealed class StudyTableToolMenu : MonoBehaviour
    {
        private static readonly List<StudyTableToolMenu> ActiveMenus = new List<StudyTableToolMenu>();
        [SerializeField] private PaperNoteCanvas paper;
        private GameObject menuObject;
        private GameObject menuPanel;
        private TMP_Text selectedToolText;
        private Button pencilButton;
        private Button eraserButton;
        private Button hoveredButton;
        private bool directControllerClickHeld;
        private bool desktopClickHeld;
        private Bounds deskBounds;
        private bool hasDeskBounds;
        private Canvas menuCanvas;

        public static bool TryHandleDesktopScreenPoint(Vector2 screenPoint, Camera eventCamera, bool pressed)
        {
            if (eventCamera == null)
                return false;
            return TryHandleAnyRayInternal(eventCamera.ScreenPointToRay(screenPoint), pressed, true);
        }

        public static void EndDesktopPointer()
        {
            foreach (StudyTableToolMenu menu in ActiveMenus)
            {
                if (menu == null)
                    continue;
                menu.desktopClickHeld = false;
                menu.SetPointerFeedback(null, false);
            }
        }

        public static bool TryHandleAnyRay(Ray ray, bool pressed)
        {
            return TryHandleAnyRayInternal(ray, pressed, false);
        }

        /// <summary>Consumes a native XR UI hit without dispatching its click or pointer state again.</summary>
        public static bool ContainsAnyRay(Ray ray)
        {
            foreach (StudyTableToolMenu menu in ActiveMenus)
            {
                if (menu == null || menu.menuObject == null || !menu.menuObject.activeInHierarchy)
                    continue;
                Plane plane = new Plane(menu.menuObject.transform.forward, menu.menuObject.transform.position);
                if (!plane.Raycast(ray, out float distance) || distance < 0f || distance > 2.2f)
                    continue;
                RectTransform panel = (RectTransform)menu.menuPanel.transform;
                if (panel.rect.Contains(panel.InverseTransformPoint(ray.GetPoint(distance))))
                    return true;
            }
            return false;
        }

        private static bool TryHandleAnyRayInternal(Ray ray, bool pressed, bool desktop)
        {
            bool consumed = false;
            for (int i = ActiveMenus.Count - 1; i >= 0; i--)
            {
                StudyTableToolMenu menu = ActiveMenus[i];
                if (menu == null)
                {
                    ActiveMenus.RemoveAt(i);
                    continue;
                }
                if (!consumed && menu.TryHandleRay(ray, pressed, desktop))
                    consumed = true;
            }
            return consumed;
        }

        public void Initialize(PaperNoteCanvas paperNote)
        {
            paper = paperNote;
            BuildMenu(true);
        }

        public void Initialize(PaperNoteCanvas paperNote, Bounds tabletop)
        {
            Initialize(paperNote, tabletop, true);
        }

        /// <summary>Test seam for pointer checks that do not need an XR physics raycaster.</summary>
        public void Initialize(PaperNoteCanvas paperNote, Bounds tabletop, bool includeTrackedRaycaster)
        {
            deskBounds = tabletop;
            hasDeskBounds = true;
            paper = paperNote;
            BuildMenu(includeTrackedRaycaster);
        }

        private void OnEnable()
        {
            if (!ActiveMenus.Contains(this))
                ActiveMenus.Add(this);
            PaperTool.SelectionChanged += RefreshSelection;
        }

        private void OnDisable()
        {
            ActiveMenus.Remove(this);
            PaperTool.SelectionChanged -= RefreshSelection;
            SetPointerFeedback(null, false);
        }

        private void BuildMenu(bool includeTrackedRaycaster)
        {
            if (menuObject != null)
                return;
            menuObject = new GameObject("TableToolMenu", typeof(RectTransform));
            menuObject.transform.SetParent(transform, false);
            // Students face world -Z; their right is world -X. The panel's
            // nearest edge stays outside the paper's 0.55m footprint, and its
            // bottom edge stays above the tabletop. Aim the complete canvas
            // toward the intended seated head, so labels and hitboxes agree.
            Vector3 panelPosition = transform.position + new Vector3(-0.45f, 0.20f, 0.04f);
            Vector3 seatedHead = hasDeskBounds
                ? new Vector3(deskBounds.center.x, deskBounds.max.y + 0.62f, deskBounds.max.z + 0.22f)
                : transform.position + new Vector3(0f, 0.62f, 0.62f);
            menuObject.transform.SetPositionAndRotation(panelPosition, Quaternion.LookRotation(panelPosition - seatedHead, Vector3.up));
            menuObject.transform.localScale = Vector3.one * 0.00075f;
            menuCanvas = menuObject.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.WorldSpace;
            menuCanvas.sortingOrder = 20;
            menuCanvas.worldCamera = Camera.main;
            menuObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            menuObject.AddComponent<GraphicRaycaster>();
            if (includeTrackedRaycaster)
                menuObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            menuObject.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 400f);

            menuPanel = StudyUiStyle.CreatePanel(menuObject.transform, "FloatingToolPanel", Vector2.zero, new Vector2(380f, 400f), StudyUiStyle.Panel, true);
            StudyUiStyle.CreateText(menuPanel.transform, "MenuTitle", "DRAWING TOOLS", new Vector2(0f, 152f), new Vector2(330f, 38f), 27f, TextAlignmentOptions.Left, StudyUiStyle.Text).fontStyle = FontStyles.Bold;
            StudyUiStyle.CreateText(menuPanel.transform, "Instruction", "Choose a tool, then draw\non the paper.", new Vector2(0f, 101f), new Vector2(330f, 62f), 20f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            StudyUiStyle.CreateText(menuPanel.transform, "ToolHeading", "TOOL", new Vector2(0f, 49f), new Vector2(330f, 26f), 16f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            pencilButton = StudyUiStyle.CreateButton(menuPanel.transform, "Pencil", "PENCIL", new Vector2(-85f, 0f), new Vector2(158f, 62f), () => SelectTool(PaperToolKind.Pencil));
            eraserButton = StudyUiStyle.CreateButton(menuPanel.transform, "Eraser", "ERASER", new Vector2(85f, 0f), new Vector2(158f, 62f), () => SelectTool(PaperToolKind.Eraser));
            selectedToolText = StudyUiStyle.CreateText(menuPanel.transform, "SelectedTool", "Pencil selected", new Vector2(0f, -59f), new Vector2(330f, 36f), 21f, TextAlignmentOptions.Left, StudyUiStyle.Accent);
            StudyUiStyle.CreateText(menuPanel.transform, "ActionHeading", "ACTIONS", new Vector2(0f, -105f), new Vector2(330f, 26f), 16f, TextAlignmentOptions.Left, StudyUiStyle.Muted);
            StudyUiStyle.CreateButton(menuPanel.transform, "ClearPaper", "CLEAR PAPER", new Vector2(0f, -152f), new Vector2(330f, 58f), ClearPaper);
            RefreshSelection(PaperTool.SelectedKind);
        }

        private void LateUpdate()
        {
            if (menuObject == null)
                return;
            Camera camera = Camera.main;
            if (camera == null)
                return;
            if (menuCanvas.worldCamera == null)
                menuCanvas.worldCamera = camera;

            // Only the closest occupied desk presents controls. Sixteen full
            // panels otherwise obscure furniture and compete with the board.
            StudyTableToolMenu nearest = null;
            float nearestDistance = 1.1f * 1.1f;
            foreach (StudyTableToolMenu candidate in ActiveMenus)
            {
                if (candidate == null || candidate.paper == null)
                    continue;
                Vector3 delta = camera.transform.position - candidate.transform.position;
                if (delta.z < 0.08f || Mathf.Abs(delta.y) > 1.3f)
                    continue;
                delta.y = 0f;
                if (delta.sqrMagnitude < nearestDistance)
                {
                    nearestDistance = delta.sqrMagnitude;
                    nearest = candidate;
                }
            }
            bool visible = nearest == this;
            if (menuObject.activeSelf != visible)
            {
                menuObject.SetActive(visible);
                SetPointerFeedback(null, false);
                directControllerClickHeld = desktopClickHeld = false;
            }
        }

        private bool TryHandleRay(Ray ray, bool pressed, bool desktop)
        {
            if (menuObject == null || !menuObject.activeInHierarchy)
                return false;
            Plane plane = new Plane(menuObject.transform.forward, menuObject.transform.position);
            if (!plane.Raycast(ray, out float distance) || distance < 0f || distance > 2.2f)
            {
                SetPointerFeedback(null, false);
                if (desktop) desktopClickHeld = false; else directControllerClickHeld = false;
                return false;
            }
            Vector3 worldPoint = ray.GetPoint(distance);
            Button hit = null;
            foreach (Button button in menuObject.GetComponentsInChildren<Button>(false))
            {
                if (!button.isActiveAndEnabled || !button.interactable)
                    continue;
                RectTransform rect = (RectTransform)button.transform;
                if (rect.rect.Contains(rect.InverseTransformPoint(worldPoint)))
                {
                    hit = button;
                    break;
                }
            }
            bool held = desktop ? desktopClickHeld : directControllerClickHeld;
            SetPointerFeedback(hit, pressed);
            if (hit != null && pressed && !held)
                hit.onClick.Invoke();
            if (desktop) desktopClickHeld = pressed; else directControllerClickHeld = pressed;
            RectTransform panel = (RectTransform)menuPanel.transform;
            return panel.rect.Contains(panel.InverseTransformPoint(worldPoint));
        }

        private void SetPointerFeedback(Button target, bool pressed)
        {
            if (EventSystem.current == null)
                return;
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            if (hoveredButton != target && hoveredButton != null)
            {
                hoveredButton.OnPointerUp(pointer);
                hoveredButton.OnPointerExit(pointer);
            }
            if (target != null)
            {
                if (hoveredButton != target)
                    target.OnPointerEnter(pointer);
                if (pressed)
                    target.OnPointerDown(pointer);
                else
                    target.OnPointerUp(pointer);
            }
            hoveredButton = target;
        }

        private void SelectTool(PaperToolKind tool)
        {
            PaperTool.Select(tool);
            RefreshSelection(tool);
        }

        private void ClearPaper()
        {
            if (paper != null)
                paper.ClearNote();
            ControllerHaptics.PulseRightController();
        }

        private void RefreshSelection(PaperToolKind tool)
        {
            if (selectedToolText != null)
                selectedToolText.text = tool == PaperToolKind.Pencil ? "Pencil selected" : "Eraser selected";
            RefreshToolButton(pencilButton, tool == PaperToolKind.Pencil, "PENCIL");
            RefreshToolButton(eraserButton, tool == PaperToolKind.Eraser, "ERASER");
        }

        private static void RefreshToolButton(Button button, bool selected, string label)
        {
            if (button == null)
                return;
            StudyUiStyle.StyleButton(button, selected);
            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            text.text = selected ? label + "\n<size=13>ACTIVE</size>" : label;
        }
    }
}
