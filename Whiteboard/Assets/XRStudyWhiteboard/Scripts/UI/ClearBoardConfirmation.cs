using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    public sealed class ClearBoardConfirmation : MonoBehaviour
    {
        [SerializeField] private XRStudyWhiteboardManager manager;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);

            Hide();
        }

        public void Initialize(XRStudyWhiteboardManager whiteboardManager, Button cancel, Button confirm)
        {
            manager = whiteboardManager;
            cancelButton = cancel;
            confirmButton = confirm;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Cancel()
        {
            if (manager != null)
                manager.CancelClear();
            else
                Hide();
        }

        private void Confirm()
        {
            if (manager != null)
                manager.ConfirmClear();
            else
                Hide();
        }
    }
}
