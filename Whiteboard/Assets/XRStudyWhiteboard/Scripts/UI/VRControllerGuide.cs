using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace XRStudyWhiteboard
{
    /// <summary>A compact headset guide that stays readable without blocking the classroom.</summary>
    public sealed class VRControllerGuide : MonoBehaviour
    {
        [SerializeField, Min(3f)] private float initialDisplaySeconds = 14f;
        private Transform view;
        private GameObject guidePanel;
        private GameObject reopenHint;
        private XRInputDevice rightController;
        private bool rightPrimaryWasPressed;
        private float hideAt;
        private bool built;

        public void Initialize(Transform cameraTransform)
        {
            view = cameraTransform;
            if (isActiveAndEnabled && !built)
                Build();
        }

        private void Start()
        {
            if (view == null && Camera.main != null)
                view = Camera.main.transform;
            Build();
        }

        private void Build()
        {
            if (built || view == null)
                return;
            built = true;

            GameObject canvasObject = new GameObject("VR Controller Guide", typeof(RectTransform));
            canvasObject.transform.SetParent(view, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0.03f, 1.25f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * 0.00135f;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 440f);

            guidePanel = StudyUiStyle.CreatePanel(canvasObject.transform, "GuidePanel", Vector2.zero,
                new Vector2(780f, 440f), new Color(0.045f, 0.055f, 0.07f, 0.97f), true);
            StudyUiStyle.CreatePanel(guidePanel.transform, "Accent", new Vector2(0f, 211f),
                new Vector2(744f, 6f), StudyUiStyle.Accent);
            TMP_Text title = StudyUiStyle.CreateText(guidePanel.transform, "Title", "VR CONTROLLER GUIDE",
                new Vector2(-184f, 166f), new Vector2(372f, 46f), 28f,
                TextAlignmentOptions.Left, StudyUiStyle.Text);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text subtitle = StudyUiStyle.CreateText(guidePanel.transform, "Subtitle", "NAVIGATE, CHOOSE A TOOL, THEN WRITE",
                new Vector2(190f, 166f), new Vector2(328f, 38f), 15f,
                TextAlignmentOptions.Right, StudyUiStyle.Muted);
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;

            GameObject leftCard = StudyUiStyle.CreatePanel(guidePanel.transform, "LeftController",
                new Vector2(-188f, 11f), new Vector2(354f, 240f), StudyUiStyle.Surface, true);
            TMP_Text leftHeading = StudyUiStyle.CreateText(leftCard.transform, "Heading", "LEFT CONTROLLER",
                new Vector2(0f, 82f), new Vector2(306f, 34f), 22f,
                TextAlignmentOptions.Left, StudyUiStyle.Accent);
            leftHeading.fontStyle = FontStyles.Bold;
            StudyUiStyle.CreateText(leftCard.transform, "Controls",
                "X   GO TO WHITEBOARD\nY   NEXT TABLE\nSTICK   MOVE",
                new Vector2(0f, -22f), new Vector2(306f, 150f), 23f,
                TextAlignmentOptions.Left, StudyUiStyle.Text);

            GameObject rightCard = StudyUiStyle.CreatePanel(guidePanel.transform, "RightController",
                new Vector2(188f, 11f), new Vector2(354f, 240f), StudyUiStyle.Surface, true);
            TMP_Text rightHeading = StudyUiStyle.CreateText(rightCard.transform, "Heading", "RIGHT CONTROLLER",
                new Vector2(0f, 82f), new Vector2(306f, 34f), 22f,
                TextAlignmentOptions.Left, StudyUiStyle.Accent);
            rightHeading.fontStyle = FontStyles.Bold;
            StudyUiStyle.CreateText(rightCard.transform, "Controls",
                "TRIGGER   SELECT / WRITE\nSTICK   TURN\nA   CLOSE GUIDE",
                new Vector2(0f, -22f), new Vector2(306f, 150f), 23f,
                TextAlignmentOptions.Left, StudyUiStyle.Text);

            StudyUiStyle.CreateText(guidePanel.transform, "Footer",
                "Paper stays fixed while you write  •  Press A anytime to reopen this guide",
                new Vector2(0f, -178f), new Vector2(720f, 34f), 18f,
                TextAlignmentOptions.Center, StudyUiStyle.Muted);

            reopenHint = StudyUiStyle.CreatePanel(canvasObject.transform, "ReopenHint",
                new Vector2(300f, -185f), new Vector2(150f, 48f), new Color(0.045f, 0.055f, 0.07f, 0.90f), true);
            StudyUiStyle.CreateText(reopenHint.transform, "Label", "A  CONTROLS", Vector2.zero,
                new Vector2(130f, 32f), 17f, TextAlignmentOptions.Center, StudyUiStyle.Text);

            ShowGuide(true);
        }

        private void Update()
        {
            if (!built)
                Build();
            if (!built)
                return;

            if (!rightController.isValid)
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool pressed = rightController.isValid
                && rightController.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool value)
                && value;
            if (pressed && !rightPrimaryWasPressed)
                ShowGuide(!guidePanel.activeSelf);
            rightPrimaryWasPressed = pressed;

            if (guidePanel.activeSelf && Time.unscaledTime >= hideAt)
                ShowGuide(false);
        }

        private void ShowGuide(bool show)
        {
            guidePanel.SetActive(show);
            reopenHint.SetActive(!show);
            if (show)
                hideAt = Time.unscaledTime + initialDisplaySeconds;
        }
    }
}
