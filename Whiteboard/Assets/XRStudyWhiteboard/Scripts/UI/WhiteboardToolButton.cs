using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    public sealed class WhiteboardToolButton : MonoBehaviour
    {
        [SerializeField] private WhiteboardTool tool;
        [SerializeField] private XRStudyWhiteboardManager manager;
        [SerializeField] private Outline selectionOutline;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(SelectTool);

            if (manager != null)
                manager.StateChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.StateChanged -= Refresh;
        }

        public void Initialize(XRStudyWhiteboardManager whiteboardManager, WhiteboardTool targetTool, Outline outline)
        {
            manager = whiteboardManager;
            tool = targetTool;
            selectionOutline = outline;
        }

        private void SelectTool()
        {
            if (manager != null)
                manager.SetTool(tool);
        }

        private void Refresh()
        {
            if (selectionOutline != null)
                selectionOutline.enabled = manager != null && manager.CurrentTool == tool;
        }
    }
}
