using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Extension point for the paper prototype's two-finger swipe. Direct
    /// Marker/Eraser UI buttons are the reliable functional fallback for this
    /// implementation; no unstable gesture detector is enabled by default.
    /// </summary>
    public sealed class HandToolSwitchGesture : MonoBehaviour
    {
        [SerializeField] private bool enabledForFutureIteration;

        public bool EnabledForFutureIteration => enabledForFutureIteration;
    }
}
