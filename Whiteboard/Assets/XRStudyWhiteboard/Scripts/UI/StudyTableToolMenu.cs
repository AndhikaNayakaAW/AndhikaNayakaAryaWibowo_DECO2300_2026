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
        private static readonly Color PanelColour = new Color(0.035f, 0.075f, 0.12f, 0.97f);
        private static readonly Color ButtonColour = new Color(0.09f, 0.14f, 0.19f, 1f);
        private static readonly Color AccentColour = new Color(0.25f, 0.75f, 0.92f, 1f);

        [SerializeField] private PaperNoteCanvas paper;

        private GameObject menuObject;
        private GameObject menuPanel;
        private TMP_Text selectedToolText;

        public void Initialize(PaperNoteCanvas paperNote)
        {
            paper = paperNote;
            BuildMenu();
        }

        private void OnEnable()
        {
            PaperTool.SelectionChanged += RefreshSelection;
        }

        private void OnDisable()
        {
            PaperTool.SelectionChanged -= RefreshSelection;
        }

        private void OnDestroy()
        {
            PaperTool.SelectionChanged -= RefreshSelection;
        }

        private void BuildMenu()
        {
            if (menuObject != null)
                return;

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            menuObject = new GameObject("TableToolMenu");
            menuObject.transform.SetParent(transform, false);
            menuObject.transform.localPosition = new Vector3(0.32f, 0.1f, 0.2f);
            // The top of the table is the most comfortable aiming surface for
            // both a Quest ray and the editor simulator's controller ray.
            menuObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            menuObject.transform.localScale = Vector3.one * 0.00115f;

            Canvas canvas = menuObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            menuObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            menuObject.AddComponent<GraphicRaycaster>();
            menuObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform canvasRect = menuObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(420f, 330f);

            menuPanel = CreatePanel(menuObject.transform, "FloatingToolPanel", new Vector2(0f, 75f), new Vector2(410f, 205f), PanelColour);
            CreateText(menuPanel.transform, "MenuTitle", "PAPER TOOLS", font, new Vector2(0f, 73f), new Vector2(360f, 40f), 23f, TextAlignmentOptions.Center, AccentColour);
            CreateButton(menuPanel.transform, "Pencil", "PENCIL", font, new Vector2(-125f, 7f), new Vector2(112f, 62f), () => SelectTool(PaperToolKind.Pencil));
            CreateButton(menuPanel.transform, "Eraser", "ERASER", font, new Vector2(0f, 7f), new Vector2(112f, 62f), () => SelectTool(PaperToolKind.Eraser));
            CreateButton(menuPanel.transform, "ClearPaper", "CLEAR PAPER", font, new Vector2(125f, 7f), new Vector2(112f, 62f), ClearPaper);
            selectedToolText = CreateText(menuPanel.transform, "SelectedTool", "PENCIL READY", font, new Vector2(0f, -70f), new Vector2(360f, 34f), 17f, TextAlignmentOptions.Center, Color.white);

            GameObject openButton = CreatePanel(menuObject.transform, "OpenToolsButton", new Vector2(0f, -90f), new Vector2(210f, 62f), ButtonColour);
            Button open = openButton.AddComponent<Button>();
            Image openImage = openButton.GetComponent<Image>();
            open.targetGraphic = openImage;
            SetButtonColours(open, ButtonColour);
            CreateText(openButton.transform, "Label", "TOOLS", font, Vector2.zero, new Vector2(190f, 52f), 20f, TextAlignmentOptions.Center, Color.white);
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
            textComponent.raycastTarget = false;
            return textComponent;
        }
    }
}
