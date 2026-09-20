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
            // mengubah titik uv dari input menjadi satu alur goresan.
            if (canvas == null || manager == null)
                return;
            if (manager.IsClearConfirmationVisible || manager.IsPlaybackActive)
            {
                drawing = false;
                return;
            }

            canvas.UpdateCursor(uv);
            if (!drawing || !canvas.IsStrokeOpen)
            {
                drawing = true;
                canvas.BeginStroke(uv);
                return;
            }

            canvas.ContinueStroke(uv, trustedDesktopInput);
        }

        private void OnDisable()
        {
            EndStroke();
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
