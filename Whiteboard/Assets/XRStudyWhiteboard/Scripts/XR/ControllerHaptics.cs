// script overview: this helper requests a short vibration from the right controller. it first checks that the
// device exists and supports haptic impulses. tool selection and other interactions can reuse it without
// duplicating device checks.

// search: controller feedback | vibration | right controller | haptics.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in unity engine.xr available to this script.
using UnityEngine.XR;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // declare the controller haptics class.
    public static class ControllerHaptics
    // begin the class controller haptics block.
    {
        // method: pulse right controller.
        // find the right controller, check impulse support, and request a brief vibration on its first haptic
        // channel.
        public static void PulseRightController(float amplitude = 0.18f, float duration = 0.035f)
        // begin the method pulse right controller block.
        {
            // store device as input devices: get device at xr node using xr node right hand.
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            // run the following statement only when not (whether device is a valid device) or not (device: try
            // get haptic capabilities using the output value capabilities) or not (capabilities supports
            // impulse).
            if (!device.isValid || !device.TryGetHapticCapabilities(out HapticCapabilities capabilities) || !capabilities.supportsImpulse)
                // leave this method now without running the remaining statements.
                return;

            // device: send haptic impulse using 0, amplitude, duration.
            device.SendHapticImpulse(0u, amplitude, duration);
        // finish the method pulse right controller block.
        }
    // finish the class controller haptics block.
    }
// finish the namespace block.
}
