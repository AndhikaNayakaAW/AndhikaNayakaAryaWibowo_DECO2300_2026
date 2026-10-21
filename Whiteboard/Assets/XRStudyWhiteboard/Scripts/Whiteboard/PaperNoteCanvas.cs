// script overview: this script owns the drawing texture for one desk paper. it converts a ray intersection on
// the top of the paper into a two-dimensional writing position, then paints pencil or eraser strokes into
// pixels. it smooths small input fluctuations, rejects large tracking jumps, joins nearby samples, and uploads
// the changed texture once per frame. each paper keeps its own ink. it also positions the writing crosshair.

// search: paper writing | table writing | pencil | eraser | texture | smoothing.
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
    // small quest-friendly writing surface used by the student desk papers.
    // it intentionally shares the same ray/trigger workflow as the main
    // whiteboard, but keeps each student's notes on a separate texture.
    // each desk paper has a separate drawing texture and notes.
    // declare the paper note canvas class based on unity behaviour.
    public sealed class PaperNoteCanvas : MonoBehaviour
    // begin the class paper note canvas block.
    {
        // keep active notes as list<paper note canvas>, initially a new list<paper note canvas>.
        private static readonly List<PaperNoteCanvas> ActiveNotes = new List<PaperNoteCanvas>();

        // paper renderer: the renderer used to display this sheet and position its writing overlay. keep paper
        // renderer as renderer for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Renderer paperRenderer;
        // paper collider: the collision shape used to calculate contact on the paper top plane. keep paper
        // collider as collider for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Collider paperCollider;
        // paper world size: the physical sheet width and depth used to scale the paper brush correctly. keep
        // paper world size as two-dimensional vector, initially a new two-dimensional vector using 0.55, 0.38.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private Vector2 paperWorldSize = new Vector2(0.55f, 0.38f);
        // texture width: the drawing texture width in pixels. keep texture width as int, initially 768. allow
        // unity to save and configure this field in the inspector.
        [SerializeField] private int textureWidth = 768;
        // texture height: the drawing texture height in pixels. keep texture height as int, initially 512.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private int textureHeight = 512;
        // paper uses a fine pencil line, deliberately smaller than the
        // marker used by whiteboardcanvas. this keeps a table note readable
        // instead of making it look like a second whiteboard.
        // pencil size: the physical paper pencil diameter in metres, smaller than the board marker. keep
        // pencil size as float, initially 0.01. allow unity to save and configure this field in the inspector.
        [SerializeField] private float pencilSize = 0.01f;
        // eraser size: the physical eraser diameter in metres. keep eraser size as float, initially 0.032.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private float eraserSize = 0.032f;
        // interpolation spacing: the maximum preferred gap between interpolated points in normalised texture
        // coordinates. keep interpolation spacing as float, initially 0.0005. allow unity to save and
        // configure this field in the inspector.
        [SerializeField] private float interpolationSpacing = 0.0005f;
        // maximum interpolation steps: the safety limit on work used to connect one pair of samples. keep
        // maximum interpolation steps as int, initially 4096. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private int maximumInterpolationSteps = 4096;
        // maximum uv jump: the largest accepted tracked-input jump before breaking stroke continuity. keep
        // maximum uv jump as float, initially 0.08. allow unity to save and configure this field in the
        // inspector.
        [SerializeField, Range(0.02f, 1f)] private float maximumUvJump = 0.08f;
        // stroke point smoothing: how strongly the filtered point follows the latest accepted candidate. keep
        // stroke point smoothing as float, initially 0.9. allow unity to save and configure this field in the
        // inspector.
        [SerializeField, Range(0.35f, 1f)] private float strokePointSmoothing = 0.9f;

        // note texture: the runtime texture displaying only this paper ink. keep note texture as texture2 d
        // for later use.
        private Texture2D noteTexture;
        // pixels: the cpu-side colour buffer that the brush changes before texture upload. keep pixels as
        // 32-bit colour[] for later use.
        private Color32[] pixels;
        // previous uv: the last painted texture coordinate used as the start of the next connected segment.
        // keep previous uv as two-dimensional vector for later use.
        private Vector2 previousUv;
        // has previous point: whether a previous point is available for joining the next sample. keep has
        // previous point as bool for later use.
        private bool hasPreviousPoint;
        // input before last uv: the raw sample before the latest one, used by the three-sample spike filter.
        // keep input before last uv as two-dimensional vector for later use.
        private Vector2 inputBeforeLastUv;
        // last input uv: the latest raw contact sample used to close a stroke exactly at release. keep last
        // input uv as two-dimensional vector for later use.
        private Vector2 lastInputUv;
        // filtered input uv: the previous smoothed sample used as the starting value for input blending. keep
        // filtered input uv as two-dimensional vector for later use.
        private Vector2 filteredInputUv;
        // input sample count: how many recent input samples are available to the smoothing filter. keep input
        // sample count as int for later use.
        private int inputSampleCount;
        // reacquire anchor uv: the new contact position used while confirming stable tracking after a jump.
        // keep reacquire anchor uv as two-dimensional vector for later use.
        private Vector2 reacquireAnchorUv;
        // reacquire sample count: the number of nearby samples collected before accepting reacquired contact.
        // keep reacquire sample count as int for later use.
        private int reacquireSampleCount;
        // previous stroke was erasing: whether the current paper stroke uses the eraser rather than the
        // pencil. keep previous stroke was erasing as bool for later use.
        private bool previousStrokeWasErasing;
        // texture dirty: whether the cpu pixel buffer has changes waiting to be uploaded this frame. keep
        // texture dirty as bool for later use.
        private bool textureDirty;
        // latest input is desktop: whether the latest sample came from the trusted desktop cursor rather than
        // tracked input. keep latest input is desktop as bool for later use.
        private bool latestInputIsDesktop;
        // writing surface renderer: the renderer for the paper overlay whose texture coordinates match the
        // drawing contact mapping. keep writing surface renderer as renderer for later use.
        private Renderer writingSurfaceRenderer;
        // writing surface material: the owned material used by the visible paper writing overlay. keep writing
        // surface material as material for later use.
        private Material writingSurfaceMaterial;
        // keep crosshair as transform for later use.
        private Transform crosshair;
        // keep crosshair renderers as list<renderer>, initially a new list<renderer>.
        private readonly List<Renderer> crosshairRenderers = new List<Renderer>();

        // expose notes as active notes.
        public static IReadOnlyList<PaperNoteCanvas> Notes => ActiveNotes;

        // method: configure.
        // connect this sheet renderer and collider, store a safe physical size, and initialise its own drawing
        // surface.
        public void Configure(Renderer renderer, Collider collider, Vector2 worldSize)
        // begin the method configure block.
        {
            // set paper renderer to renderer.
            paperRenderer = renderer;
            // set paper collider to collider.
            paperCollider = collider;
            // set paper world size to a new two-dimensional vector using the larger of 0.05 and world size x,
            // the larger of 0.05 and world size y.
            paperWorldSize = new Vector2(Mathf.Max(0.05f, worldSize.x), Mathf.Max(0.05f, worldSize.y));
            // call ensure crosshair: create the writing crosshair once and reuse it to show the current
            // writing location.
            EnsureCrosshair();
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
        // finish the method configure block.
        }

        // method: configure writing sizes.
        // limit the physical pencil and eraser sizes so the pencil stays fine and the eraser remains wider.
        public void ConfigureWritingSizes(float finePencilSize, float wideEraserSize)
        // begin the method configure writing sizes block.
        {
            // set pencil size to fine pencil size clamped between 0.001 and 0.05.
            pencilSize = Mathf.Clamp(finePencilSize, 0.001f, 0.05f);
            // set eraser size to wide eraser size clamped between pencil size and 0.15.
            eraserSize = Mathf.Clamp(wideEraserSize, pencilSize, 0.15f);
        // finish the method configure writing sizes block.
        }

        // method: on enable.
        // register this component and attach the listeners it needs while it is active.
        private void OnEnable()
        // begin the method on enable block.
        {
            // run the following statement only when not (whether active notes contains this).
            if (!ActiveNotes.Contains(this))
                // append this to active notes.
                ActiveNotes.Add(this);
            // subscribe on tool selection changed to paper tool selection changed so it runs when the event is
            // raised.
            PaperTool.SelectionChanged += OnToolSelectionChanged;
        // finish the method on enable block.
        }

        // method: on disable.
        // stop active interaction and detach the state that should not remain active while this component is
        // disabled.
        private void OnDisable()
        // begin the method on disable block.
        {
            // unsubscribe on tool selection changed from paper tool selection changed so this object no longer
            // receives the event.
            PaperTool.SelectionChanged -= OnToolSelectionChanged;
            // remove this from active notes.
            ActiveNotes.Remove(this);
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
        // finish the method on disable block.
        }

        // method: on tool selection changed.
        // end the current paper stroke whenever the selected pencil or eraser changes.
        private void OnToolSelectionChanged(PaperToolKind tool)
        // begin the method on tool selection changed block.
        {
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
        // finish the method on tool selection changed block.
        }

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
            // run the following statement only when paper renderer is missing.
            if (paperRenderer == null)
                // set paper renderer to the renderer component attached to this object.
                paperRenderer = GetComponent<Renderer>();
            // run the following statement only when paper collider is missing.
            if (paperCollider == null)
                // set paper collider to the collider component attached to this object.
                paperCollider = GetComponent<Collider>();

            // call ensure crosshair: create the writing crosshair once and reuse it to show the current
            // writing location.
            EnsureCrosshair();
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
        // finish the method awake block.
        }

        // method: on destroy.
        // release owned resources or event listeners when unity destroys this component.
        private void OnDestroy()
        // begin the method on destroy block.
        {
            // destroy owned using writing surface material.
            DestroyOwned(writingSurfaceMaterial);
            // destroy owned using note texture.
            DestroyOwned(noteTexture);
            // visit each renderer in crosshair renderers.
            foreach (Renderer renderer in crosshairRenderers)
                // run the following statement only when renderer exists.
                if (renderer != null) DestroyOwned(renderer.sharedMaterial);
        // finish the method on destroy block.
        }

        // method: destroy owned.
        // release an object created by this drawing system. use delayed destruction in play mode and immediate
        // destruction in editor mode.
        private static void DestroyOwned(Object owned)
        // begin the method destroy owned block.
        {
            // run the following statement only when owned is missing.
            if (owned == null) return;
            // run the following statement only when whether the application is running in play mode.
            if (Application.isPlaying) Destroy(owned);
            // handle the alternative when the preceding condition is false.
            else DestroyImmediate(owned);
        // finish the method destroy owned block.
        }

        // method: late update.
        // unity calls this after normal updates. use the latest input and camera state for the work described
        // below.
        private void LateUpdate()
        // begin the method late update block.
        {
            // run the following statement only when texture dirty.
            if (textureDirty)
                // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
                // pending-change flag.
                ApplyTexture();
        // finish the method late update block.
        }

        // method: initialize surface.
        // create the runtime texture and pixel buffer if needed, then connect the visible writing surface.
        public void InitializeSurface()
        // begin the method initialize surface block.
        {
            // run the following statement only when note texture exists.
            if (noteTexture != null)
            // begin the if statement block.
            {
                // call ensure writing surface: build a dedicated visible writing mesh above the paper with
                // texture coordinates that match the contact mapping.
                EnsureWritingSurface();
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // set texture width to the larger of 128 and texture width.
            textureWidth = Mathf.Max(128, textureWidth);
            // set texture height to the larger of 128 and texture height.
            textureHeight = Mathf.Max(128, textureHeight);
            // set note texture to a new texture2 d using texture width, texture height, texture format rgba32,
            // false, false.
            noteTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, false)
            // begin the object initializer expression values.
            {
                // set name to the text "student paper note runtime texture".
                name = "Student Paper Note Runtime Texture",
                // set filter mode to filter mode bilinear.
                filterMode = FilterMode.Bilinear,
                // set wrap mode to texture wrap mode clamp.
                wrapMode = TextureWrapMode.Clamp
            // finish the object initializer expression values.
            };

            // set pixels to a new 32-bit colour[texture width * texture height] array.
            pixels = new Color32[textureWidth * textureHeight];
            // fill using colour white.
            Fill(Color.white);
            // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
            // pending-change flag.
            ApplyTexture();
            // call ensure writing surface: build a dedicated visible writing mesh above the paper with texture
            // coordinates that match the contact mapping.
            EnsureWritingSurface();
        // finish the method initialize surface block.
        }

        // method: try get uv.
        // find whether the ray reaches this writing surface and return its normalised two-dimensional texture
        // position.
        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        // begin the method try get uv block.
        {
            // return try get paper intersection using ray, max distance, , uv to the caller.
            return TryGetPaperIntersection(ray, maxDistance, out _, out uv);
        // finish the method try get uv block.
        }

        // method: draw at uv.
        // paint or erase this paper at the input position. start a new stroke when necessary and join only
        // valid nearby points.
        public void DrawAtUV(Vector2 uv, bool erasing)
        // begin the method draw at uv block.
        {
            // send uv to this writing surface for drawing.
            DrawAtUV(uv, erasing, false);
        // finish the method draw at uv block.
        }

        // method: draw at uv.
        // paint or erase this paper at the input position. start a new stroke when necessary and join only
        // valid nearby points.
        public void DrawAtUV(Vector2 uv, bool erasing, bool trustedDesktopInput)
        // begin the method draw at uv block.
        {
            // write on the paper top surface with the pencil or eraser size.
            // set latest input is desktop to trusted desktop input.
            latestInputIsDesktop = trustedDesktopInput;
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
            // run the following statement only when not (has previous point).
            if (!hasPreviousPoint)
            // begin the if statement block.
            {
                // run the following statement only when reacquire sample count is greater than 0 and not (try
                // reacquire surface using uv).
                if (reacquireSampleCount > 0 && !TryReacquireSurface(uv))
                // begin the if statement block.
                {
                    // leave this method now without running the remaining statements.
                    return;
                // finish the if statement block.
                }

                // set previous stroke was erasing to erasing.
                previousStrokeWasErasing = erasing;
                // set input sample count to 0.
                inputSampleCount = 0;
                // stamp using uv, erasing.
                Stamp(uv, erasing);
                // set previous uv to uv.
                previousUv = uv;
                // set has previous point to true.
                hasPreviousPoint = true;
                // set input before last uv to uv.
                inputBeforeLastUv = uv;
                // set last input uv to uv.
                lastInputUv = uv;
                // set filtered input uv to uv.
                filteredInputUv = uv;
                // set input sample count to 1.
                inputSampleCount = 1;
                // set reacquire sample count to 0.
                reacquireSampleCount = 0;
                // mark the pixel buffer as changed so late update uploads the new drawing this frame.
                textureDirty = true;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // run the following statement only when previous stroke was erasing differs from erasing.
            if (previousStrokeWasErasing != erasing)
            // begin the if statement block.
            {
                // set previous stroke was erasing to erasing.
                previousStrokeWasErasing = erasing;
                // set input sample count to 0.
                inputSampleCount = 0;
                // stamp using uv, erasing.
                Stamp(uv, erasing);
                // set previous uv to uv.
                previousUv = uv;
                // set input before last uv to uv.
                inputBeforeLastUv = uv;
                // set last input uv to uv.
                lastInputUv = uv;
                // set filtered input uv to uv.
                filteredInputUv = uv;
                // set input sample count to 1.
                inputSampleCount = 1;
                // mark the pixel buffer as changed so late update uploads the new drawing this frame.
                textureDirty = true;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store distance as the distance between previous uv and uv.
            float distance = Vector2.Distance(previousUv, uv);
            // do not connect two unrelated controller hits with a long
            // segment when the thin paper collider is briefly missed.
            // run the following statement only when not (trusted desktop input) and distance is greater than
            // maximum uv jump.
            if (!trustedDesktopInput && distance > maximumUvJump)
            // begin the if statement block.
            {
                // break stroke continuity so a later contact starts a separate line.
                hasPreviousPoint = false;
                // set input sample count to 0.
                inputSampleCount = 0;
                // set reacquire anchor uv to uv.
                reacquireAnchorUv = uv;
                // set reacquire sample count to 1.
                reacquireSampleCount = 1;
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store filtered uv as filter input point using uv.
            Vector2 filteredUv = FilterInputPoint(uv);

            // set distance to the distance between previous uv and filtered uv.
            distance = Vector2.Distance(previousUv, filteredUv);
            // set uv to filtered uv.
            uv = filteredUv;

            // store brush diameter as if erasing, use eraser size, otherwise use pencil size.
            float brushDiameter = erasing ? eraserSize : pencilSize;
            // store brush radius in uv as brush diameter multiplied by 0.5 divided by the larger of paper
            // world size x and paper world size y.
            float brushRadiusInUv = (brushDiameter * 0.5f)
                // brush diameter multiplied by 0.5 divided by the larger of paper world size x and paper world
                // size y.
                / Mathf.Max(paperWorldSize.x, paperWorldSize.y);
            // store spacing as the smaller of the larger of 0.00025 and interpolation spacing and the larger
            // of 0.00025 and brush radius in uv multiplied by 0.4.
            float spacing = Mathf.Min(
                // supply the larger of 0.00025 and interpolation spacing to the enclosing call.
                Mathf.Max(0.00025f, interpolationSpacing),
                // supply the larger of 0.00025 and brush radius in uv multiplied by 0.4 to the enclosing call.
                Mathf.Max(0.00025f, brushRadiusInUv * 0.4f));
            // store steps as distance divided by spacing rounded upward to an integer clamped between 1 and
            // the larger of 1 and maximum interpolation steps.
            int steps = Mathf.Clamp(
                // supply distance divided by spacing rounded upward to an integer to the enclosing call.
                Mathf.CeilToInt(distance / spacing),
                // supply 1 to the enclosing call.
                1,
                // supply the larger of 1 and maximum interpolation steps to the enclosing call.
                Mathf.Max(1, maximumInterpolationSteps));
            // store segment start as previous uv.
            Vector2 segmentStart = previousUv;
            // repeat this loop while i is at most steps. increase i by one after each pass.
            for (int i = 1; i <= steps; i++)
            // begin the for statement block.
            {
                // store segment end as an interpolated value from previous uv toward uv using i divided by
                // steps converted to float.
                Vector2 segmentEnd = Vector2.Lerp(previousUv, uv, i / (float)steps);
                // stamp segment using segment start, segment end, erasing.
                StampSegment(segmentStart, segmentEnd, erasing);
                // set segment start to segment end.
                segmentStart = segmentEnd;
            // finish the for statement block.
            }

            // set previous uv to uv.
            previousUv = uv;
            // mark the pixel buffer as changed so late update uploads the new drawing this frame.
            textureDirty = true;
        // finish the method draw at uv block.
        }

        // method: try reacquire surface.
        // require a short run of nearby samples after a large jump before accepting a new stroke location.
        private bool TryReacquireSurface(Vector2 uv)
        // begin the method try reacquire surface block.
        {
            // run the following statement only when reacquire sample count is at most 0.
            if (reacquireSampleCount <= 0)
            // begin the if statement block.
            {
                // set reacquire anchor uv to uv.
                reacquireAnchorUv = uv;
                // set reacquire sample count to 1.
                reacquireSampleCount = 1;
                // report that stable contact has not yet been confirmed, so no new stroke point should be
                // painted.
                return false;
            // finish the if statement block.
            }

            // run the following statement only when the distance between reacquire anchor uv and uv is greater
            // than maximum uv jump.
            if (Vector2.Distance(reacquireAnchorUv, uv) > maximumUvJump)
            // begin the if statement block.
            {
                // set reacquire anchor uv to uv.
                reacquireAnchorUv = uv;
                // set reacquire sample count to 1.
                reacquireSampleCount = 1;
                // report that stable contact has not yet been confirmed, so no new stroke point should be
                // painted.
                return false;
            // finish the if statement block.
            }

            // set reacquire anchor uv to uv.
            reacquireAnchorUv = uv;
            // increase reacquire sample count by one.
            reacquireSampleCount++;
            // run the following statement only when reacquire sample count is less than 3.
            if (reacquireSampleCount < 3)
                // report that stable contact has not yet been confirmed, so no new stroke point should be
                // painted.
                return false;

            // set reacquire sample count to 0.
            reacquireSampleCount = 0;
            // report that enough nearby samples confirm stable contact and drawing can resume.
            return true;
        // finish the method try reacquire surface block.
        }

        // method: update cursor.
        // place the crosshair at the current writing coordinates and adjust its appearance for the active
        // tool.
        public void UpdateCursor(Vector2 uv)
        // begin the method update cursor block.
        {
            // call ensure crosshair: create the writing crosshair once and reuse it to show the current
            // writing location.
            EnsureCrosshair();
            // run the following statement only when crosshair is missing.
            if (crosshair == null)
                // leave this method now without running the remaining statements.
                return;

            // store box as paper collider treated as box collider.
            BoxCollider box = paperCollider as BoxCollider;
            // store center as if box exists, use box center, otherwise use the zero vector.
            Vector3 center = box != null ? box.center : Vector3.zero;
            // store size as if box exists, use box size, otherwise use unit scale on all three axes.
            Vector3 size = box != null ? box.size : Vector3.one;
            // store lift as 0.003 divided by the larger of 0.0001 and the absolute value of paper renderer
            // transform lossy scale y.
            float lift = 0.003f / Mathf.Max(0.0001f, Mathf.Abs(paperRenderer.transform.lossyScale.y));
            // set crosshair local position to a new three-dimensional vector using center x plus uv x minus
            // 0.5 multiplied by size x, center y plus size y multiplied by 0.5 plus lift, center z plus uv y
            // minus 0.5 multiplied by size z.
            crosshair.localPosition = new Vector3(
                // supply center x plus uv x minus 0.5 multiplied by size x to the enclosing call.
                center.x + (uv.x - 0.5f) * size.x,
                // supply center y plus size y multiplied by 0.5 plus lift to the enclosing call.
                center.y + size.y * 0.5f + lift,
                // supply center z plus uv y minus 0.5 multiplied by size z to the enclosing call.
                center.z + (uv.y - 0.5f) * size.z);

            // store colour as a new colour using 0.05, 0.9, 1, 1.
            Color colour = new Color(0.05f, 0.9f, 1f, 1f);
            // repeat this loop while i is less than the number of items in crosshair renderers. increase i by
            // one after each pass.
            for (int i = 0; i < crosshairRenderers.Count; i++)
                // run the following statement only when the entry at i in crosshair renderers exists.
                if (crosshairRenderers[i] != null)
                    // set the entry at i in crosshair renderers material colour to colour.
                    crosshairRenderers[i].material.color = colour;

            // store diameter as if paper tool is eraser active, use eraser size, otherwise use pencil size.
            float diameter = PaperTool.IsEraserActive ? eraserSize : pencilSize;
            // store span as the larger of 0.018 and diameter multiplied by 2.2.
            float span = Mathf.Max(0.018f, diameter * 2.2f);
            // store thickness as the larger of 0.0015 and diameter multiplied by 0.18.
            float thickness = Mathf.Max(0.0015f, diameter * 0.18f);
            // set crosshair bar scale using 0, span, thickness, 0.002.
            SetCrosshairBarScale(0, span, thickness, 0.002f);
            // set crosshair bar scale using 1, thickness, 0.002, span.
            SetCrosshairBarScale(1, thickness, 0.002f, span);
        // finish the method update cursor block.
        }

        // method: end stroke.
        // close the current stroke and reset continuity so the next contact does not join an earlier line.
        public void EndStroke()
        // begin the method end stroke block.
        {
            // call close stroke to latest input: finish the stroke at the latest raw input point so smoothing
            // does not leave a gap at the endpoint.
            CloseStrokeToLatestInput();
            // break stroke continuity so a later contact starts a separate line.
            hasPreviousPoint = false;
            // set input sample count to 0.
            inputSampleCount = 0;
            // set reacquire sample count to 0.
            reacquireSampleCount = 0;
        // finish the method end stroke block.
        }

        // method: clear note.
        // end writing and reset this sheet to white without changing other desk papers.
        public void ClearNote()
        // begin the method clear note block.
        {
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
            // fill using colour white.
            Fill(Color.white);
            // clear the pending texture-change flag because this code is handling the upload directly.
            textureDirty = false;
            // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
            // pending-change flag.
            ApplyTexture();
        // finish the method clear note block.
        }

        // method: try get nearest.
        // check the active desk papers and return the nearest valid ray intersection and matching texture
        // position.
        public static bool TryGetNearest(Ray ray, float maxDistance, out PaperNoteCanvas note, out Vector2 uv)
        // begin the method try get nearest block.
        {
            // set note to no object.
            note = null;
            // set uv to default.
            uv = default;
            // store nearest distance as max distance.
            float nearestDistance = maxDistance;

            // repeat this loop while i is at least 0. decrease i by one after each pass.
            for (int i = ActiveNotes.Count - 1; i >= 0; i--)
            // begin the for statement block.
            {
                // store candidate as the entry at i in active notes.
                PaperNoteCanvas candidate = ActiveNotes[i];
                // run the following statement only when candidate is missing.
                if (candidate == null)
                // begin the if statement block.
                {
                    // remove the entry at i from active notes.
                    ActiveNotes.RemoveAt(i);
                    // skip the rest of this loop pass and move to the next entry.
                    continue;
                // finish the if statement block.
                }

                // run the following statement only when not (candidate: try get paper intersection using ray,
                // nearest distance, the output value candidate distance, the output value candidate uv).
                if (!candidate.TryGetPaperIntersection(
                        // supply ray to the enclosing call.
                        ray,
                        // supply nearest distance to the enclosing call.
                        nearestDistance,
                        // supply the output value candidate distance to the enclosing call.
                        out float candidateDistance,
                        // supply the output value candidate uv to the enclosing call.
                        out Vector2 candidateUv))
                    // skip the rest of this loop pass and move to the next entry.
                    continue;

                // set nearest distance to candidate distance.
                nearestDistance = candidateDistance;
                // set note to candidate.
                note = candidate;
                // set uv to candidate uv.
                uv = candidateUv;
            // finish the for statement block.
            }

            // return note exists to the caller.
            return note != null;
        // finish the method try get nearest block.
        }

        // method: try get paper intersection.
        // intersect the top paper plane and use that same hit for distance and texture coordinates. reject
        // sides, backs, and points outside the sheet.
        private bool TryGetPaperIntersection(Ray ray, float maxDistance, out float distance, out Vector2 uv)
        // begin the method try get paper intersection block.
        {
            // set distance to 0.
            distance = 0f;
            // set uv to default.
            uv = default;
            // run the following statement only when paper collider is missing.
            if (paperCollider == null)
                // return false to the caller.
                return false;

            // the generated papers use boxcolliders. raycasthit.texturecoord
            // is only reliable for mesh colliders, so calculate both the
            // nearest hit distance and uv on the top plane. keeping these
            // values from the same intersection prevents a side-face hit
            // from selecting the paper while the uv comes from its top.
            // run the following statement only when paper collider matches box collider box.
            if (paperCollider is BoxCollider box)
            // begin the if statement block.
            {
                // store half size as box size multiplied by 0.5.
                Vector3 halfSize = box.size * 0.5f;
                // store top local point as box center plus the upward unit vector multiplied by half size y.
                Vector3 topLocalPoint = box.center + Vector3.up * halfSize.y;
                // store paper plane as a new plane using the upward unit vector converted from box transform
                // local direction into world direction, top local point converted from box transform local
                // coordinates into world coordinates.
                Plane paperPlane = new Plane(
                    // supply the upward unit vector converted from box transform local direction into world
                    // direction to the enclosing call.
                    box.transform.TransformDirection(Vector3.up),
                    // supply top local point converted from box transform local coordinates into world
                    // coordinates to the enclosing call.
                    box.transform.TransformPoint(topLocalPoint));
                // run the following statement only when the dot product of ray direction and paper plane
                // normal is at least 0 or not (paper plane: raycast using ray, distance) or distance is less
                // than 0 or distance is greater than max distance.
                if (Vector3.Dot(ray.direction, paperPlane.normal) >= 0f
                    // the dot product of ray direction and paper plane normal is at least 0 or not (paper
                    // plane: raycast using ray, distance).
                    || !paperPlane.Raycast(ray, out distance)
                    // the dot product of ray direction and paper plane normal is at least 0 or not (paper
                    // plane: raycast using ray, distance) or distance is less than 0.
                    || distance < 0f
                    // the dot product of ray direction and paper plane normal is at least 0 or not (paper
                    // plane: raycast using ray, distance) or distance is less than 0 or distance is greater
                    // than max distance.
                    || distance > maxDistance)
                    // return false to the caller.
                    return false;

                // store local point as the point distance units along ray converted from world coordinates
                // into box transform local coordinates.
                Vector3 localPoint = box.transform.InverseTransformPoint(ray.GetPoint(distance));
                // run the following statement only when local point x is less than box center x minus half
                // size x or local point x is greater than box center x plus half size x or local point z is
                // less than box center z minus half size z or local point z is greater than box center z plus
                // half size z.
                if (localPoint.x < box.center.x - halfSize.x
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x.
                    || localPoint.x > box.center.x + halfSize.x
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x or local point z is less than box center z minus half
                    // size z.
                    || localPoint.z < box.center.z - halfSize.z
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x or local point z is less than box center z minus half
                    // size z or local point z is greater than box center z plus half size z.
                    || localPoint.z > box.center.z + halfSize.z)
                    // return false to the caller.
                    return false;

                // set uv to a new two-dimensional vector using the proportion of local point x between box
                // center x minus half size x and box center x plus half size x, the proportion of local point
                // z between box center z minus half size z and box center z plus half size z.
                uv = new Vector2(
                    // supply the proportion of local point x between box center x minus half size x and box
                    // center x plus half size x to the enclosing call.
                    Mathf.InverseLerp(box.center.x - halfSize.x, box.center.x + halfSize.x, localPoint.x),
                    // supply the proportion of local point z between box center z minus half size z and box
                    // center z plus half size z to the enclosing call.
                    Mathf.InverseLerp(box.center.z - halfSize.z, box.center.z + halfSize.z, localPoint.z));
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // run the following statement only when not (paper collider: raycast using ray, the output
                // value hit, max distance).
                if (!paperCollider.Raycast(ray, out RaycastHit hit, maxDistance))
                    // return false to the caller.
                    return false;
                // set distance to hit distance.
                distance = hit.distance;
                // set uv to hit texture coord.
                uv = hit.textureCoord;
            // finish the else clause block.
            }

            // return uv x is at least 0 and uv x is at most 1 and uv y is at least 0 and uv y is at most 1 to
            // the caller.
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        // finish the method try get paper intersection block.
        }

        // method: stamp.
        // paint a single brush impression in the pixel buffer with a soft edge.
        private void Stamp(Vector2 uv, bool erasing)
        // begin the method stamp block.
        {
            // run the following statement only when pixels is missing.
            if (pixels == null)
                // call initialize surface: create the runtime texture and pixel buffer if needed, then connect
                // the visible writing surface.
                InitializeSurface();

            // store colour as if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use a
            // new 32-bit colour using 20, 25, 35, 255.
            Color32 colour = erasing ? new Color32(255, 255, 255, 255) : new Color32(20, 25, 35, 255);
            // store diameter as if erasing, use eraser size, otherwise use pencil size.
            float diameter = erasing ? eraserSize : pencilSize;
            // store radius x as the larger of 1 and diameter divided by paper world size x multiplied by
            // texture width multiplied by 0.5.
            float radiusX = Mathf.Max(1f, (diameter / paperWorldSize.x) * textureWidth * 0.5f);
            // store radius y as the larger of 1 and diameter divided by paper world size y multiplied by
            // texture height multiplied by 0.5.
            float radiusY = Mathf.Max(1f, (diameter / paperWorldSize.y) * textureHeight * 0.5f);
            // store center x as uv x limited to the range from zero to one multiplied by texture width minus
            // 1.
            float centerX = Mathf.Clamp01(uv.x) * (textureWidth - 1);
            // store center y as uv y limited to the range from zero to one multiplied by texture height minus
            // 1.
            float centerY = Mathf.Clamp01(uv.y) * (textureHeight - 1);

            // store min x as the larger of 0 and center x minus radius x minus 1 rounded downward to an
            // integer.
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX - 1f));
            // store max x as the smaller of texture width minus 1 and center x plus radius x plus 1 rounded
            // upward to an integer.
            int maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(centerX + radiusX + 1f));
            // store min y as the larger of 0 and center y minus radius y minus 1 rounded downward to an
            // integer.
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY - 1f));
            // store max y as the smaller of texture height minus 1 and center y plus radius y plus 1 rounded
            // upward to an integer.
            int maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(centerY + radiusY + 1f));
            // store radius x squared as radius x multiplied by radius x.
            float radiusXSquared = radiusX * radiusX;
            // store radius y squared as radius y multiplied by radius y.
            float radiusYSquared = radiusY * radiusY;

            // repeat this loop while y is at most max y. increase y by one after each pass.
            for (int y = minY; y <= maxY; y++)
            // begin the for statement block.
            {
                // store dy as y minus center y.
                float dy = y - centerY;
                // repeat this loop while x is at most max x. increase x by one after each pass.
                for (int x = minX; x <= maxX; x++)
                // begin the for statement block.
                {
                    // store dx as x minus center x.
                    float dx = x - centerX;
                    // store normalized distance as mathf: sqrt using dx multiplied by dx divided by radius x
                    // squared plus dy multiplied by dy divided by radius y squared.
                    float normalizedDistance = Mathf.Sqrt(
                        // supply dx multiplied by dx divided by radius x squared plus dy multiplied by dy
                        // divided by radius y squared to the enclosing call.
                        (dx * dx) / radiusXSquared + (dy * dy) / radiusYSquared);
                    // run the following statement only when normalized distance is greater than 1.05.
                    if (normalizedDistance > 1.05f)
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // store coverage as if normalized distance is at most 0.86, use 1, otherwise use 1 minus
                    // the proportion of normalized distance between 0.86 and 1.05.
                    float coverage = normalizedDistance <= 0.86f
                        // if normalized distance is at most 0.86, use 1, otherwise use 1 minus the proportion
                        // of normalized distance between 0.86 and 1.05.
                        ? 1f
                        // if normalized distance is at most 0.86, use 1, otherwise use 1 minus the proportion
                        // of normalized distance between 0.86 and 1.05.
                        : 1f - Mathf.InverseLerp(0.86f, 1.05f, normalizedDistance);
                    // store pixel index as y multiplied by texture width plus x.
                    int pixelIndex = y * textureWidth + x;
                    // store existing as the entry at pixel index in pixels.
                    Color32 existing = pixels[pixelIndex];
                    // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated
                    // value from existing r toward colour r using coverage rounded to the nearest integer
                    // converted to byte, an interpolated value from existing g toward colour g using coverage
                    // rounded to the nearest integer converted to byte, an interpolated value from existing b
                    // toward colour b using coverage rounded to the nearest integer converted to byte, 255.
                    pixels[pixelIndex] = new Color32(
                        // supply an interpolated value from existing r toward colour r using coverage rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                        // supply an interpolated value from existing g toward colour g using coverage rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                        // supply an interpolated value from existing b toward colour b using coverage rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                        // supply 255 to the enclosing call.
                        255);
                // finish the for statement block.
                }
            // finish the for statement block.
            }
        // finish the method stamp block.
        }

        // method: stamp segment.
        // paint the area along a short line segment so neighbouring samples form a continuous stroke.
        private void StampSegment(Vector2 startUv, Vector2 endUv, bool erasing)
        // begin the method stamp segment block.
        {
            // run the following statement only when pixels is missing.
            if (pixels == null)
                // call initialize surface: create the runtime texture and pixel buffer if needed, then connect
                // the visible writing surface.
                InitializeSurface();

            // store colour as if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use a
            // new 32-bit colour using 20, 25, 35, 255.
            Color32 colour = erasing ? new Color32(255, 255, 255, 255) : new Color32(20, 25, 35, 255);
            // store diameter as if erasing, use eraser size, otherwise use pencil size.
            float diameter = erasing ? eraserSize : pencilSize;
            // store radius x as the larger of 1 and diameter divided by paper world size x multiplied by
            // texture width multiplied by 0.5.
            float radiusX = Mathf.Max(1f, (diameter / paperWorldSize.x) * textureWidth * 0.5f);
            // store radius y as the larger of 1 and diameter divided by paper world size y multiplied by
            // texture height multiplied by 0.5.
            float radiusY = Mathf.Max(1f, (diameter / paperWorldSize.y) * textureHeight * 0.5f);
            // store start x as start uv x limited to the range from zero to one multiplied by texture width
            // minus 1.
            float startX = Mathf.Clamp01(startUv.x) * (textureWidth - 1);
            // store start y as start uv y limited to the range from zero to one multiplied by texture height
            // minus 1.
            float startY = Mathf.Clamp01(startUv.y) * (textureHeight - 1);
            // store end x as end uv x limited to the range from zero to one multiplied by texture width minus
            // 1.
            float endX = Mathf.Clamp01(endUv.x) * (textureWidth - 1);
            // store end y as end uv y limited to the range from zero to one multiplied by texture height minus
            // 1.
            float endY = Mathf.Clamp01(endUv.y) * (textureHeight - 1);

            // store min x as the larger of 0 and the smaller of start x and end x minus radius x minus 1
            // rounded downward to an integer.
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(startX, endX) - radiusX - 1f));
            // store max x as the smaller of texture width minus 1 and the larger of start x and end x plus
            // radius x plus 1 rounded upward to an integer.
            int maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(Mathf.Max(startX, endX) + radiusX + 1f));
            // store min y as the larger of 0 and the smaller of start y and end y minus radius y minus 1
            // rounded downward to an integer.
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(startY, endY) - radiusY - 1f));
            // store max y as the smaller of texture height minus 1 and the larger of start y and end y plus
            // radius y plus 1 rounded upward to an integer.
            int maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(Mathf.Max(startY, endY) + radiusY + 1f));

            // store start as a new two-dimensional vector using start x divided by radius x, start y divided
            // by radius y.
            Vector2 start = new Vector2(startX / radiusX, startY / radiusY);
            // store end as a new two-dimensional vector using end x divided by radius x, end y divided by
            // radius y.
            Vector2 end = new Vector2(endX / radiusX, endY / radiusY);
            // store segment as end minus start.
            Vector2 segment = end - start;
            // store segment length squared as the squared length of segment.
            float segmentLengthSquared = segment.sqrMagnitude;
            // repeat this loop while y is at most max y. increase y by one after each pass.
            for (int y = minY; y <= maxY; y++)
            // begin the for statement block.
            {
                // repeat this loop while x is at most max x. increase x by one after each pass.
                for (int x = minX; x <= maxX; x++)
                // begin the for statement block.
                {
                    // store point as a new two-dimensional vector using x divided by radius x, y divided by
                    // radius y.
                    Vector2 point = new Vector2(x / radiusX, y / radiusY);
                    // store t as if segment length squared is greater than 1e-06, use the dot product of point
                    // minus start and segment divided by segment length squared limited to the range from zero
                    // to one, otherwise use 0.
                    float t = segmentLengthSquared > 0.000001f
                        // if segment length squared is greater than 1e-06, use the dot product of point minus
                        // start and segment divided by segment length squared limited to the range from zero
                        // to one, otherwise use 0.
                        ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSquared)
                        // if segment length squared is greater than 1e-06, use the dot product of point minus
                        // start and segment divided by segment length squared limited to the range from zero
                        // to one, otherwise use 0.
                        : 0f;
                    // store normalized distance as the distance between point and an interpolated value from
                    // start toward end using t.
                    float normalizedDistance = Vector2.Distance(point, Vector2.Lerp(start, end, t));
                    // run the following statement only when normalized distance is greater than 1.05.
                    if (normalizedDistance > 1.05f)
                        // skip the rest of this loop pass and move to the next entry.
                        continue;

                    // store coverage as if normalized distance is at most 0.86, use 1, otherwise use 1 minus
                    // the proportion of normalized distance between 0.86 and 1.05.
                    float coverage = normalizedDistance <= 0.86f
                        // if normalized distance is at most 0.86, use 1, otherwise use 1 minus the proportion
                        // of normalized distance between 0.86 and 1.05.
                        ? 1f
                        // if normalized distance is at most 0.86, use 1, otherwise use 1 minus the proportion
                        // of normalized distance between 0.86 and 1.05.
                        : 1f - Mathf.InverseLerp(0.86f, 1.05f, normalizedDistance);
                    // store pixel index as y multiplied by texture width plus x.
                    int pixelIndex = y * textureWidth + x;
                    // store existing as the entry at pixel index in pixels.
                    Color32 existing = pixels[pixelIndex];
                    // run the following statement only when erasing.
                    if (erasing)
                    // begin the if statement block.
                    {
                        // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated
                        // value from existing r toward colour r using coverage rounded to the nearest integer
                        // converted to byte, an interpolated value from existing g toward colour g using
                        // coverage rounded to the nearest integer converted to byte, an interpolated value
                        // from existing b toward colour b using coverage rounded to the nearest integer
                        // converted to byte, 255.
                        pixels[pixelIndex] = new Color32(
                            // supply an interpolated value from existing r toward colour r using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                            // supply an interpolated value from existing g toward colour g using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                            // supply an interpolated value from existing b toward colour b using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                            // supply 255 to the enclosing call.
                            255);
                    // finish the if statement block.
                    }
                    // handle the alternative when the preceding condition is false.
                    else
                    // begin the else clause block.
                    {
                        // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated
                        // value from existing r toward colour r using coverage rounded to the nearest integer
                        // converted to byte, an interpolated value from existing g toward colour g using
                        // coverage rounded to the nearest integer converted to byte, an interpolated value
                        // from existing b toward colour b using coverage rounded to the nearest integer
                        // converted to byte, 255.
                        pixels[pixelIndex] = new Color32(
                            // supply an interpolated value from existing r toward colour r using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                            // supply an interpolated value from existing g toward colour g using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                            // supply an interpolated value from existing b toward colour b using coverage
                            // rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                            // supply 255 to the enclosing call.
                            255);
                    // finish the else clause block.
                    }
                // finish the for statement block.
                }
            // finish the for statement block.
            }
        // finish the method stamp segment block.
        }

        // method: filter input point.
        // reduce isolated input spikes with recent samples and blend toward the accepted point to keep
        // handwriting stable.
        private Vector2 FilterInputPoint(Vector2 uv)
        // begin the method filter input point block.
        {
            // run the following statement only when input sample count is at most 0.
            if (inputSampleCount <= 0)
            // begin the if statement block.
            {
                // set input before last uv to uv.
                inputBeforeLastUv = uv;
                // set last input uv to uv.
                lastInputUv = uv;
                // set filtered input uv to uv.
                filteredInputUv = uv;
                // set input sample count to 1.
                inputSampleCount = 1;
                // return uv to the caller.
                return uv;
            // finish the if statement block.
            }

            // store candidate as if input sample count equals 1, use uv, otherwise use a new two-dimensional
            // vector using median using input before last uv x, last input uv x, uv x, median using input
            // before last uv y, last input uv y, uv y.
            Vector2 candidate = inputSampleCount == 1
                // if input sample count equals 1, use uv, otherwise use a new two-dimensional vector using
                // median using input before last uv x, last input uv x, uv x, median using input before last
                // uv y, last input uv y, uv y.
                ? uv
                // if input sample count equals 1, use uv, otherwise use a new two-dimensional vector using
                // median using input before last uv x, last input uv x, uv x, median using input before last
                // uv y, last input uv y, uv y.
                : new Vector2(
                    // supply median using input before last uv x, last input uv x, uv x to the enclosing call.
                    Median(inputBeforeLastUv.x, lastInputUv.x, uv.x),
                    // supply median using input before last uv y, last input uv y, uv y to the enclosing call.
                    Median(inputBeforeLastUv.y, lastInputUv.y, uv.y));
            // store filtered as an interpolated value from filtered input uv toward candidate using stroke
            // point smoothing.
            Vector2 filtered = Vector2.Lerp(filteredInputUv, candidate, strokePointSmoothing);
            // set input before last uv to last input uv.
            inputBeforeLastUv = lastInputUv;
            // set last input uv to uv.
            lastInputUv = uv;
            // set filtered input uv to filtered.
            filteredInputUv = filtered;
            // set input sample count to the smaller of input sample count plus 1 and 3.
            inputSampleCount = Mathf.Min(inputSampleCount + 1, 3);
            // return filtered to the caller.
            return filtered;
        // finish the method filter input point block.
        }

        // method: close stroke to latest input.
        // finish the stroke at the latest raw input point so smoothing does not leave a gap at the endpoint.
        private void CloseStrokeToLatestInput()
        // begin the method close stroke to latest input block.
        {
            // run the following statement only when not (has previous point) or input sample count is at most
            // 0.
            if (!hasPreviousPoint || inputSampleCount <= 0)
                // leave this method now without running the remaining statements.
                return;

            // finish on the latest raw sample. the filtered point can trail a
            // fast final drag event by more than the pencil radius, leaving a
            // small but visible gap at a line or circle endpoint.
            // store endpoint as last input uv.
            Vector2 endpoint = lastInputUv;
            // store distance as the distance between previous uv and endpoint.
            float distance = Vector2.Distance(previousUv, endpoint);
            // run the following statement only when not (latest input is desktop) and distance is greater than
            // maximum uv jump.
            if (!latestInputIsDesktop && distance > maximumUvJump)
                // leave this method now without running the remaining statements.
                return;

            // store brush diameter as if previous stroke was erasing, use eraser size, otherwise use pencil
            // size.
            float brushDiameter = previousStrokeWasErasing ? eraserSize : pencilSize;
            // store brush radius in uv as brush diameter multiplied by 0.5 divided by the larger of paper
            // world size x and paper world size y.
            float brushRadiusInUv = (brushDiameter * 0.5f)
                // brush diameter multiplied by 0.5 divided by the larger of paper world size x and paper world
                // size y.
                / Mathf.Max(paperWorldSize.x, paperWorldSize.y);
            // store spacing as the smaller of the larger of 0.00025 and interpolation spacing and the larger
            // of 0.00025 and brush radius in uv multiplied by 0.4.
            float spacing = Mathf.Min(
                // supply the larger of 0.00025 and interpolation spacing to the enclosing call.
                Mathf.Max(0.00025f, interpolationSpacing),
                // supply the larger of 0.00025 and brush radius in uv multiplied by 0.4 to the enclosing call.
                Mathf.Max(0.00025f, brushRadiusInUv * 0.4f));
            // store steps as distance divided by spacing rounded upward to an integer clamped between 1 and
            // the larger of 1 and maximum interpolation steps.
            int steps = Mathf.Clamp(
                // supply distance divided by spacing rounded upward to an integer to the enclosing call.
                Mathf.CeilToInt(distance / spacing),
                // supply 1 to the enclosing call.
                1,
                // supply the larger of 1 and maximum interpolation steps to the enclosing call.
                Mathf.Max(1, maximumInterpolationSteps));
            // store segment start as previous uv.
            Vector2 segmentStart = previousUv;
            // repeat this loop while i is at most steps. increase i by one after each pass.
            for (int i = 1; i <= steps; i++)
            // begin the for statement block.
            {
                // store segment end as an interpolated value from previous uv toward endpoint using i divided
                // by steps converted to float.
                Vector2 segmentEnd = Vector2.Lerp(previousUv, endpoint, i / (float)steps);
                // stamp segment using segment start, segment end, previous stroke was erasing.
                StampSegment(segmentStart, segmentEnd, previousStrokeWasErasing);
                // set segment start to segment end.
                segmentStart = segmentEnd;
            // finish the for statement block.
            }

            // set previous uv to endpoint.
            previousUv = endpoint;
            // mark the pixel buffer as changed so late update uploads the new drawing this frame.
            textureDirty = true;
        // finish the method close stroke to latest input block.
        }

        // method: median.
        // return the middle of three values by removing the smallest and largest. this rejects a one-sample
        // spike.
        private static float Median(float a, float b, float c)
        // begin the method median block.
        {
            // return a plus b plus c minus the smaller of a and the smaller of b and c minus the larger of a
            // and the larger of b and c to the caller.
            return a + b + c - Mathf.Min(a, Mathf.Min(b, c)) - Mathf.Max(a, Mathf.Max(b, c));
        // finish the method median block.
        }

        // method: ensure crosshair.
        // create the writing crosshair once and reuse it to show the current writing location.
        private void EnsureCrosshair()
        // begin the method ensure crosshair block.
        {
            // run the following statement only when crosshair exists or paper renderer is missing.
            if (crosshair != null || paperRenderer == null)
                // leave this method now without running the remaining statements.
                return;

            // set crosshair to a new game object using the text "paperwritingcrosshair" transform.
            crosshair = new GameObject("PaperWritingCrosshair").transform;
            // parent crosshair under paper renderer transform using local coordinates.
            crosshair.SetParent(paperRenderer.transform, false);

            // store shader as the child or resource the text "universal render pipeline/unlit" found in
            // shader.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            // run the following statement only when shader is missing.
            if (shader == null)
                // set shader to the child or resource the text "unlit/color" found in shader.
                shader = Shader.Find("Unlit/Color");
            // store material as if shader exists, use a new material using shader, otherwise use no object.
            Material material = shader != null ? new Material(shader) : null;
            // create crosshair bar using the text "horizontal", a new three-dimensional vector using 0.07,
            // 0.005, 0.005, material.
            CreateCrosshairBar("Horizontal", new Vector3(0.07f, 0.005f, 0.005f), material);
            // create crosshair bar using the text "vertical", a new three-dimensional vector using 0.005,
            // 0.005, 0.07, material.
            CreateCrosshairBar("Vertical", new Vector3(0.005f, 0.005f, 0.07f), material);
        // finish the method ensure crosshair block.
        }

        // method: ensure writing surface.
        // build a dedicated visible writing mesh above the paper with texture coordinates that match the
        // contact mapping.
        private void EnsureWritingSurface()
        // begin the method ensure writing surface block.
        {
            // run the following statement only when writing surface renderer exists or paper renderer is
            // missing or note texture is missing.
            if (writingSurfaceRenderer != null || paperRenderer == null || noteTexture == null)
                // leave this method now without running the remaining statements.
                return;

            // store surface as game object: create primitive using primitive type quad.
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            // set surface name to the text "paper writing surface".
            surface.name = "Paper Writing Surface";
            // parent surface transform under paper renderer transform using local coordinates.
            surface.transform.SetParent(paperRenderer.transform, false);
            // set surface transform local position to a new three-dimensional vector using 0, 0.5 plus 0.001
            // divided by the larger of 0.0001 and paper renderer transform lossy scale y, 0.
            surface.transform.localPosition = new Vector3(
                // supply 0 to the enclosing call.
                0f,
                // supply 0.5 plus 0.001 divided by the larger of 0.0001 and paper renderer transform lossy
                // scale y to the enclosing call.
                0.5f + 0.001f / Mathf.Max(0.0001f, paperRenderer.transform.lossyScale.y),
                // supply 0 to the enclosing call.
                0f);
            // quad u follows local +x; rotating +90 degrees sends quad v
            // along paper +z, exactly matching trygetpaperintersection.
            // the former -90 degree rotation sent v along -z and displayed
            // every contact at the opposite depth on the sheet.
            // set surface transform local rotation to quaternion: euler using 90, 0, 0.
            surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            // store inherited scale as paper renderer transform lossy scale.
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
            // set surface transform local scale to a new three-dimensional vector using paper world size x
            // divided by the larger of 0.0001 and inherited scale x, paper world size y divided by the larger
            // of 0.0001 and inherited scale z, 1.
            surface.transform.localScale = new Vector3(
                // supply paper world size x divided by the larger of 0.0001 and inherited scale x to the
                // enclosing call.
                paperWorldSize.x / Mathf.Max(0.0001f, inheritedScale.x),
                // supply paper world size y divided by the larger of 0.0001 and inherited scale z to the
                // enclosing call.
                paperWorldSize.y / Mathf.Max(0.0001f, inheritedScale.z),
                // supply 1 to the enclosing call.
                1f);

            // store surface collider as the the collider component attached to this object component on
            // surface.
            Collider surfaceCollider = surface.GetComponent<Collider>();
            // run the following statement only when surface collider exists.
            if (surfaceCollider != null)
            // begin the if statement block.
            {
                // run the following statement only when whether the application is running in play mode.
                if (Application.isPlaying)
                    // schedule surface collider for destruction after this update.
                    Destroy(surfaceCollider);
                // handle the alternative when the preceding condition is false.
                else
                    // destroy surface collider immediately for editor cleanup.
                    DestroyImmediate(surfaceCollider);
            // finish the if statement block.
            }

            // set writing surface renderer to the the renderer component attached to this object component on
            // surface.
            writingSurfaceRenderer = surface.GetComponent<Renderer>();
            // run the following statement only when writing surface renderer is missing.
            if (writingSurfaceRenderer == null)
                // leave this method now without running the remaining statements.
                return;

            // a writing sheet must stay white and its pencil readable even
            // under the classroom's dim baked lighting.
            // store shader as the child or resource the text "universal render pipeline/unlit" found in
            // shader.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            // run the following statement only when shader is missing.
            if (shader == null)
                // set shader to the child or resource the text "unlit/texture" found in shader.
                shader = Shader.Find("Unlit/Texture");
            // set writing surface material to if shader exists, use a new material using shader, otherwise use
            // no object.
            writingSurfaceMaterial = shader != null ? new Material(shader) : null;

            // run the following statement only when writing surface material is missing.
            if (writingSurfaceMaterial == null)
                // leave this method now without running the remaining statements.
                return;

            // set writing surface material name to the text "student paper writing surface".
            writingSurfaceMaterial.name = "Student Paper Writing Surface";
            // set writing surface material main texture to note texture.
            writingSurfaceMaterial.mainTexture = noteTexture;
            // run the following statement only when writing surface material: has property using the text
            // "_basemap".
            if (writingSurfaceMaterial.HasProperty("_BaseMap"))
                // writing surface material: set texture using the text "_basemap", note texture.
                writingSurfaceMaterial.SetTexture("_BaseMap", noteTexture);
            // run the following statement only when writing surface material: has property using the text
            // "_cull".
            if (writingSurfaceMaterial.HasProperty("_Cull"))
                // writing surface material: set float using the text "_cull", 0.
                writingSurfaceMaterial.SetFloat("_Cull", 0f);
            // run the following statement only when whether the application is running in play mode.
            if (Application.isPlaying)
                // set writing surface renderer material to writing surface material.
                writingSurfaceRenderer.material = writingSurfaceMaterial;
            // handle the alternative when the preceding condition is false.
            else
                // set writing surface renderer shared material to writing surface material.
                writingSurfaceRenderer.sharedMaterial = writingSurfaceMaterial;
            // run the following statement only when writing surface material: has property using the text
            // "_basecolor".
            if (writingSurfaceMaterial.HasProperty("_BaseColor"))
                // writing surface material: set colour using the text "_basecolor", colour white.
                writingSurfaceMaterial.SetColor("_BaseColor", Color.white);

        // finish the method ensure writing surface block.
        }

        // method: create crosshair bar.
        // build one thin unlit bar of the writing crosshair and attach it to the surface.
        private void CreateCrosshairBar(string name, Vector3 worldScale, Material material)
        // begin the method create crosshair bar block.
        {
            // store bar as game object: create primitive using primitive type cube.
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // set bar name to name.
            bar.name = name;
            // parent bar transform under crosshair using local coordinates.
            bar.transform.SetParent(crosshair, false);
            // store inherited scale as paper renderer transform lossy scale.
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
            // set bar transform local scale to a new three-dimensional vector using world scale x divided by
            // the larger of 0.0001 and inherited scale x, world scale y divided by the larger of 0.0001 and
            // inherited scale y, world scale z divided by the larger of 0.0001 and inherited scale z.
            bar.transform.localScale = new Vector3(
                // supply world scale x divided by the larger of 0.0001 and inherited scale x to the enclosing
                // call.
                worldScale.x / Mathf.Max(0.0001f, inheritedScale.x),
                // supply world scale y divided by the larger of 0.0001 and inherited scale y to the enclosing
                // call.
                worldScale.y / Mathf.Max(0.0001f, inheritedScale.y),
                // supply world scale z divided by the larger of 0.0001 and inherited scale z to the enclosing
                // call.
                worldScale.z / Mathf.Max(0.0001f, inheritedScale.z));
            // store collider as the the collider component attached to this object component on bar.
            Collider collider = bar.GetComponent<Collider>();
            // run the following statement only when collider exists.
            if (collider != null)
            // begin the if statement block.
            {
                // run the following statement only when whether the application is running in play mode.
                if (Application.isPlaying)
                    // schedule collider for destruction after this update.
                    Destroy(collider);
                // handle the alternative when the preceding condition is false.
                else
                    // destroy collider immediately for editor cleanup.
                    DestroyImmediate(collider);
            // finish the if statement block.
            }
            // store renderer as the the renderer component attached to this object component on bar.
            Renderer renderer = bar.GetComponent<Renderer>();
            // run the following statement only when renderer exists.
            if (renderer != null)
            // begin the if statement block.
            {
                // run the following statement only when material exists.
                if (material != null)
                // begin the if statement block.
                {
                    // run the following statement only when whether the application is running in play mode.
                    if (Application.isPlaying)
                        // set renderer material to material.
                        renderer.material = material;
                    // handle the alternative when the preceding condition is false.
                    else
                        // set renderer shared material to material.
                        renderer.sharedMaterial = material;
                // finish the if statement block.
                }
                // append renderer to crosshair renderers.
                crosshairRenderers.Add(renderer);
            // finish the if statement block.
            }
        // finish the method create crosshair bar block.
        }

        // method: fill.
        // set every pixel in the buffer to the supplied colour. clearing uses a white fill.
        private void Fill(Color colour)
        // begin the method fill block.
        {
            // store fill as colour.
            Color32 fill = colour;
            // repeat this loop while i is less than the number of items in pixels. increase i by one after
            // each pass.
            for (int i = 0; i < pixels.Length; i++)
                // set the entry at i in pixels to fill.
                pixels[i] = fill;
        // finish the method fill block.
        }

        // method: set crosshair bar scale.
        // resize one crosshair bar in local coordinates so its visible size remains useful on the scaled
        // surface.
        private void SetCrosshairBarScale(int index, float x, float y, float z)
        // begin the method set crosshair bar scale block.
        {
            // run the following statement only when index is less than 0 or index is at least the number of
            // items in crosshair renderers or the entry at index in crosshair renderers is missing.
            if (index < 0 || index >= crosshairRenderers.Count || crosshairRenderers[index] == null)
                // leave this method now without running the remaining statements.
                return;

            // store bar as the entry at index in crosshair renderers transform.
            Transform bar = crosshairRenderers[index].transform;
            // store inherited scale as paper renderer transform lossy scale.
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
            // set bar local scale to a new three-dimensional vector using x divided by the larger of 0.0001
            // and inherited scale x, y divided by the larger of 0.0001 and inherited scale y, z divided by the
            // larger of 0.0001 and inherited scale z.
            bar.localScale = new Vector3(
                // supply x divided by the larger of 0.0001 and inherited scale x to the enclosing call.
                x / Mathf.Max(0.0001f, inheritedScale.x),
                // supply y divided by the larger of 0.0001 and inherited scale y to the enclosing call.
                y / Mathf.Max(0.0001f, inheritedScale.y),
                // supply z divided by the larger of 0.0001 and inherited scale z to the enclosing call.
                z / Mathf.Max(0.0001f, inheritedScale.z));
        // finish the method set crosshair bar scale block.
        }

        // method: apply texture.
        // copy the pixel buffer into the texture and upload it, then clear the pending-change flag.
        private void ApplyTexture()
        // begin the method apply texture block.
        {
            // run the following statement only when note texture is missing or pixels is missing.
            if (noteTexture == null || pixels == null)
                // leave this method now without running the remaining statements.
                return;

            // copy pixels into note texture before uploading the texture.
            noteTexture.SetPixels32(pixels);
            // upload changes to note texture.
            noteTexture.Apply(false, false);
            // clear the pending texture-change flag because this code is handling the upload directly.
            textureDirty = false;
            // call ensure writing surface: build a dedicated visible writing mesh above the paper with texture
            // coordinates that match the contact mapping.
            EnsureWritingSurface();
            // run the following statement only when writing surface material exists.
            if (writingSurfaceMaterial != null)
            // begin the if statement block.
            {
                // set writing surface material main texture to note texture.
                writingSurfaceMaterial.mainTexture = noteTexture;
                // run the following statement only when writing surface material: has property using the text
                // "_basemap".
                if (writingSurfaceMaterial.HasProperty("_BaseMap"))
                    // writing surface material: set texture using the text "_basemap", note texture.
                    writingSurfaceMaterial.SetTexture("_BaseMap", noteTexture);
            // finish the if statement block.
            }
        // finish the method apply texture block.
        }
    // finish the class paper note canvas block.
    }
// finish the namespace block.
}
