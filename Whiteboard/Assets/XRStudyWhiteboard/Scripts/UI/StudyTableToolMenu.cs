using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Creates the small tool button that lives beside one paper and the
    /// floating pencil, eraser, and clear-paper menu opened by that button.
    /// The menu is generated at runtime because the number and positions of
    /// tabletops are detected from the imported classroom model.
    /// </summary>
    public sealed class StudyTableToolMenu : MonoBehaviour
    {
        private static readonly List<StudyTableToolMenu> ActiveMenus = new List<StudyTableToolMenu>();
        private static readonly Color PanelColour = new Color(0.035f, 0.075f, 0.12f, 0.97f);
        private static readonly Color ButtonColour = new Color(0.09f, 0.14f, 0.19f, 1f);
        private static readonly Color AccentColour = new Color(0.25f, 0.75f, 0.92f, 1f);

        [SerializeField] private PaperNoteCanvas paper;

        private GameObject menuObject;
        private GameObject menuPanel;
        private TMP_Text selectedToolText;
        private bool directControllerClickHeld;

        public static bool TryHandleAnyRay(Ray ray, bool pressed)
        {
            for (int i = ActiveMenus.Count - 1; i >= 0; i--)
            {
                StudyTableToolMenu menu = ActiveMenus[i];
                if (menu == null)
                {
                    ActiveMenus.RemoveAt(i);
                    continue;
                }

                if (menu.TryHandleRay(ray, pressed))
                    return true;
            }

            return false;
        }

        public void Initialize(PaperNoteCanvas paperNote)
        {
            paper = paperNote;
            if (!ActiveMenus.Contains(this))
                ActiveMenus.Add(this);
            BuildMenu();
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
        }

        private void OnDestroy()
        {
            ActiveMenus.Remove(this);
            PaperTool.SelectionChanged -= RefreshSelection;
        }

        private void BuildMenu()
        {
            if (menuObject != null)
                return;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            menuObject = new GameObject("TableToolMenu");
            menuObject.transform.SetParent(transform, false);
            // Put the button just beyond the paper's front-right corner. The
            // student teleport anchor approaches from +Z, so placing it at
            // -Z put the old button behind the paper and under the simulator
            // controller ray.
            menuObject.transform.localPosition = new Vector3(-0.38f, 0.1f, 0.28f);
            // The canvas lies flat on the tabletop.  Its readable side must
            // face the student sitting at +Z; the previous -90 rotation put
            // the screen's top edge away from the student and made TOOLS look
            // upside down in the headset/editor view.
            menuObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // World-space canvases on the tabletop are viewed from their
            // back side by the seated camera. Flip only the horizontal UI
            // axis so the readable side remains visible and TOOLS is not
            // mirrored, while preserving the table-facing ray plane.
            menuObject.transform.localScale = new Vector3(-0.00115f, 0.00115f, 0.00115f);

            Canvas canvas = menuObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            canvas.worldCamera = Camera.main;
            menuObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            GraphicRaycaster graphicRaycaster = menuObject.AddComponent<GraphicRaycaster>();
            graphicRaycaster.ignoreReversedGraphics = false;
            TrackedDeviceGraphicRaycaster trackedRaycaster = menuObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            trackedRaycaster.ignoreReversedGraphics = false;

            RectTransform canvasRect = menuObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(420f, 330f);

            menuPanel = CreatePanel(menuObject.transform, "FloatingToolPanel", new Vector2(0f, 84f), new Vector2(470f, 224f), PanelColour);
            CreateText(menuPanel.transform, "MenuTitle", "PAPER TOOLS", font, new Vector2(0f, 78f), new Vector2(410f, 40f), 23f, TextAlignmentOptions.Center, AccentColour);
            CreateButton(menuPanel.transform, "Pencil", "PENCIL", font, new Vector2(-145f, 8f), new Vector2(130f, 66f), () => SelectTool(PaperToolKind.Pencil));
            CreateButton(menuPanel.transform, "Eraser", "ERASER", font, new Vector2(0f, 8f), new Vector2(130f, 66f), () => SelectTool(PaperToolKind.Eraser));
            CreateButton(menuPanel.transform, "ClearPaper", "CLEAR PAPER", font, new Vector2(145f, 8f), new Vector2(130f, 66f), ClearPaper);
            selectedToolText = CreateText(menuPanel.transform, "SelectedTool", "PENCIL READY", font, new Vector2(0f, -76f), new Vector2(410f, 34f), 17f, TextAlignmentOptions.Center, Color.white);

            GameObject openButton = CreatePanel(menuObject.transform, "OpenToolsButton", new Vector2(0f, -104f), new Vector2(250f, 72f), ButtonColour);
            Button open = openButton.AddComponent<Button>();
            Image openImage = openButton.GetComponent<Image>();
            open.targetGraphic = openImage;
            SetButtonColours(open, ButtonColour);
            CreateText(openButton.transform, "Label", "TOOLS", font, Vector2.zero, new Vector2(230f, 60f), 20f, TextAlignmentOptions.Center, Color.white);
            open.onClick.AddListener(ToggleMenu);

            menuPanel.SetActive(false);
            RefreshSelection(PaperTool.SelectedKind);
        }

        private void ToggleMenu()
        {
            if (menuPanel == null)
                return;

            menuPanel.SetActive(!menuPanel.activeSelf);
            ControllerHaptics.PulseRightController();
        }

        private void LateUpdate()
        {
            if (menuObject == null)
                return;

            // Camera.main can be assigned after runtime XR startup. Keeping
            // it on the world canvas makes both the standard XRUI module and
            // the editor GraphicRaycaster use the same event camera.
            Canvas canvas = menuObject.GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }

        private bool TryHandleRay(Ray ray, bool pressed)
        {
            if (menuObject == null)
                return false;

            Canvas canvas = menuObject.GetComponent<Canvas>();
            RectTransform canvasRect = menuObject.GetComponent<RectTransform>();
            if (canvas == null || canvasRect == null)
                return false;

            Plane menuPlane = new Plane(menuObject.transform.forward, menuObject.transform.position);
            if (!menuPlane.Raycast(ray, out float distance) || distance < 0f)
            {
                directControllerClickHeld = false;
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(distance);

            Button[] buttons = menuObject.GetComponentsInChildren<Button>(false);
            Button hitButton = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !button.isActiveAndEnabled)
                    continue;

                RectTransform buttonRect = button.transform as RectTransform;
                if (buttonRect == null)
                    continue;

                Vector2 buttonPoint = buttonRect.InverseTransformPoint(worldPoint);
                // Require the ray-plane hit to be inside the actual button.
                // The old broad “near button” tolerance also matched rays
                // aimed at the paper, so the menu consumed every paper
                // stroke before PaperNoteCanvas could receive it.
                if (buttonRect.rect.Contains(buttonPoint))
                {
                    hitButton = button;
                    break;
                }
            }

            if (hitButton != null && pressed && !directControllerClickHeld)
                hitButton.onClick.Invoke();

            directControllerClickHeld = pressed;
            if (hitButton != null)
                return true;

            // A hidden menu must not block the paper. Only an open panel
            // consumes the empty space between its buttons.
            if (menuPanel != null && menuPanel.activeSelf)
            {
                RectTransform panelRect = menuPanel.transform as RectTransform;
                if (panelRect != null && panelRect.rect.Contains(panelRect.InverseTransformPoint(worldPoint)))
                    return true;
            }

            directControllerClickHeld = false;
            return false;
        }

        private void SelectTool(PaperToolKind tool)
        {
            PaperTool.Select(tool);
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        private void ClearPaper()
        {
            if (paper != null)
                paper.ClearNote();

            if (menuPanel != null)
                menuPanel.SetActive(false);
            ControllerHaptics.PulseRightController();
        }

        private void RefreshSelection(PaperToolKind tool)
        {
            if (selectedToolText != null)
                selectedToolText.text = tool == PaperToolKind.Pencil ? "PENCIL READY" : "ERASER READY";
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color colour)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = panel.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = name != "FloatingToolPanel";
            return panel;
        }

        private static void CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreatePanel(parent, name, position, size, ButtonColour);
            Button button = buttonObject.AddComponent<Button>();
            Image image = buttonObject.GetComponent<Image>();
            button.targetGraphic = image;
            SetButtonColours(button, ButtonColour);
            TMP_Text text = CreateText(buttonObject.transform, "Label", label, font, Vector2.zero, size - new Vector2(10f, 8f), 17f, TextAlignmentOptions.Center, Color.white);
            text.raycastTarget = false;
            button.onClick.AddListener(action);
        }

        private static void SetButtonColours(Button button, Color normal)
        {
            ColorBlock colours = button.colors;
            colours.normalColor = normal;
            colours.highlightedColor = Color.Lerp(normal, Color.white, 0.3f);
            colours.pressedColor = Color.Lerp(normal, Color.black, 0.2f);
            colours.selectedColor = colours.highlightedColor;
            colours.fadeDuration = 0.08f;
            button.colors = colours;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color colour)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.font = font;
            textComponent.fontSize = fontSize;
            textComponent.color = colour;
            textComponent.alignment = alignment;
            // The tabletop canvas is intentionally viewed from its back
            // side so it can sit above the paper without z-fighting. Flip
            // the glyph layer back to normal reading order; the button
            // rectangle itself remains in the correct raycast position.
            textComponent.transform.localScale = new Vector3(-1f, 1f, 1f);
            textComponent.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            textComponent.raycastTarget = false;
            return textComponent;
        }
    }
}
