using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    public sealed class WhiteboardClearButton : MonoBehaviour
    {
        [SerializeField] private XRStudyWhiteboardManager manager;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(RequestClear);
        }

        public void Initialize(XRStudyWhiteboardManager whiteboardManager)
        {
            manager = whiteboardManager;
        }

        private void RequestClear()
        {
            if (manager != null)
                manager.RequestClear();
        }
    }
}
