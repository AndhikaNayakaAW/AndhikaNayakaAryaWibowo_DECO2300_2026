// script overview: this script represents a destination for the player camera in world coordinates. its
// position is the intended eye position, and its horizontal forward direction gives the viewing direction. it
// finds or creates named anchors and draws a scene-view marker for inspection. locomotion uses the anchor to
// move the rig while preserving real headset pitch and roll.

// search: teleportation | seated view | camera destination | eye height.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // a destination for the player's camera, expressed in world space.
    // position is eye height; forward supplies yaw only, never hmd pitch or roll.
    // declare the seat anchor class based on unity behaviour.
    public sealed class SeatAnchor : MonoBehaviour
    // begin the class seat anchor block.
    {
        // expose camera position as transform position.
        public Vector3 CameraPosition => transform.position;
        // expose facing direction as the unit direction of transform forward projected onto the plane
        // perpendicular to the upward unit vector.
        public Vector3 FacingDirection => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // method: find or create.
        // reuse a named scene anchor or create one at the requested camera position facing horizontally.
        public static SeatAnchor FindOrCreate(Transform sceneRoot, string anchorName, Vector3 cameraPosition, Vector3 forward)
        // begin the method find or create block.
        {
            // store anchors as the child or resource the text "navigationanchors" found in scene root.
            Transform anchors = sceneRoot.Find("NavigationAnchors");
            // run the following statement only when anchors is missing.
            if (anchors == null)
            // begin the if statement block.
            {
                // set anchors to a new game object using the text "navigationanchors" transform.
                anchors = new GameObject("NavigationAnchors").transform;
                // parent anchors under scene root using local coordinates.
                anchors.SetParent(sceneRoot, false);
            // finish the if statement block.
            }

            // store existing as the child or resource anchor name found in anchors.
            Transform existing = anchors.Find(anchorName);
            // run the following statement only when existing exists.
            if (existing != null)
            // begin the if statement block.
            {
                // store authored as the the seat anchor component attached to this object component on
                // existing.
                SeatAnchor authored = existing.GetComponent<SeatAnchor>();
                // return if authored exists, use authored, otherwise use a newly attached add component<seat
                // anchor> component on existing game object to the caller.
                return authored != null ? authored : existing.gameObject.AddComponent<SeatAnchor>();
            // finish the if statement block.
            }

            // store anchor as a new game object using anchor name transform.
            Transform anchor = new GameObject(anchorName).transform;
            // parent anchor under anchors using local coordinates.
            anchor.SetParent(anchors, false);
            // set forward to forward projected onto the plane perpendicular to the upward unit vector.
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            // place anchor at camera position facing quaternion: look rotation using if the squared length of
            // forward is greater than 0.001, use forward, otherwise use the forward unit vector, the upward
            // unit vector.
            anchor.SetPositionAndRotation(cameraPosition,
                // supply quaternion: look rotation using if the squared length of forward is greater than
                // 0.001, use forward, otherwise use the forward unit vector, the upward unit vector to the
                // enclosing call.
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward : Vector3.forward, Vector3.up));
            // return a newly attached add component<seat anchor> component on anchor game object to the
            // caller.
            return anchor.gameObject.AddComponent<SeatAnchor>();
        // finish the method find or create block.
        }

        // method: on draw gizmos selected.
        // draw the destination and facing direction in the scene view when this anchor is selected.
        private void OnDrawGizmosSelected()
        // begin the method on draw gizmos selected block.
        {
            // set gizmos colour to a new colour using 0.2, 0.8, 1, 1.
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            // gizmos: draw wire sphere using camera position, 0.09.
            Gizmos.DrawWireSphere(CameraPosition, 0.09f);
            // gizmos: draw ray using camera position, facing direction multiplied by 0.35.
            Gizmos.DrawRay(CameraPosition, FacingDirection * 0.35f);
        // finish the method on draw gizmos selected block.
        }
    // finish the class seat anchor block.
    }
// finish the namespace block.
}
