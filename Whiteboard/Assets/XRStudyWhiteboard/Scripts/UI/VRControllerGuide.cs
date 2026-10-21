// script overview: this script creates the control guide shown inside the headset. the guide describes
// left-controller movement and teleportation, and right-controller selection, writing, turning, and the guide
// shortcut. the right a button closes or reopens it. the guide is attached to the camera and hides after its
// display period. it explains controls but does not implement drawing or teleportation itself.

// search: controller guide | left controller | right controller | x button | y button | a button.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make text mesh pro types available for readable interface labels.
using TMPro;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;
// make the types in unity engine.xr available to this script.
using UnityEngine.XR;
// use xr common usages as the local name for unity engine.xr.common usages.
using XRCommonUsages = UnityEngine.XR.CommonUsages;
// use xr input device as the local name for unity engine.xr.input device.
using XRInputDevice = UnityEngine.XR.InputDevice;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // a compact headset guide that stays readable without blocking the classroom.
    // declare the vr controller guide class based on unity behaviour.
    public sealed class VRControllerGuide : MonoBehaviour
    // begin the class vr controller guide block.
    {
        // initial display seconds: how long the guide remains visible before automatically hiding. keep
        // initial display seconds as float, initially 14. allow unity to save and configure this field in the
        // inspector.
        [SerializeField, Min(3f)] private float initialDisplaySeconds = 14f;
        // keep view as transform for later use.
        private Transform view;
        // keep guide panel as game object for later use.
        private GameObject guidePanel;
        // keep reopen hint as game object for later use.
        private GameObject reopenHint;
        // keep right controller as xr input device for later use.
        private XRInputDevice rightController;
        // right primary was pressed: the previous right-a state used to toggle the guide once per press. keep
        // right primary was pressed as bool for later use.
        private bool rightPrimaryWasPressed;
        // keep hide at as float for later use.
        private float hideAt;
        // keep built as bool for later use.
        private bool built;

        // method: initialize.
        // connect the supplied objects and prepare this component for its current role.
        public void Initialize(Transform cameraTransform)
        // begin the method initialize block.
        {
            // set view to camera transform.
            view = cameraTransform;
            // run the following statement only when is active and enabled and not (built).
            if (isActiveAndEnabled && !built)
                // call build: create the camera-attached guide with separate left and right controller cards
                // and an a-button reopen hint.
                Build();
        // finish the method initialize block.
        }

        // method: start.
        // unity calls this before the first normal update. finish setup that depends on other components being
        // ready.
        private void Start()
        // begin the method start block.
        {
            // run the following statement only when view is missing and camera main exists.
            if (view == null && Camera.main != null)
                // set view to camera main transform.
                view = Camera.main.transform;
            // call build: create the camera-attached guide with separate left and right controller cards and
            // an a-button reopen hint.
            Build();
        // finish the method start block.
        }

        // method: build.
        // create the camera-attached guide with separate left and right controller cards and an a-button
        // reopen hint.
        private void Build()
        // begin the method build block.
        {
            // run the following statement only when built or view is missing.
            if (built || view == null)
                // leave this method now without running the remaining statements.
                return;
            // set built to true.
            built = true;

            // store canvas object as a new game object using the text "vr controller guide", the type
            // information for rect transform.
            GameObject canvasObject = new GameObject("VR Controller Guide", typeof(RectTransform));
            // parent canvas object transform under view using local coordinates.
            canvasObject.transform.SetParent(view, false);
            // set canvas object transform local position to a new three-dimensional vector using 0, 0.03,
            // 1.25.
            canvasObject.transform.localPosition = new Vector3(0f, 0.03f, 1.25f);
            // set canvas object transform local rotation to quaternion identity.
            canvasObject.transform.localRotation = Quaternion.identity;
            // set canvas object transform local scale to unit scale on all three axes multiplied by 0.00135.
            canvasObject.transform.localScale = Vector3.one * 0.00135f;
            // store canvas as a newly attached add component<canvas> component on canvas object.
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            // set canvas render mode to render mode world space.
            canvas.renderMode = RenderMode.WorldSpace;
            // set canvas sorting order to 200.
            canvas.sortingOrder = 200;
            // store scaler as a newly attached add component<canvas scaler> component on canvas object.
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            // set scaler ui scale mode to canvas scaler scale mode constant pixel size.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            // set the the rect transform component attached to this object component on canvas object size
            // delta to a new two-dimensional vector using 780, 440.
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 440f);

            // set guide panel to study ui style: create panel using canvas object transform, the text
            // "guidepanel", the zero two-dimensional vector, a new two-dimensional vector using 780, 440, a
            // new colour using 0.045, 0.055, 0.07, 0.97, true.
            guidePanel = StudyUiStyle.CreatePanel(canvasObject.transform, "GuidePanel", Vector2.zero,
                // supply a new two-dimensional vector using 780, 440 to the enclosing call.
                new Vector2(780f, 440f), new Color(0.045f, 0.055f, 0.07f, 0.97f), true);
            // study ui style: create panel using guide panel transform, the text "accent", a new
            // two-dimensional vector using 0, 211, a new two-dimensional vector using 744, 6, study ui style
            // accent.
            StudyUiStyle.CreatePanel(guidePanel.transform, "Accent", new Vector2(0f, 211f),
                // supply a new two-dimensional vector using 744, 6 to the enclosing call.
                new Vector2(744f, 6f), StudyUiStyle.Accent);
            // store title as study ui style: create text using guide panel transform, the text "title", the
            // text "vr controller guide", a new two-dimensional vector using the negative of 184, 166, a new
            // two-dimensional vector using 372, 46, 28, text alignment options left, study ui style text.
            TMP_Text title = StudyUiStyle.CreateText(guidePanel.transform, "Title", "VR CONTROLLER GUIDE",
                // supply a new two-dimensional vector using the negative of 184, 166 to the enclosing call.
                new Vector2(-184f, 166f), new Vector2(372f, 46f), 28f,
                // supply text alignment options left to the enclosing call.
                TextAlignmentOptions.Left, StudyUiStyle.Text);
            // set title font style to font styles bold.
            title.fontStyle = FontStyles.Bold;
            // set title text wrapping mode to text wrapping modes no wrap.
            title.textWrappingMode = TextWrappingModes.NoWrap;
            // store subtitle as study ui style: create text using guide panel transform, the text "subtitle",
            // the text "navigate, choose a tool, then write", a new two-dimensional vector using 190, 166, a
            // new two-dimensional vector using 328, 38, 15, text alignment options right, study ui style
            // muted.
            TMP_Text subtitle = StudyUiStyle.CreateText(guidePanel.transform, "Subtitle", "NAVIGATE, CHOOSE A TOOL, THEN WRITE",
                // supply a new two-dimensional vector using 190, 166 to the enclosing call.
                new Vector2(190f, 166f), new Vector2(328f, 38f), 15f,
                // supply text alignment options right to the enclosing call.
                TextAlignmentOptions.Right, StudyUiStyle.Muted);
            // set subtitle text wrapping mode to text wrapping modes no wrap.
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;

            // store left card as study ui style: create panel using guide panel transform, the text
            // "leftcontroller", a new two-dimensional vector using the negative of 188, 11, a new
            // two-dimensional vector using 354, 240, study ui style surface, true.
            GameObject leftCard = StudyUiStyle.CreatePanel(guidePanel.transform, "LeftController",
                // supply a new two-dimensional vector using the negative of 188, 11 to the enclosing call.
                new Vector2(-188f, 11f), new Vector2(354f, 240f), StudyUiStyle.Surface, true);
            // store left heading as study ui style: create text using left card transform, the text "heading",
            // the text "left controller", a new two-dimensional vector using 0, 82, a new two-dimensional
            // vector using 306, 34, 22, text alignment options left, study ui style accent.
            TMP_Text leftHeading = StudyUiStyle.CreateText(leftCard.transform, "Heading", "LEFT CONTROLLER",
                // supply a new two-dimensional vector using 0, 82 to the enclosing call.
                new Vector2(0f, 82f), new Vector2(306f, 34f), 22f,
                // supply text alignment options left to the enclosing call.
                TextAlignmentOptions.Left, StudyUiStyle.Accent);
            // set left heading font style to font styles bold.
            leftHeading.fontStyle = FontStyles.Bold;
            // study ui style: create text using left card transform, the text "controls", the text "x go to
            // whiteboard / y next table / stick move", a new two-dimensional vector using 0, the negative of
            // 22, a new two-dimensional vector using 306, 150, 23, text alignment options left, study ui style
            // text.
            StudyUiStyle.CreateText(leftCard.transform, "Controls",
                // supply the text "x go to whiteboard / y next table / stick move" to the enclosing call.
                "X   GO TO WHITEBOARD\nY   NEXT TABLE\nSTICK   MOVE",
                // supply a new two-dimensional vector using 0, the negative of 22 to the enclosing call.
                new Vector2(0f, -22f), new Vector2(306f, 150f), 23f,
                // supply text alignment options left to the enclosing call.
                TextAlignmentOptions.Left, StudyUiStyle.Text);

            // store right card as study ui style: create panel using guide panel transform, the text
            // "rightcontroller", a new two-dimensional vector using 188, 11, a new two-dimensional vector
            // using 354, 240, study ui style surface, true.
            GameObject rightCard = StudyUiStyle.CreatePanel(guidePanel.transform, "RightController",
                // supply a new two-dimensional vector using 188, 11 to the enclosing call.
                new Vector2(188f, 11f), new Vector2(354f, 240f), StudyUiStyle.Surface, true);
            // store right heading as study ui style: create text using right card transform, the text
            // "heading", the text "right controller", a new two-dimensional vector using 0, 82, a new
            // two-dimensional vector using 306, 34, 22, text alignment options left, study ui style accent.
            TMP_Text rightHeading = StudyUiStyle.CreateText(rightCard.transform, "Heading", "RIGHT CONTROLLER",
                // supply a new two-dimensional vector using 0, 82 to the enclosing call.
                new Vector2(0f, 82f), new Vector2(306f, 34f), 22f,
                // supply text alignment options left to the enclosing call.
                TextAlignmentOptions.Left, StudyUiStyle.Accent);
            // set right heading font style to font styles bold.
            rightHeading.fontStyle = FontStyles.Bold;
            // study ui style: create text using right card transform, the text "controls", the text "trigger
            // select / write / stick turn / a close guide", a new two-dimensional vector using 0, the negative
            // of 22, a new two-dimensional vector using 306, 150, 23, text alignment options left, study ui
            // style text.
            StudyUiStyle.CreateText(rightCard.transform, "Controls",
                // supply the text "trigger select / write / stick turn / a close guide" to the enclosing call.
                "TRIGGER   SELECT / WRITE\nSTICK   TURN\nA   CLOSE GUIDE",
                // supply a new two-dimensional vector using 0, the negative of 22 to the enclosing call.
                new Vector2(0f, -22f), new Vector2(306f, 150f), 23f,
                // supply text alignment options left to the enclosing call.
                TextAlignmentOptions.Left, StudyUiStyle.Text);

            // study ui style: create text using guide panel transform, the text "footer", the text "paper
            // stays fixed while you write • press a anytime to reopen this guide", a new two-dimensional
            // vector using 0, the negative of 178, a new two-dimensional vector using 720, 34, 18, text
            // alignment options center, study ui style muted.
            StudyUiStyle.CreateText(guidePanel.transform, "Footer",
                // supply the text "paper stays fixed while you write • press a anytime to reopen this guide"
                // to the enclosing call.
                "Paper stays fixed while you write  •  Press A anytime to reopen this guide",
                // supply a new two-dimensional vector using 0, the negative of 178 to the enclosing call.
                new Vector2(0f, -178f), new Vector2(720f, 34f), 18f,
                // supply text alignment options center to the enclosing call.
                TextAlignmentOptions.Center, StudyUiStyle.Muted);

            // set reopen hint to study ui style: create panel using canvas object transform, the text
            // "reopenhint", a new two-dimensional vector using 300, the negative of 185, a new two-dimensional
            // vector using 150, 48, a new colour using 0.045, 0.055, 0.07, 0.9, true.
            reopenHint = StudyUiStyle.CreatePanel(canvasObject.transform, "ReopenHint",
                // supply a new two-dimensional vector using 300, the negative of 185 to the enclosing call.
                new Vector2(300f, -185f), new Vector2(150f, 48f), new Color(0.045f, 0.055f, 0.07f, 0.90f), true);
            // study ui style: create text using reopen hint transform, the text "label", the text "a
            // controls", the zero two-dimensional vector, a new two-dimensional vector using 130, 32, 17, text
            // alignment options center, study ui style text.
            StudyUiStyle.CreateText(reopenHint.transform, "Label", "A  CONTROLS", Vector2.zero,
                // supply a new two-dimensional vector using 130, 32 to the enclosing call.
                new Vector2(130f, 32f), 17f, TextAlignmentOptions.Center, StudyUiStyle.Text);

            // show guide using true.
            ShowGuide(true);
        // finish the method build block.
        }

        // method: update.
        // read new right-a presses to toggle the guide and hide it when the display timer expires.
        private void Update()
        // begin the method update block.
        {
            // run the following statement only when not (built).
            if (!built)
                // call build: create the camera-attached guide with separate left and right controller cards
                // and an a-button reopen hint.
                Build();
            // run the following statement only when not (built).
            if (!built)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when not (whether right controller is a valid device).
            if (!rightController.isValid)
                // set right controller to input devices: get device at xr node using xr node right hand.
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            // store pressed as whether right controller is a valid device and whether right controller
            // supplies xr common usages primary button, storing it in the output value value and value.
            bool pressed = rightController.isValid
                // whether right controller is a valid device and whether right controller supplies xr common
                // usages primary button, storing it in the output value value.
                && rightController.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool value)
                // whether right controller is a valid device and whether right controller supplies xr common
                // usages primary button, storing it in the output value value and value.
                && value;
            // run the following statement only when pressed and not (right primary was pressed).
            if (pressed && !rightPrimaryWasPressed)
                // show guide using not (guide panel active self).
                ShowGuide(!guidePanel.activeSelf);
            // set right primary was pressed to pressed.
            rightPrimaryWasPressed = pressed;

            // run the following statement only when guide panel active self and elapsed real seconds is at
            // least hide at.
            if (guidePanel.activeSelf && Time.unscaledTime >= hideAt)
                // show guide using false.
                ShowGuide(false);
        // finish the method update block.
        }

        // method: show guide.
        // show the full guide or the reopen hint and restart the display timer when opening.
        private void ShowGuide(bool show)
        // begin the method show guide block.
        {
            // set the active state of guide panel using show.
            guidePanel.SetActive(show);
            // set the active state of reopen hint using not (show).
            reopenHint.SetActive(!show);
            // run the following statement only when show.
            if (show)
                // set hide at to elapsed real seconds plus initial display seconds.
                hideAt = Time.unscaledTime + initialDisplaySeconds;
        // finish the method show guide block.
        }
    // finish the class vr controller guide block.
    }
// finish the namespace block.
}
