using System;
using UnityEngine;

namespace XRStudyWhiteboard
{
    public enum WhiteboardTool
    {
        Marker,
        Eraser
    }

    public enum WhiteboardColour
    {
        Black,
        Red,
        Blue,
        Green
    }

    /// <summary>
    /// Keeps the whiteboard state in one place so controllers, hands, and UI
    /// all use the same tool and colour selection.
    /// </summary>
    public sealed class XRStudyWhiteboardManager : MonoBehaviour
    {
        [SerializeField] private WhiteboardCanvas canvas;
        [SerializeField] private WhiteboardStatusDisplay statusDisplay;
        [SerializeField] private ClearBoardConfirmation clearConfirmation;
        [SerializeField] private WhiteboardTool startingTool = WhiteboardTool.Marker;
        [SerializeField] private WhiteboardColour startingColour = WhiteboardColour.Black;

        public WhiteboardTool CurrentTool { get; private set; }
        public WhiteboardColour CurrentColour { get; private set; }
        public bool IsClearConfirmationVisible => clearConfirmation != null && clearConfirmation.IsVisible;

        public event Action StateChanged;

        private void Awake()
        {
            CurrentTool = startingTool;
            CurrentColour = startingColour;

            if (canvas != null)
                canvas.SetManager(this);

            RefreshState();
        }

        public void SetTool(WhiteboardTool tool)
        {
            if (CurrentTool == tool)
                return;

            CurrentTool = tool;
            RefreshState();
        }

        public void SetColour(WhiteboardColour colour)
        {
            if (CurrentColour == colour)
                return;

            CurrentColour = colour;
            RefreshState();
            ControllerHaptics.PulseRightController();
        }

        public Color GetCurrentInkColour()
        {
            return GetColour(CurrentColour);
        }

        public static Color GetColour(WhiteboardColour colour)
        {
            switch (colour)
            {
                case WhiteboardColour.Red:
                    return new Color(0.88f, 0.08f, 0.08f);
                case WhiteboardColour.Blue:
                    return new Color(0.05f, 0.25f, 0.92f);
                case WhiteboardColour.Green:
                    return new Color(0.05f, 0.58f, 0.18f);
                default:
                    return new Color(0.025f, 0.03f, 0.04f);
            }
        }

        public void RequestClear()
        {
            if (clearConfirmation != null)
            {
                clearConfirmation.Show();
                ControllerHaptics.PulseRightController();
            }
        }

        public void ConfirmClear()
        {
            if (canvas != null)
                canvas.ClearBoard();

            if (clearConfirmation != null)
                clearConfirmation.Hide();

            ControllerHaptics.PulseRightController();
            RefreshState();
        }

        public void CancelClear()
        {
            if (clearConfirmation != null)
                clearConfirmation.Hide();
        }

        public void SetReferences(WhiteboardCanvas boardCanvas, WhiteboardStatusDisplay display, ClearBoardConfirmation confirmation)
        {
            canvas = boardCanvas;
            statusDisplay = display;
            clearConfirmation = confirmation;

            if (canvas != null)
                canvas.SetManager(this);

            RefreshState();
        }

        private void RefreshState()
        {
            if (statusDisplay != null)
                statusDisplay.Refresh(this);

            StateChanged?.Invoke();
        }
    }
}
