using System.Collections.Generic;
using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>Associates each detected desk with a fixed camera/head destination.</summary>
    public sealed class StudyTableTeleportPoint : MonoBehaviour
    {
        private static readonly List<StudyTableTeleportPoint> ActivePoints = new List<StudyTableTeleportPoint>();

        [SerializeField] private PaperNoteCanvas paper;
        [SerializeField] private SeatAnchor seatAnchor;
        [SerializeField, Min(0f)] private float headDistanceFromDeskEdge = 0.22f;
        [SerializeField, Min(0.1f)] private float eyeHeightAboveDesktop = 0.62f;
        [SerializeField] private int tableIndex;

        public static IReadOnlyList<StudyTableTeleportPoint> Points => ActivePoints;
        public PaperNoteCanvas Paper => paper;
        public SeatAnchor SeatAnchor => seatAnchor;
        public int TableIndex => tableIndex;
        public Vector3 TeleportPosition => seatAnchor != null ? seatAnchor.CameraPosition : transform.position;
        public Vector3 ViewTarget => paper != null ? paper.transform.position : transform.position;

        public void Initialize(PaperNoteCanvas paperNote)
        {
            // Compatibility for existing generated scenes; runtime classroom
            // setup supplies the actual fitted tabletop bounds below.
            Vector3 center = paperNote != null ? paperNote.transform.position : transform.position;
            Initialize(paperNote, new Bounds(center, new Vector3(0.9f, 0f, 0.65f)), ActivePoints.IndexOf(this) + 1);
        }

        public void Initialize(PaperNoteCanvas paperNote, Bounds deskBounds, int index)
        {
            paper = paperNote;
            tableIndex = index;
            if (seatAnchor != null)
                return;

            // Stationery sets inherit the imported classroom's forward-facing
            // desk layout. The head belongs just beyond the near tabletop edge,
            // above the chair seat, not 1.4 metres behind a movable sheet of paper.
            Vector3 towardChair = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float deskHalfDepth = Mathf.Abs(towardChair.x) * deskBounds.extents.x
                + Mathf.Abs(towardChair.z) * deskBounds.extents.z;
            Vector3 headPosition = deskBounds.center + towardChair * (deskHalfDepth + headDistanceFromDeskEdge);
            headPosition.y = deskBounds.max.y + eyeHeightAboveDesktop;
            seatAnchor = XRStudyWhiteboard.SeatAnchor.FindOrCreate(transform.root,
                "Table" + tableIndex.ToString("00") + "SeatAnchor", headPosition, -towardChair);
        }

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
