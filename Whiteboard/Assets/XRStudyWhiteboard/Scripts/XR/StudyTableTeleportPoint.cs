// script overview: this script connects a desk paper to its seated camera anchor and table number. it
// calculates an eye position beyond the near desk edge using the actual desk bounds. enabled points register
// in a shared list so navigation can find and cycle through the tables. it defines the destination while
// locomotion performs the movement.

// search: teleportation | table destination | seated view | desk bounds.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // associates each detected desk with a fixed camera/head destination.
    // declare the study table teleport point class based on unity behaviour.
    public sealed class StudyTableTeleportPoint : MonoBehaviour
    // begin the class study table teleport point block.
    {
        // keep active points as list<study table teleport point>, initially a new list<study table teleport
        // point>.
        private static readonly List<StudyTableTeleportPoint> ActivePoints = new List<StudyTableTeleportPoint>();

        // paper: the individual desk sheet controlled by this component. keep paper as paper note canvas for
        // later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private PaperNoteCanvas paper;
        // keep seat anchor as seat anchor for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private SeatAnchor seatAnchor;
        // head distance from desk edge: the horizontal eye offset beyond the near tabletop edge in metres.
        // keep head distance from desk edge as float, initially 0.22. allow unity to save and configure this
        // field in the inspector.
        [SerializeField, Min(0f)] private float headDistanceFromDeskEdge = 0.22f;
        // eye height above desktop: the vertical distance from the tabletop to the seated eyes in metres. keep
        // eye height above desktop as float, initially 0.62. allow unity to save and configure this field in
        // the inspector.
        [SerializeField, Min(0.1f)] private float eyeHeightAboveDesktop = 0.62f;
        // keep table index as int for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private int tableIndex;

        // expose points as active points.
        public static IReadOnlyList<StudyTableTeleportPoint> Points => ActivePoints;
        // expose paper as paper.
        public PaperNoteCanvas Paper => paper;
        // expose seat anchor as seat anchor.
        public SeatAnchor SeatAnchor => seatAnchor;
        // expose table index as table index.
        public int TableIndex => tableIndex;
        // expose teleport position as if seat anchor exists, use seat anchor camera position, otherwise use
        // transform position.
        public Vector3 TeleportPosition => seatAnchor != null ? seatAnchor.CameraPosition : transform.position;
        // expose view target as if paper exists, use paper transform position, otherwise use transform
        // position.
        public Vector3 ViewTarget => paper != null ? paper.transform.position : transform.position;

        // method: initialize.
        // associate the paper and table number with this desk, then create a camera anchor above and just
        // beyond its near edge.
        public void Initialize(PaperNoteCanvas paperNote)
        // begin the method initialize block.
        {
            // compatibility for existing generated scenes; runtime classroom
            // setup supplies the actual fitted tabletop bounds below.
            // store center as if paper note exists, use paper note transform position, otherwise use transform
            // position.
            Vector3 center = paperNote != null ? paperNote.transform.position : transform.position;
            // initialize using paper note, a new bounds using center, a new three-dimensional vector using
            // 0.9, 0, 0.65, active points: index of using this plus 1.
            Initialize(paperNote, new Bounds(center, new Vector3(0.9f, 0f, 0.65f)), ActivePoints.IndexOf(this) + 1);
        // finish the method initialize block.
        }

        // method: initialize.
        // associate the paper and table number with this desk, then create a camera anchor above and just
        // beyond its near edge.
        public void Initialize(PaperNoteCanvas paperNote, Bounds deskBounds, int index)
        // begin the method initialize block.
        {
            // set paper to paper note.
            paper = paperNote;
            // set table index to index.
            tableIndex = index;
            // run the following statement only when seat anchor exists.
            if (seatAnchor != null)
                // leave this method now without running the remaining statements.
                return;

            // stationery sets inherit the imported classroom's forward-facing
            // desk layout. the head belongs just beyond the near tabletop edge,
            // above the chair seat, not 1.4 metres behind a movable sheet of paper.
            // store toward chair as the unit direction of transform forward projected onto the plane
            // perpendicular to the upward unit vector.
            Vector3 towardChair = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            // store desk half depth as the absolute value of toward chair x multiplied by desk bounds extents
            // x plus the absolute value of toward chair z multiplied by desk bounds extents z.
            float deskHalfDepth = Mathf.Abs(towardChair.x) * deskBounds.extents.x
                // the absolute value of toward chair x multiplied by desk bounds extents x plus the absolute
                // value of toward chair z multiplied by desk bounds extents z.
                + Mathf.Abs(towardChair.z) * deskBounds.extents.z;
            // store head position as desk bounds center plus toward chair multiplied by desk half depth plus
            // head distance from desk edge.
            Vector3 headPosition = deskBounds.center + towardChair * (deskHalfDepth + headDistanceFromDeskEdge);
            // set head position y to desk bounds max y plus eye height above desktop.
            headPosition.y = deskBounds.max.y + eyeHeightAboveDesktop;
            // set seat anchor to xr study whiteboard seat anchor: find or create using transform root, the
            // text "table" plus table index: to string using the text "00" plus the text "seatanchor", head
            // position, the negative of toward chair.
            seatAnchor = XRStudyWhiteboard.SeatAnchor.FindOrCreate(transform.root,
                // supply the text "table" plus table index: to string using the text "00" plus the text
                // "seatanchor" to the enclosing call.
                "Table" + tableIndex.ToString("00") + "SeatAnchor", headPosition, -towardChair);
        // finish the method initialize block.
        }

        // method: on enable.
        // register this component and attach the listeners it needs while it is active.
        private void OnEnable()
        // begin the method on enable block.
        {
            // run the following statement only when not (whether active points contains this).
            if (!ActivePoints.Contains(this))
                // append this to active points.
                ActivePoints.Add(this);
        // finish the method on enable block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // remove this from active points.
            ActivePoints.Remove(this);
        // finish the method on disable block.
        }
    // finish the class study table teleport point block.
    }
// finish the namespace block.
}
