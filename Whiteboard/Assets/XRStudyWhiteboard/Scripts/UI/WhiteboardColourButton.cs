using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    public sealed class WhiteboardColourButton : MonoBehaviour
    {
        // tombol ini mengubah warna marker dan tanda pilihan aktif.
        [SerializeField] private WhiteboardColour colour;
        [SerializeField] private XRStudyWhiteboardManager manager;
        [SerializeField] private Image swatch;
        [SerializeField] private Outline selectionOutline;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(SelectColour);

            if (manager != null)
                manager.StateChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= Refresh;
        }

        public void Initialize(XRStudyWhiteboardManager whiteboardManager, WhiteboardColour targetColour, Image targetSwatch, Outline outline)
        {
            manager = whiteboardManager;
            colour = targetColour;
            swatch = targetSwatch;
            selectionOutline = outline;
        }

        private void SelectColour()
        {
            if (manager != null)
                manager.SetColour(colour);
        }

        private void Refresh()
        {
            if (swatch != null)
                swatch.color = XRStudyWhiteboardManager.GetColour(colour);
            if (selectionOutline != null)
                selectionOutline.enabled = manager != null && manager.CurrentColour == colour;
        }
    }
}
