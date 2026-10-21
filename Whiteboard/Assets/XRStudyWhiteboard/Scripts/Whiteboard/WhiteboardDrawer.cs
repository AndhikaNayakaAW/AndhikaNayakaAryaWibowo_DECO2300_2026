// script overview: this script connects incoming writing positions to the whiteboard canvas. it starts a
// stroke for the first point, continues an open stroke, and closes it when input ends. it blocks writing
// during clear confirmation and playback. both controller input and optional hand input use this shared
// drawing bridge.

// search: whiteboard writing | start stroke | continue stroke | end stroke.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // converts a sequence of uv points into strokes. input sources call this
    // class instead of duplicating marker and eraser logic.
    // declare the whiteboard drawer class based on unity behaviour.
    public sealed class WhiteboardDrawer : MonoBehaviour
    // begin the class whiteboard drawer block.
    {
        // canvas: the main board pixel surface that receives drawing operations. keep canvas as whiteboard
        // canvas for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardCanvas canvas;
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private XRStudyWhiteboardManager manager;
        // keep drawing as bool for later use.
        private bool drawing;

        // method: set references.
        // connect the existing scene objects used by this component rather than creating another copy of those
        // systems.
        public void SetReferences(WhiteboardCanvas whiteboardCanvas, XRStudyWhiteboardManager whiteboardManager)
        // begin the method set references block.
        {
            // set canvas to whiteboard canvas.
            canvas = whiteboardCanvas;
            // set manager to whiteboard manager.
            manager = whiteboardManager;
        // finish the method set references block.
        }

        // method: draw at uv.
        // start or continue a board stroke at these texture coordinates unless confirmation or playback blocks
        // live drawing.
        public void DrawAtUV(Vector2 uv)
        // begin the method draw at uv block.
        {
            // send uv to this writing surface for drawing.
            DrawAtUV(uv, false);
        // finish the method draw at uv block.
        }

        // method: draw at uv.
        // start or continue a board stroke at these texture coordinates unless confirmation or playback blocks
        // live drawing.
        public void DrawAtUV(Vector2 uv, bool trustedDesktopInput)
        // begin the method draw at uv block.
        {
            // convert incoming texture coordinates into one continuous stroke.
            // run the following statement only when canvas is missing or manager is missing.
            if (canvas == null || manager == null)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when manager is clear confirmation visible or manager is
            // playback active.
            if (manager.IsClearConfirmationVisible || manager.IsPlaybackActive)
            // begin the if statement block.
            {
                // set drawing to false.
                drawing = false;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // canvas: update cursor using uv.
            canvas.UpdateCursor(uv);
            // run the following statement only when not (drawing) or not (canvas is stroke open).
            if (!drawing || !canvas.IsStrokeOpen)
            // begin the if statement block.
            {
                // set drawing to true.
                drawing = true;
                // canvas: begin stroke using uv.
                canvas.BeginStroke(uv);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // canvas: continue stroke using uv, trusted desktop input.
            canvas.ContinueStroke(uv, trustedDesktopInput);
        // finish the method draw at uv block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
        // finish the method on disable block.
        }

        // method: end stroke.
        // close the current stroke and reset continuity so the next contact does not join an earlier line.
        public void EndStroke()
        // begin the method end stroke block.
        {
            // run the following statement only when not (drawing).
            if (!drawing)
                // leave this method now without running the remaining statements.
                return;

            // set drawing to false.
            drawing = false;
            // run the following statement only when canvas exists.
            if (canvas != null)
                // end the current stroke on canvas so later hits do not join it.
                canvas.EndStroke();
        // finish the method end stroke block.
        }
    // finish the class whiteboard drawer block.
    }
// finish the namespace block.
}
