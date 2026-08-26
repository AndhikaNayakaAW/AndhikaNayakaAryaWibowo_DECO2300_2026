using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    public sealed class ClearBoardConfirmation : MonoBehaviour
    {
        [SerializeField] private XRStudyWhiteboardManager manager;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        private bool listenersBound;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            BindListeners();
            Hide();
        }

        public void Initialize(XRStudyWhiteboardManager whiteboardManager, Button cancel, Button confirm)
        {
            manager = whiteboardManager;
            cancelButton = cancel;
            confirmButton = confirm;
            BindListeners();
        }

        private void BindListeners()
        {
            if (listenersBound)
                return;

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);

            listenersBound = cancelButton != null || confirmButton != null;
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
