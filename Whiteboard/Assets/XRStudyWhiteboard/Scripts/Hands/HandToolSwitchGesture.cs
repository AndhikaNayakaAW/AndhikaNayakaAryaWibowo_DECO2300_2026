// script overview: this script is a placeholder for a possible hand gesture that switches tools. it only
// stores and exposes a future-feature flag. it does not currently detect a swipe or change tools. the existing
// marker and eraser buttons provide the working tool selection.

// search: hand gesture | future feature | optional input.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // extension point for the paper prototype's two-finger swipe. direct
    // marker/eraser ui buttons are the reliable functional fallback for this
    // implementation; no unstable gesture detector is enabled by default.
    // declare the hand tool switch gesture class based on unity behaviour.
    public sealed class HandToolSwitchGesture : MonoBehaviour
    // begin the class hand tool switch gesture block.
    {
        // keep enabled for future iteration as bool for later use. allow unity to save and configure this
        // field in the inspector.
        [SerializeField] private bool enabledForFutureIteration;

        // expose enabled for future iteration as enabled for future iteration.
        public bool EnabledForFutureIteration => enabledForFutureIteration;
    // finish the class hand tool switch gesture block.
    }
// finish the namespace block.
}
