using System.Collections.Generic;
using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Runtime anchor for a detected student table. The locomotion helper
    /// uses it for editor shortcuts, while the floor TeleportationArea still
    /// provides the normal controller teleport path on Quest.
    /// </summary>
    public sealed class StudyTableTeleportPoint : MonoBehaviour
    {
        private static readonly List<StudyTableTeleportPoint> ActivePoints = new List<StudyTableTeleportPoint>();

        [SerializeField] private PaperNoteCanvas paper;
        [SerializeField] private float approachDistance = 1.4f;
        [SerializeField] private float floorHeight = 0f;

        public static IReadOnlyList<StudyTableTeleportPoint> Points => ActivePoints;
        public PaperNoteCanvas Paper => paper;

        public void Initialize(PaperNoteCanvas paperNote)
        {
            paper = paperNote;
        }

        public Vector3 TeleportPosition
        {
            get
            {
                // memberi posisi pendekatan agar kamera menghadap kertas.
                Vector3 forward = transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f)
                    forward = Vector3.forward;
                forward.Normalize();

                Vector3 position = paper != null ? paper.transform.position : transform.position;
                position += forward * approachDistance;
                position.y = floorHeight;
                return position;
            }
        }

        public Vector3 ViewTarget => paper != null
            ? paper.transform.position + Vector3.up * 0.04f
            : transform.position + Vector3.up;

        private void OnEnable()
        {
            if (!ActivePoints.Contains(this))
                ActivePoints.Add(this);
        }

        private void OnDisable()
        {
            ActivePoints.Remove(this);
        }
    }
}
