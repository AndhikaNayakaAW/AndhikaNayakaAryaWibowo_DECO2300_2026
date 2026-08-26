using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Converts a sequence of UV points into strokes. Input sources call this
    /// class instead of duplicating marker and eraser logic.
    /// </summary>
    public sealed class WhiteboardDrawer : MonoBehaviour
    {
        [SerializeField] private WhiteboardCanvas canvas;
        [SerializeField] private XRStudyWhiteboardManager manager;
        private bool drawing;

        public void SetReferences(WhiteboardCanvas whiteboardCanvas, XRStudyWhiteboardManager whiteboardManager)
        {
            canvas = whiteboardCanvas;
            manager = whiteboardManager;
        }

        public void DrawAtUV(Vector2 uv)
        {
            DrawAtUV(uv, false);
        }

        public void DrawAtUV(Vector2 uv, bool trustedDesktopInput)
        {
            if (canvas == null || manager == null || manager.IsClearConfirmationVisible)
                return;

            canvas.UpdateCursor(uv);
            if (!drawing)
            {
                drawing = true;
                canvas.BeginStroke(uv);
                return;
            }

            canvas.ContinueStroke(uv, trustedDesktopInput);
        }

        public void EndStroke()
        {
            if (!drawing)
                return;

            drawing = false;
            if (canvas != null)
                canvas.EndStroke();
        }
    }
}
