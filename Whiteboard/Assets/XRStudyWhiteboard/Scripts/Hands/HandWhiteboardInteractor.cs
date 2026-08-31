using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Secondary hand path. The installed XR Hands package supplies the joint
    /// poses; a pinch starts a short ray from the index fingertip to the board.
    /// Controller drawing remains independent of this optional component.
    /// </summary>
    public sealed class HandWhiteboardInteractor : MonoBehaviour
    {
        // pinch tangan kanan adalah jalur tambahan untuk mulai menggambar.
        [SerializeField] private WhiteboardCanvas canvas;
        [SerializeField] private WhiteboardDrawer drawer;
        [SerializeField] private float pinchStartDistance = 0.025f;
        [SerializeField] private float pinchReleaseDistance = 0.04f;
        [SerializeField] private float maxRayDistance = 0.35f;

        private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
        private bool wasPinching;

        private void OnEnable()
        {
            SubsystemManager.GetSubsystems(handSubsystems);
        }

        public void SetReferences(WhiteboardCanvas whiteboardCanvas, WhiteboardDrawer whiteboardDrawer)
        {
            canvas = whiteboardCanvas;
            drawer = whiteboardDrawer;
        }

        private void Update()
        {
            // mencegah mouse dan hand simulator menggambar secara bersamaan di editor.
            // The editor Device Simulator can expose a hand subsystem while
            // the desktop mouse is already driving the controller test ray.
            // Running both writers at once produces the characteristic
            // vertical spikes over an otherwise correct mouse circle. Keep
            // this optional path for a real headset, where it is needed.
            // The editor Device Simulator can report an active XR runtime
            // even though the desktop/controller test is the intended input
            // source. Letting this secondary hand path run at the same time
            // creates the duplicate vertical strokes seen in the Game view.
            if (Application.isEditor)
            {
                if (wasPinching && drawer != null)
                    drawer.EndStroke();
                wasPinching = false;
                return;
            }

            if (canvas == null || drawer == null || handSubsystems.Count == 0)
                return;

            XRHand hand = default;
            bool tracked = false;
            for (int i = 0; i < handSubsystems.Count; i++)
            {
                if (handSubsystems[i] == null)
                    continue;

                XRHand rightHand = handSubsystems[i].rightHand;
                if (rightHand.isTracked)
                {
                    hand = rightHand;
                    tracked = true;
                    break;
                }
            }

            if (!tracked)
            {
                if (wasPinching)
                    drawer.EndStroke();
                wasPinching = false;
                return;
            }

            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            if (!indexTip.TryGetPose(out Pose indexPose) || !thumbTip.TryGetPose(out Pose thumbPose))
                return;

            float pinchDistance = Vector3.Distance(indexPose.position, thumbPose.position);
            bool pinching = wasPinching
                ? pinchDistance <= pinchReleaseDistance
                : pinchDistance <= pinchStartDistance;

            if (pinching)
            {
                Vector3 direction = (canvas.transform.position - indexPose.position).normalized;
                Ray ray = new Ray(indexPose.position, direction);
                if (canvas.TryGetUV(ray, maxRayDistance, out Vector2 uv))
                    drawer.DrawAtUV(uv);
                else
                    drawer.EndStroke();
            }
            else
            {
                drawer.EndStroke();
            }

            wasPinching = pinching;
        }
    }
}
