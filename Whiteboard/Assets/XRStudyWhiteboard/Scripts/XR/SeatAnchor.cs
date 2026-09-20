using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// A destination for the player's camera, expressed in world space.
    /// Position is eye height; forward supplies yaw only, never HMD pitch or roll.
    /// </summary>
    public sealed class SeatAnchor : MonoBehaviour
    {
        public Vector3 CameraPosition => transform.position;
        public Vector3 FacingDirection => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        public static SeatAnchor FindOrCreate(Transform sceneRoot, string anchorName, Vector3 cameraPosition, Vector3 forward)
        {
            Transform anchors = sceneRoot.Find("NavigationAnchors");
            if (anchors == null)
            {
                anchors = new GameObject("NavigationAnchors").transform;
                anchors.SetParent(sceneRoot, false);
            }

            Transform existing = anchors.Find(anchorName);
            if (existing != null)
            {
                SeatAnchor authored = existing.GetComponent<SeatAnchor>();
                return authored != null ? authored : existing.gameObject.AddComponent<SeatAnchor>();
            }

            Transform anchor = new GameObject(anchorName).transform;
            anchor.SetParent(anchors, false);
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            anchor.SetPositionAndRotation(cameraPosition,
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward : Vector3.forward, Vector3.up));
            return anchor.gameObject.AddComponent<SeatAnchor>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
            Gizmos.DrawWireSphere(CameraPosition, 0.09f);
            Gizmos.DrawRay(CameraPosition, FacingDirection * 0.35f);
        }
    }
}
