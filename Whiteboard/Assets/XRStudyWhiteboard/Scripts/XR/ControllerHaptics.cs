using UnityEngine.XR;

namespace XRStudyWhiteboard
{
    public static class ControllerHaptics
    {
        public static void PulseRightController(float amplitude = 0.18f, float duration = 0.035f)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid || !device.TryGetHapticCapabilities(out HapticCapabilities capabilities) || !capabilities.supportsImpulse)
                return;

            device.SendHapticImpulse(0u, amplitude, duration);
        }
    }
}
