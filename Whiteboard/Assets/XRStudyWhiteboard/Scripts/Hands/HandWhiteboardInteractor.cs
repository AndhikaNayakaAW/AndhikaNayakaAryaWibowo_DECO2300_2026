// script overview: this script provides the optional right-hand pinch drawing path. it reads the thumb and
// index fingertip positions, checks whether the fingers are close enough, and points a short ray toward the
// whiteboard. it sends valid contact points to the shared drawer. this path is skipped in the editor so hand
// simulation does not draw over desktop input.

// search: hand tracking | pinch | optional drawing | right hand.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.subsystems implementation available to this script.
using UnityEngine.SubsystemsImplementation;
// make the types in unity engine.xr.hands available to this script.
using UnityEngine.XR.Hands;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // secondary hand path. the installed xr hands package supplies the joint
    // poses; a pinch starts a short ray from the index fingertip to the board.
    // controller drawing remains independent of this optional component.
    // declare the hand whiteboard interactor class based on unity behaviour.
    public sealed class HandWhiteboardInteractor : MonoBehaviour
    // begin the class hand whiteboard interactor block.
    {
        // right-hand pinch drawing is an optional input path.
        // canvas: the main board pixel surface that receives drawing operations. keep canvas as whiteboard
        // canvas for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardCanvas canvas;
        // drawer: the shared bridge that starts, continues, and ends whiteboard strokes. keep drawer as
        // whiteboard drawer for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private WhiteboardDrawer drawer;
        // keep pinch start distance as float, initially 0.025. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private float pinchStartDistance = 0.025f;
        // keep pinch release distance as float, initially 0.04. allow unity to save and configure this field
        // in the inspector.
        [SerializeField] private float pinchReleaseDistance = 0.04f;
        // max ray distance: the maximum permitted ray contact distance in metres. keep max ray distance as
        // float, initially 0.35. allow unity to save and configure this field in the inspector.
        [SerializeField] private float maxRayDistance = 0.35f;

        // keep hand subsystems as list<xr hand subsystem>, initially a new list<xr hand subsystem>.
        private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
        // keep was pinching as bool for later use.
        private bool wasPinching;

        // method: on enable.
        // register this component and attach the listeners it needs while it is active.
        private void OnEnable()
        // begin the method on enable block.
        {
            // subsystem manager: get subsystems using hand subsystems.
            SubsystemManager.GetSubsystems(handSubsystems);
        // finish the method on enable block.
        }

        // method: set references.
        // connect the existing scene objects used by this component rather than creating another copy of those
        // systems.
        public void SetReferences(WhiteboardCanvas whiteboardCanvas, WhiteboardDrawer whiteboardDrawer)
        // begin the method set references block.
        {
            // set canvas to whiteboard canvas.
            canvas = whiteboardCanvas;
            // set drawer to whiteboard drawer.
            drawer = whiteboardDrawer;
        // finish the method set references block.
        }

        // method: update.
        // unity calls this once per frame while the component is enabled. process the current input or state
        // described below.
        private void Update()
        // begin the method update block.
        {
            // prevent the editor mouse and simulated hand from drawing at the same time.
            // the editor device simulator can expose a hand subsystem while
            // the desktop mouse is already driving the controller test ray.
            // running both writers at once produces the characteristic
            // vertical spikes over an otherwise correct mouse circle. keep
            // this optional path for a real headset, where it is needed.
            // the editor device simulator can report an active xr runtime
            // even though the desktop/controller test is the intended input
            // source. letting this secondary hand path run at the same time
            // creates the duplicate vertical strokes seen in the game view.
            // run the following statement only when whether this is the unity editor.
            if (Application.isEditor)
            // begin the if statement block.
            {
                // run the following statement only when was pinching and drawer exists.
                if (wasPinching && drawer != null)
                    // end the current stroke on drawer so later hits do not join it.
                    drawer.EndStroke();
                // set was pinching to false.
                wasPinching = false;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when canvas is missing or drawer is missing or the number of
            // items in hand subsystems equals 0.
            if (canvas == null || drawer == null || handSubsystems.Count == 0)
                // leave this method now without running the remaining statements.
                return;

            // store hand as default.
            XRHand hand = default;
            // store tracked as false.
            bool tracked = false;
            // repeat this loop while i is less than the number of items in hand subsystems. increase i by one
            // after each pass.
            for (int i = 0; i < handSubsystems.Count; i++)
            // begin the for statement block.
            {
                // run the following statement only when the entry at i in hand subsystems is missing.
                if (handSubsystems[i] == null)
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // store right hand as the entry at i in hand subsystems right hand.
                XRHand rightHand = handSubsystems[i].rightHand;
                // run the following statement only when right hand is tracked.
                if (rightHand.isTracked)
                // begin the if statement block.
                {
                    // set hand to right hand.
                    hand = rightHand;
                    // set tracked to true.
                    tracked = true;
                    // leave the nearest loop or switch after finding the required result.
                    break;
                // finish the if statement block.
                }
            // finish the for statement block.
            }

            // run the following statement only when not (tracked).
            if (!tracked)
            // begin the if statement block.
            {
                // run the following statement only when was pinching.
                if (wasPinching)
                    // end the current stroke on drawer so later hits do not join it.
                    drawer.EndStroke();
                // set was pinching to false.
                wasPinching = false;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store index tip as hand: get joint using xr hand joint id index tip.
            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            // store thumb tip as hand: get joint using xr hand joint id thumb tip.
            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            // run the following statement only when not (index tip: try get pose using the output value index
            // pose) or not (thumb tip: try get pose using the output value thumb pose).
            if (!indexTip.TryGetPose(out Pose indexPose) || !thumbTip.TryGetPose(out Pose thumbPose))
                // leave this method now without running the remaining statements.
                return;

            // store pinch distance as the distance between index pose position and thumb pose position.
            float pinchDistance = Vector3.Distance(indexPose.position, thumbPose.position);
            // store pinching as if was pinching, use pinch distance is at most pinch release distance,
            // otherwise use pinch distance is at most pinch start distance.
            bool pinching = wasPinching
                // if was pinching, use pinch distance is at most pinch release distance, otherwise use pinch
                // distance is at most pinch start distance.
                ? pinchDistance <= pinchReleaseDistance
                // if was pinching, use pinch distance is at most pinch release distance, otherwise use pinch
                // distance is at most pinch start distance.
                : pinchDistance <= pinchStartDistance;

            // run the following statement only when pinching.
            if (pinching)
            // begin the if statement block.
            {
                // store direction as the unit direction of canvas transform position minus index pose
                // position.
                Vector3 direction = (canvas.transform.position - indexPose.position).normalized;
                // store ray as a new ray using index pose position, direction.
                Ray ray = new Ray(indexPose.position, direction);
                // run the following statement only when canvas: try get uv using ray, max ray distance, the
                // output value uv.
                if (canvas.TryGetUV(ray, maxRayDistance, out Vector2 uv))
                    // send uv to drawer for drawing.
                    drawer.DrawAtUV(uv);
                // handle the alternative when the preceding condition is false.
                else
                    // end the current stroke on drawer so later hits do not join it.
                    drawer.EndStroke();
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // end the current stroke on drawer so later hits do not join it.
                drawer.EndStroke();
            // finish the else clause block.
            }

            // set was pinching to pinching.
            wasPinching = pinching;
        // finish the method update block.
        }
    // finish the class hand whiteboard interactor block.
    }
// finish the namespace block.
}
