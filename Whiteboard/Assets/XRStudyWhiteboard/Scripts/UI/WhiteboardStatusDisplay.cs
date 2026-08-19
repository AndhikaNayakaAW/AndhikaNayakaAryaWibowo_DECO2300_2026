using TMPro;
using UnityEngine;

namespace XRStudyWhiteboard
{
    public sealed class WhiteboardStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text toolText;
        [SerializeField] private TMP_Text colourText;
        [SerializeField] private TMP_Text inputText;
        [SerializeField] private TMP_Text drawingText;

        public void Initialize(TMP_Text toolLabel, TMP_Text colourLabel, TMP_Text inputLabel, TMP_Text drawingLabel)
        {
            toolText = toolLabel;
            colourText = colourLabel;
            inputText = inputLabel;
            drawingText = drawingLabel;
        }

        public void Refresh(XRStudyWhiteboardManager manager)
        {
            if (manager == null)
                return;

            if (toolText != null)
                toolText.text = "CURRENT TOOL\n<color=#9BE7FF>" + manager.CurrentTool.ToString().ToUpperInvariant() + "</color>";
            if (colourText != null)
                colourText.text = "COLOUR\n<color=#FFFFFF>" + manager.CurrentColour.ToString().ToUpperInvariant() + "</color>";
            if (inputText != null)
                inputText.text = "INPUT\nCONTROLLER / HANDS";
        }

        public void SetDrawingState(bool drawing)
        {
            if (drawingText != null)
                drawingText.text = "DRAWING\n" + (drawing ? "ACTIVE" : "READY");
        }
    }
}
