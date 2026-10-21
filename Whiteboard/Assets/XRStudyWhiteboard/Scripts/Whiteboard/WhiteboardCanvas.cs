// script overview: this script is the pixel drawing surface for the main whiteboard. it maps ray hits on the
// visible board face to texture coordinates, paints the selected marker colour or eraser, smooths samples, and
// connects them into strokes. it batches texture uploads and reports drawing events for recording. it also
// restores board pixels and applies recorded events during playback. device input is handled by other scripts.

// search: whiteboard writing | colour | marker | eraser | texture | strokes | recording.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make the types in system available to this script.
using System;
// make typed lists and dictionaries available for collecting runtime objects and data.
using System.Collections.Generic;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // owns the lightweight cpu texture used by the board. the texture is
    // intentionally small enough for quest while still producing connected,
    // readable strokes.
    // declare the whiteboard canvas class based on unity behaviour.
    public sealed class WhiteboardCanvas : MonoBehaviour
    // begin the class whiteboard canvas block.
    {
        // apply unity or c sharp metadata: header("board surface").
        [Header("Board surface")]
        // surface renderer: the renderer that displays the main board drawing texture. keep surface renderer
        // as renderer for later use. allow unity to save and configure this field in the inspector.
        [SerializeField] private Renderer surfaceRenderer;
        // surface collider: the board shape used to calculate contact with its visible front face. keep
        // surface collider as collider for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Collider surfaceCollider;
        // keep cursor as transform for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Transform cursor;
        // keep cursor renderer as renderer for later use. allow unity to save and configure this field in the
        // inspector.
        [SerializeField] private Renderer cursorRenderer;
        // board world size: the physical board width and height used to turn metres into texture-pixel brush
        // sizes. keep board world size as two-dimensional vector, initially a new two-dimensional vector using
        // 3, 1.5. allow unity to save and configure this field in the inspector.
        [SerializeField] private Vector2 boardWorldSize = new Vector2(3f, 1.5f);
        // flip horizontal texture uv: whether the board texture horizontal direction needs reversing to match
        // the visible face. keep flip horizontal texture uv as bool, initially true. allow unity to save and
        // configure this field in the inspector.
        [SerializeField] private bool flipHorizontalTextureUv = true;

        // apply unity or c sharp metadata: header("drawing").
        [Header("Drawing")]
        // texture width: the drawing texture width in pixels. keep texture width as int, initially 768. allow
        // unity to save and configure this field in the inspector.
        [SerializeField] private int textureWidth = 768;
        // texture height: the drawing texture height in pixels. keep texture height as int, initially 384.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private int textureHeight = 384;
        // marker size: the physical marker brush diameter in metres. keep marker size as float, initially
        // 0.032. allow unity to save and configure this field in the inspector.
        [SerializeField] private float markerSize = 0.032f;
        // eraser size: the physical eraser diameter in metres. keep eraser size as float, initially 0.06.
        // allow unity to save and configure this field in the inspector.
        [SerializeField] private float eraserSize = 0.06f;
        // marker opacity: the opacity used when blending marker ink into existing pixels. keep marker opacity
        // as float, initially 0.95. allow unity to save and configure this field in the inspector.
        [SerializeField, Range(0.1f, 1f)] private float markerOpacity = 0.95f;
        // interpolation spacing: the maximum preferred gap between interpolated points in normalised texture
        // coordinates. keep interpolation spacing as float, initially 0.0006. allow unity to save and
        // configure this field in the inspector.
        [SerializeField] private float interpolationSpacing = 0.0006f;
        // maximum interpolation steps: the safety limit on work used to connect one pair of samples. keep
        // maximum interpolation steps as int, initially 8192. allow unity to save and configure this field in
        // the inspector.
        [SerializeField] private int maximumInterpolationSteps = 8192;
        // maximum uv jump: the largest accepted tracked-input jump before breaking stroke continuity. keep
        // maximum uv jump as float, initially 0.08. allow unity to save and configure this field in the
        // inspector.
        [SerializeField, Range(0.02f, 1f)] private float maximumUvJump = 0.08f;
        // stroke point smoothing: how strongly the filtered point follows the latest accepted candidate. keep
        // stroke point smoothing as float, initially 0.9. allow unity to save and configure this field in the
        // inspector.
        [SerializeField, Range(0.35f, 1f)] private float strokePointSmoothing = 0.9f;

        // the runtime texture stores the whiteboard drawing.
        // board texture: the runtime texture displaying only this whiteboard ink. keep board texture as
        // texture2 d for later use.
        private Texture2D boardTexture;
        // pixels: the cpu-side colour buffer that the brush changes before texture upload. keep pixels as
        // 32-bit colour[] for later use.
        private Color32[] pixels;
        // manager: the shared source of the whiteboard tool, colour, confirmation, and playback state. keep
        // manager as xr study whiteboard manager for later use.
        private XRStudyWhiteboardManager manager;
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
        // texture dirty: whether the cpu pixel buffer has changes waiting to be uploaded this frame. keep
        // texture dirty as bool for later use.
        private bool textureDirty;
        // latest input is desktop: whether the latest sample came from the trusted desktop cursor rather than
        // tracked input. keep latest input is desktop as bool for later use.
        private bool latestInputIsDesktop;
        // stroke open: whether a whiteboard stroke has begun and has not yet ended. keep stroke open as bool
        // for later use.
        private bool strokeOpen;
        // applying playback: whether the recorder is currently applying an event through the canvas. keep
        // applying playback as bool for later use.
        private bool applyingPlayback;
        // board material: the owned material instance that keeps this board texture separate from other
        // objects. keep board material as material for later use.
        private Material boardMaterial;
        // expose is stroke open as stroke open.
        public bool IsStrokeOpen => strokeOpen;
        // declare drawing event so other components can subscribe to notifications.
        public event Action<WhiteboardRecordingEventType, Vector2, bool> DrawingEvent;
        // expose live input blocked as manager exists and manager is playback active and not (applying
        // playback).
        private bool LiveInputBlocked => manager != null && manager.IsPlaybackActive && !applyingPlayback;
        // keep crosshair as transform for later use.
        private Transform crosshair;
        // keep crosshair renderers as list<renderer>, initially a new list<renderer>.
        private readonly List<Renderer> crosshairRenderers = new List<Renderer>();

        // expose marker size as marker size.
        public float MarkerSize => markerSize;
        // expose eraser size as eraser size.
        public float EraserSize => eraserSize;
        // expose board world size as board world size.
        public Vector2 BoardWorldSize => boardWorldSize;

        // method: awake.
        // unity calls this when the component is first loaded. prepare the references and starting state
        // needed before normal updates.
        private void Awake()
        // begin the method awake block.
        {
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
            // destroy owned using board texture.
            DestroyOwned(boardTexture);
            // destroy owned using board material.
            DestroyOwned(boardMaterial);
            // visit each renderer in crosshair renderers.
            foreach (Renderer renderer in crosshairRenderers)
                // run the following statement only when renderer exists.
                if (renderer != null) DestroyOwned(renderer.sharedMaterial);
        // finish the method on destroy block.
        }

        // method: destroy owned.
        // release an object created by this drawing system. use delayed destruction in play mode and immediate
        // destruction in editor mode.
        private static void DestroyOwned(UnityEngine.Object owned)
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
            // upload one completed brush buffer per frame. applying a full
            // cpu texture from every mouse/controller sample was the source
            // of the editor lag and separated dot-like strokes.
            // run the following statement only when texture dirty.
            if (textureDirty)
                // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
                // pending-change flag.
                ApplyTexture();
        // finish the method late update block.
        }

        // method: set manager.
        // connect the manager that supplies the active tool, colour, and playback state.
        public void SetManager(XRStudyWhiteboardManager whiteboardManager)
        // begin the method set manager block.
        {
            // set manager to whiteboard manager.
            manager = whiteboardManager;
        // finish the method set manager block.
        }

        // method: configure board world size.
        // store a safe physical board width and height for brush-size conversion.
        public void ConfigureBoardWorldSize(Vector2 worldSize)
        // begin the method configure board world size block.
        {
            // set board world size to a new two-dimensional vector using the larger of 0.1 and world size x,
            // the larger of 0.1 and world size y.
            boardWorldSize = new Vector2(Mathf.Max(0.1f, worldSize.x), Mathf.Max(0.1f, worldSize.y));
        // finish the method configure board world size block.
        }

        // method: initialize surface.
        // create the runtime texture and pixel buffer if needed, then connect the visible writing surface.
        public void InitializeSurface()
        // begin the method initialize surface block.
        {
            // run the following statement only when surface renderer is missing.
            if (surfaceRenderer == null)
                // set surface renderer to the renderer component attached to this object.
                surfaceRenderer = GetComponent<Renderer>();
            // run the following statement only when surface collider is missing.
            if (surfaceCollider == null)
                // set surface collider to the collider component attached to this object.
                surfaceCollider = GetComponent<Collider>();

            // call ensure crosshair: create the writing crosshair once and reuse it to show the current
            // writing location.
            EnsureCrosshair();

            // run the following statement only when board texture exists.
            if (boardTexture != null)
                // leave this method now without running the remaining statements.
                return;

            // set texture width to the larger of 256 and texture width.
            textureWidth = Mathf.Max(256, textureWidth);
            // set texture height to the larger of 128 and texture height.
            textureHeight = Mathf.Max(128, textureHeight);
            // set board texture to a new texture2 d using texture width, texture height, texture format
            // rgba32, false, false.
            boardTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, false)
            // begin the object initializer expression values.
            {
                // set name to the text "xr study whiteboard runtime texture".
                name = "XR Study Whiteboard Runtime Texture",
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

            // run the following statement only when surface renderer exists.
            if (surfaceRenderer != null)
            // begin the if statement block.
            {
                // a material instance keeps the runtime texture local to this board.
                // store source material as surface renderer shared material.
                Material sourceMaterial = surfaceRenderer.sharedMaterial;
                // store target material as if source material exists, use a new material using source
                // material, otherwise use no object.
                Material targetMaterial = sourceMaterial != null ? new Material(sourceMaterial) : null;
                // set board material to target material.
                boardMaterial = targetMaterial;
                // set surface renderer shared material to target material.
                surfaceRenderer.sharedMaterial = targetMaterial;
                // run the following statement only when target material exists.
                if (targetMaterial != null)
                // begin the if statement block.
                {
                    // store unlit as the child or resource the text "universal render pipeline/unlit" found in
                    // shader.
                    Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                    // run the following statement only when unlit exists.
                    if (unlit != null) targetMaterial.shader = unlit;
                    // set target material colour to colour white.
                    targetMaterial.color = Color.white;
                    // set target material main texture to board texture.
                    targetMaterial.mainTexture = boardTexture;
                    // run the following statement only when target material: has property using the text
                    // "_basemap".
                    if (targetMaterial.HasProperty("_BaseMap"))
                        // target material: set texture using the text "_basemap", board texture.
                        targetMaterial.SetTexture("_BaseMap", boardTexture);
                // finish the if statement block.
                }
            // finish the if statement block.
            }
        // finish the method initialize surface block.
        }

        // method: try get uv.
        // find whether the ray reaches this writing surface and return its normalised two-dimensional texture
        // position.
        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        // begin the method try get uv block.
        {
            // set uv to default.
            uv = default;
            // run the following statement only when surface collider is missing.
            if (surfaceCollider == null)
                // return false to the caller.
                return false;

            // drawingsurface is a very thin generated boxcollider. a ray
            // grazing its edge can hit a side face for one frame; treating
            // that side point as a front-face uv creates the characteristic
            // vertical spikes and long joins in an otherwise good circle.
            // intersect only the visible +z face, matching the cursor plane,
            // and reject every side/back hit.
            // run the following statement only when surface collider matches box collider box.
            if (surfaceCollider is BoxCollider box)
            // begin the if statement block.
            {
                // store half size as box size multiplied by 0.5.
                Vector3 halfSize = box.size * 0.5f;
                // store front local point as box center plus the forward unit vector multiplied by half size
                // z.
                Vector3 frontLocalPoint = box.center + Vector3.forward * halfSize.z;
                // store board plane as a new plane using the forward unit vector converted from box transform
                // local direction into world direction, front local point converted from box transform local
                // coordinates into world coordinates.
                Plane boardPlane = new Plane(
                    // supply the forward unit vector converted from box transform local direction into world
                    // direction to the enclosing call.
                    box.transform.TransformDirection(Vector3.forward),
                    // supply front local point converted from box transform local coordinates into world
                    // coordinates to the enclosing call.
                    box.transform.TransformPoint(frontLocalPoint));
                // run the following statement only when the dot product of ray direction and board plane
                // normal is at least 0 or not (board plane: raycast using ray, the output value plane
                // distance) or plane distance is less than 0 or plane distance is greater than max distance.
                if (Vector3.Dot(ray.direction, boardPlane.normal) >= 0f
                    // the dot product of ray direction and board plane normal is at least 0 or not (board
                    // plane: raycast using ray, the output value plane distance).
                    || !boardPlane.Raycast(ray, out float planeDistance)
                    // the dot product of ray direction and board plane normal is at least 0 or not (board
                    // plane: raycast using ray, the output value plane distance) or plane distance is less
                    // than 0.
                    || planeDistance < 0f
                    // the dot product of ray direction and board plane normal is at least 0 or not (board
                    // plane: raycast using ray, the output value plane distance) or plane distance is less
                    // than 0 or plane distance is greater than max distance.
                    || planeDistance > maxDistance)
                    // return false to the caller.
                    return false;

                // store local point as the point plane distance units along ray converted from world
                // coordinates into box transform local coordinates.
                Vector3 localPoint = box.transform.InverseTransformPoint(ray.GetPoint(planeDistance));
                // run the following statement only when local point x is less than box center x minus half
                // size x or local point x is greater than box center x plus half size x or local point y is
                // less than box center y minus half size y or local point y is greater than box center y plus
                // half size y.
                if (localPoint.x < box.center.x - halfSize.x
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x.
                    || localPoint.x > box.center.x + halfSize.x
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x or local point y is less than box center y minus half
                    // size y.
                    || localPoint.y < box.center.y - halfSize.y
                    // local point x is less than box center x minus half size x or local point x is greater
                    // than box center x plus half size x or local point y is less than box center y minus half
                    // size y or local point y is greater than box center y plus half size y.
                    || localPoint.y > box.center.y + halfSize.y)
                    // return false to the caller.
                    return false;

                // store physical x as the proportion of local point x between box center x minus half size x
                // and box center x plus half size x.
                float physicalX = Mathf.InverseLerp(
                    // supply box center x minus half size x to the enclosing call.
                    box.center.x - halfSize.x,
                    // supply box center x plus half size x to the enclosing call.
                    box.center.x + halfSize.x,
                    // supply local point x to the enclosing call.
                    localPoint.x);
                // store physical y as the proportion of local point y between box center y minus half size y
                // and box center y plus half size y.
                float physicalY = Mathf.InverseLerp(
                    // supply box center y minus half size y to the enclosing call.
                    box.center.y - halfSize.y,
                    // supply box center y plus half size y to the enclosing call.
                    box.center.y + halfSize.y,
                    // supply local point y to the enclosing call.
                    localPoint.y);
                // the imported cube's visible +z face mirrors its texture
                // horizontally. return texture uvs in the same direction as
                // the visible board so a left-to-right hand movement is also
                // drawn left-to-right.
                // set uv to a new two-dimensional vector using if flip horizontal texture uv, use 1 minus
                // physical x, otherwise use physical x, physical y.
                uv = new Vector2(
                    // supply if flip horizontal texture uv, use 1 minus physical x, otherwise use physical x
                    // to the enclosing call.
                    flipHorizontalTextureUv ? 1f - physicalX : physicalX,
                    // supply physical y to the enclosing call.
                    physicalY);
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // run the following statement only when not (surface collider: raycast using ray, the output
                // value hit, max distance).
                if (!surfaceCollider.Raycast(ray, out RaycastHit hit, maxDistance))
                    // return false to the caller.
                    return false;
                // set uv to hit texture coord.
                uv = hit.textureCoord;
            // finish the else clause block.
            }
            // return uv x is at least 0 and uv x is at most 1 and uv y is at least 0 and uv y is at most 1 to
            // the caller.
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        // finish the method try get uv block.
        }

        // method: begin stroke.
        // start a new board stroke and emit its beginning for the recorder when live input is allowed.
        public void BeginStroke(Vector2 uv)
        // begin the method begin stroke block.
        {
            // run the following statement only when live input blocked.
            if (LiveInputBlocked)
                // leave this method now without running the remaining statements.
                return;
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
            // set stroke open to true.
            strokeOpen = true;
            // if drawing event exists, schedule whiteboard recording event type stroke begin after uv seconds.
            DrawingEvent?.Invoke(WhiteboardRecordingEventType.StrokeBegin, uv, false);
            // break stroke continuity so a later contact starts a separate line.
            hasPreviousPoint = false;
            // set input sample count to 0.
            inputSampleCount = 0;
            // set reacquire sample count to 0.
            reacquireSampleCount = 0;
            // draw point using uv.
            DrawPoint(uv);
        // finish the method begin stroke block.
        }

        // method: continue stroke.
        // accept the next writing point, reject unsafe jumps, smooth the input, and paint connected segments.
        public void ContinueStroke(Vector2 uv)
        // begin the method continue stroke block.
        {
            // continue stroke using uv, false.
            ContinueStroke(uv, false);
        // finish the method continue stroke block.
        }

        // method: continue stroke.
        // accept the next writing point, reject unsafe jumps, smooth the input, and paint connected segments.
        public void ContinueStroke(Vector2 uv, bool trustedDesktopInput)
        // begin the method continue stroke block.
        {
            // run the following statement only when live input blocked.
            if (LiveInputBlocked)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when not (stroke open).
            if (!strokeOpen)
            // begin the if statement block.
            {
                // begin stroke using uv.
                BeginStroke(uv);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }
            // set latest input is desktop to trusted desktop input.
            latestInputIsDesktop = trustedDesktopInput;
            // if drawing event exists, schedule whiteboard recording event type stroke point after uv seconds.
            DrawingEvent?.Invoke(WhiteboardRecordingEventType.StrokePoint, uv, trustedDesktopInput);
            // fill the gaps between samples so the stroke forms a connected line rather than separate dots.
            // run the following statement only when not (has previous point).
            if (!hasPreviousPoint)
            // begin the if statement block.
            {
                // run the following statement only when not (try reacquire surface using uv).
                if (!TryReacquireSurface(uv))
                    // leave this method now without running the remaining statements.
                    return;

                // draw point using uv.
                DrawPoint(uv);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store distance as the distance between previous uv and uv.
            float distance = Vector2.Distance(previousUv, uv);
            // a missed or re-aimed controller ray must not be joined with a
            // long straight segment. that segment is what appears as the
            // unwanted vertical line after a simulator pose jump.
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

            // store brush diameter as if manager exists and manager current tool equals whiteboard tool
            // eraser, use eraser size, otherwise use marker size.
            float brushDiameter = manager != null && manager.CurrentTool == WhiteboardTool.Eraser
                // if manager exists and manager current tool equals whiteboard tool eraser, use eraser size,
                // otherwise use marker size.
                ? eraserSize
                // if manager exists and manager current tool equals whiteboard tool eraser, use eraser size,
                // otherwise use marker size.
                : markerSize;
            // store brush radius in uv as brush diameter multiplied by 0.5 divided by the larger of board
            // world size x and board world size y.
            float brushRadiusInUv = (brushDiameter * 0.5f)
                // brush diameter multiplied by 0.5 divided by the larger of board world size x and board world
                // size y.
                / Mathf.Max(boardWorldSize.x, boardWorldSize.y);
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
            // paint each interpolated section as a capsule rather than a
            // row of circular stamps. this keeps the edge smooth even when
            // desktop/controller samples are quantised to whole pixels.
            // store segment start as previous uv.
            Vector2 segmentStart = previousUv;
            // repeat this loop while i is at most steps. increase i by one after each pass.
            for (int i = 1; i <= steps; i++)
            // begin the for statement block.
            {
                // store segment end as an interpolated value from previous uv toward uv using i divided by
                // steps converted to float.
                Vector2 segmentEnd = Vector2.Lerp(previousUv, uv, i / (float)steps);
                // stamp segment using segment start, segment end.
                StampSegment(segmentStart, segmentEnd);
                // set segment start to segment end.
                segmentStart = segmentEnd;
            // finish the for statement block.
            }

            // set previous uv to uv.
            previousUv = uv;
            // mark the pixel buffer as changed so late update uploads the new drawing this frame.
            textureDirty = true;
        // finish the method continue stroke block.
        }

        // method: end stroke.
        // close the current stroke and reset continuity so the next contact does not join an earlier line.
        public void EndStroke()
        // begin the method end stroke block.
        {
            // run the following statement only when live input blocked.
            if (LiveInputBlocked)
                // leave this method now without running the remaining statements.
                return;
            // run the following statement only when stroke open.
            if (strokeOpen)
                // if drawing event exists, schedule whiteboard recording event type stroke end after default
                // seconds.
                DrawingEvent?.Invoke(WhiteboardRecordingEventType.StrokeEnd, default, false);
            // call close stroke to latest input: finish the stroke at the latest raw input point so smoothing
            // does not leave a gap at the endpoint.
            CloseStrokeToLatestInput();
            // set stroke open to false.
            strokeOpen = false;
            // break stroke continuity so a later contact starts a separate line.
            hasPreviousPoint = false;
            // set input sample count to 0.
            inputSampleCount = 0;
            // set reacquire sample count to 0.
            reacquireSampleCount = 0;
        // finish the method end stroke block.
        }

        // method: clear board.
        // fill the board white, close the current stroke, and report a clear event to the recorder.
        public void ClearBoard()
        // begin the method clear board block.
        {
            // run the following statement only when live input blocked.
            if (LiveInputBlocked)
                // leave this method now without running the remaining statements.
                return;
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
            // call end stroke: close the current stroke and reset continuity so the next contact does not join
            // an earlier line.
            EndStroke();
            // if drawing event exists, schedule whiteboard recording event type clear after default seconds.
            DrawingEvent?.Invoke(WhiteboardRecordingEventType.Clear, default, false);
            // fill using colour white.
            Fill(Color.white);
            // clear the pending texture-change flag because this code is handling the upload directly.
            textureDirty = false;
            // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
            // pending-change flag.
            ApplyTexture();
        // finish the method clear board block.
        }

        // method: capture pixels.
        // copy the current board pixels for the starting snapshot of a recording.
        public Color32[] CapturePixels()
        // begin the method capture pixels block.
        {
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
            // return pixels: clone converted to 32-bit colour[] to the caller.
            return (Color32[])pixels.Clone();
        // finish the method capture pixels block.
        }

        // method: restore pixels.
        // replace the board with a valid saved starting snapshot before replay begins.
        internal void RestorePixels(Color32[] snapshot)
        // begin the method restore pixels block.
        {
            // call initialize surface: create the runtime texture and pixel buffer if needed, then connect the
            // visible writing surface.
            InitializeSurface();
            // set stroke open to false.
            strokeOpen = false;
            // break stroke continuity so a later contact starts a separate line.
            hasPreviousPoint = false;
            // set input sample count to 0.
            inputSampleCount = 0;
            // set reacquire sample count to 0.
            reacquireSampleCount = 0;
            // run the following statement only when snapshot exists and the number of items in snapshot equals
            // the number of items in pixels.
            if (snapshot != null && snapshot.Length == pixels.Length)
                // array: copy using snapshot, pixels, the number of items in pixels.
                Array.Copy(snapshot, pixels, pixels.Length);
            // handle the alternative when the preceding condition is false.
            else
                // fill using colour white.
                Fill(Color.white);
            // call apply texture: copy the pixel buffer into the texture and upload it, then clear the
            // pending-change flag.
            ApplyTexture();
        // finish the method restore pixels block.
        }

        // method: apply playback event.
        // apply a recorded stroke or clear event while temporarily allowing the replay brush through the
        // live-input block.
        internal void ApplyPlaybackEvent(WhiteboardRecordingEvent recordedEvent)
        // begin the method apply playback event block.
        {
            // set applying playback to true.
            applyingPlayback = true;
            // apply the replay event inside a protected block whose cleanup runs even if an operation fails.
            try
            // begin the try statement block.
            {
                // choose a branch using recorded event type.
                switch (recordedEvent.type)
                // begin the switch statement block.
                {
                    // handle the whiteboard recording event type stroke begin choice.
                    case WhiteboardRecordingEventType.StrokeBegin:
                        // begin stroke using recorded event position.
                        BeginStroke(recordedEvent.position);
                        // update cursor using recorded event position.
                        UpdateCursor(recordedEvent.position);
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the whiteboard recording event type stroke point choice.
                    case WhiteboardRecordingEventType.StrokePoint:
                        // continue stroke using recorded event position, recorded event trusted desktop input.
                        ContinueStroke(recordedEvent.position, recordedEvent.trustedDesktopInput);
                        // update cursor using recorded event position.
                        UpdateCursor(recordedEvent.position);
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the whiteboard recording event type stroke end choice.
                    case WhiteboardRecordingEventType.StrokeEnd:
                        // call end stroke: close the current stroke and reset continuity so the next contact
                        // does not join an earlier line.
                        EndStroke();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                    // handle the whiteboard recording event type clear choice.
                    case WhiteboardRecordingEventType.Clear:
                        // call clear board: fill the board white, close the current stroke, and report a clear
                        // event to the recorder.
                        ClearBoard();
                        // leave the nearest loop or switch after finding the required result.
                        break;
                // finish the switch statement block.
                }
            // finish the try statement block.
            }
            // always restore the playback-application flag when leaving the protected block.
            finally
            // begin the finally clause block.
            {
                // set applying playback to false.
                applyingPlayback = false;
            // finish the finally clause block.
            }
        // finish the method apply playback event block.
        }

        // method: update cursor.
        // place the crosshair at the current writing coordinates and adjust its appearance for the active
        // tool.
        public void UpdateCursor(Vector2 uv)
        // begin the method update cursor block.
        {
            // run the following statement only when live input blocked.
            if (LiveInputBlocked)
                // leave this method now without running the remaining statements.
                return;
            // call ensure crosshair: create the writing crosshair once and reuse it to show the current
            // writing location.
            EnsureCrosshair();
            // run the following statement only when crosshair is missing.
            if (crosshair == null)
                // leave this method now without running the remaining statements.
                return;

            // store physical x as if flip horizontal texture uv, use 1 minus uv x, otherwise use uv x.
            float physicalX = flipHorizontalTextureUv ? 1f - uv.x : uv.x;
            // store inherited scale as if surface renderer exists, use surface renderer transform lossy scale,
            // otherwise use unit scale on all three axes.
            Vector3 inheritedScale = surfaceRenderer != null
                // if surface renderer exists, use surface renderer transform lossy scale, otherwise use unit
                // scale on all three axes.
                ? surfaceRenderer.transform.lossyScale
                // if surface renderer exists, use surface renderer transform lossy scale, otherwise use unit
                // scale on all three axes.
                : Vector3.one;
            // store local board width as board world size x divided by the larger of 0.0001 and inherited
            // scale x.
            float localBoardWidth = boardWorldSize.x / Mathf.Max(0.0001f, inheritedScale.x);
            // store local board height as board world size y divided by the larger of 0.0001 and inherited
            // scale y.
            float localBoardHeight = boardWorldSize.y / Mathf.Max(0.0001f, inheritedScale.y);
            // store local position as a new three-dimensional vector using physical x minus 0.5 multiplied by
            // local board width, uv y minus 0.5 multiplied by local board height, get front surface local z.
            Vector3 localPosition = new Vector3(
                // supply physical x minus 0.5 multiplied by local board width to the enclosing call.
                (physicalX - 0.5f) * localBoardWidth,
                // supply uv y minus 0.5 multiplied by local board height to the enclosing call.
                (uv.y - 0.5f) * localBoardHeight,
                // supply get front surface local z to the enclosing call.
                GetFrontSurfaceLocalZ());
            // run the following statement only when cursor exists.
            if (cursor != null)
                // set cursor local position to local position.
                cursor.localPosition = localPosition;

            // store diameter as if manager exists and manager current tool equals whiteboard tool eraser, use
            // eraser size, otherwise use marker size.
            float diameter = manager != null && manager.CurrentTool == WhiteboardTool.Eraser ? eraserSize : markerSize;
            // run the following statement only when cursor exists.
            if (cursor != null)
                // set cursor local scale to unit scale on all three axes multiplied by diameter.
                cursor.localScale = Vector3.one * diameter;

            // run the following statement only when crosshair exists.
            if (crosshair != null)
            // begin the if statement block.
            {
                // set crosshair local position to local position.
                crosshair.localPosition = localPosition;
                // store span as the larger of 0.045 and diameter multiplied by 1.8.
                float span = Mathf.Max(0.045f, diameter * 1.8f);
                // store thickness as the larger of 0.0025 and diameter multiplied by 0.12.
                float thickness = Mathf.Max(0.0025f, diameter * 0.12f);
                // set crosshair bar scale using 0, span, thickness, 0.003.
                SetCrosshairBarScale(0, span, thickness, 0.003f);
                // set crosshair bar scale using 1, thickness, 0.003, span.
                SetCrosshairBarScale(1, thickness, 0.003f, span);
            // finish the if statement block.
            }

            // run the following statement only when cursor renderer exists.
            if (cursorRenderer != null)
                // set cursor renderer material colour to if manager exists, use manager: get current ink
                // colour, otherwise use colour black.
                cursorRenderer.material.color = manager != null ? manager.GetCurrentInkColour() : Color.black;
            // store cursor colour as a new colour using 0.05, 0.9, 1, 1.
            Color cursorColour = new Color(0.05f, 0.9f, 1f, 1f);
            // repeat this loop while i is less than the number of items in crosshair renderers. increase i by
            // one after each pass.
            for (int i = 0; i < crosshairRenderers.Count; i++)
                // run the following statement only when the entry at i in crosshair renderers exists.
                if (crosshairRenderers[i] != null)
                    // set the entry at i in crosshair renderers material colour to cursor colour.
                    crosshairRenderers[i].material.color = cursorColour;
        // finish the method update cursor block.
        }

        // method: get front surface local z.
        // find the visible front plane of the thin board collider for cursor placement.
        private float GetFrontSurfaceLocalZ()
        // begin the method get front surface local z block.
        {
            // store surface scale z as if surface renderer exists, use the absolute value of surface renderer
            // transform lossy scale z, otherwise use 1.
            float surfaceScaleZ = surfaceRenderer != null
                // if surface renderer exists, use the absolute value of surface renderer transform lossy scale
                // z, otherwise use 1.
                ? Mathf.Abs(surfaceRenderer.transform.lossyScale.z)
                // if surface renderer exists, use the absolute value of surface renderer transform lossy scale
                // z, otherwise use 1.
                : 1f;
            // the generated surface is a unit cube scaled very thin in z.
            // put the cursor just in front of its +z face so it is visible
            // without changing the ray/collider geometry.
            // return 0.5 plus 0.003 divided by the larger of 0.0001 and surface scale z to the caller.
            return 0.5f + 0.003f / Mathf.Max(0.0001f, surfaceScaleZ);
        // finish the method get front surface local z block.
        }

        // method: draw point.
        // paint one point and update the stroke position and pending texture state.
        private void DrawPoint(Vector2 uv)
        // begin the method draw point block.
        {
            // stamp using uv.
            Stamp(uv);
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
            // mark the pixel buffer as changed so late update uploads the new drawing this frame.
            textureDirty = true;
        // finish the method draw point block.
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
            // median filtering removes one-frame ray outliers; the bounded
            // low-pass step removes the remaining controller/cursor jitter
            // without reconnecting a missed surface hit.
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
            // fast final drag event by more than the brush radius, leaving a
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

            // store brush diameter as if manager exists and manager current tool equals whiteboard tool
            // eraser, use eraser size, otherwise use marker size.
            float brushDiameter = manager != null && manager.CurrentTool == WhiteboardTool.Eraser
                // if manager exists and manager current tool equals whiteboard tool eraser, use eraser size,
                // otherwise use marker size.
                ? eraserSize
                // if manager exists and manager current tool equals whiteboard tool eraser, use eraser size,
                // otherwise use marker size.
                : markerSize;
            // store brush radius in uv as brush diameter multiplied by 0.5 divided by the larger of board
            // world size x and board world size y.
            float brushRadiusInUv = (brushDiameter * 0.5f)
                // brush diameter multiplied by 0.5 divided by the larger of board world size x and board world
                // size y.
                / Mathf.Max(boardWorldSize.x, boardWorldSize.y);
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
                // stamp segment using segment start, segment end.
                StampSegment(segmentStart, segmentEnd);
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

        // method: stamp.
        // paint a single brush impression in the pixel buffer with a soft edge.
        private void Stamp(Vector2 uv)
        // begin the method stamp block.
        {
            // run the following statement only when pixels is missing.
            if (pixels == null)
                // call initialize surface: create the runtime texture and pixel buffer if needed, then connect
                // the visible writing surface.
                InitializeSurface();

            // store erasing as manager exists and manager current tool equals whiteboard tool eraser.
            bool erasing = manager != null && manager.CurrentTool == WhiteboardTool.Eraser;
            // store colour as if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if
            // manager exists, use manager: get current ink colour, otherwise use colour black converted to
            // 32-bit colour.
            Color32 colour = erasing
                // if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if manager
                // exists, use manager: get current ink colour, otherwise use colour black converted to 32-bit
                // colour.
                ? new Color32(255, 255, 255, 255)
                // if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if manager
                // exists, use manager: get current ink colour, otherwise use colour black converted to 32-bit
                // colour.
                : (Color32)(manager != null ? manager.GetCurrentInkColour() : Color.black);
            // store diameter as if erasing, use eraser size, otherwise use marker size.
            float diameter = erasing ? eraserSize : markerSize;
            // store radius x as the larger of 1 and diameter divided by board world size x multiplied by
            // texture width multiplied by 0.5.
            float radiusX = Mathf.Max(1f, (diameter / boardWorldSize.x) * textureWidth * 0.5f);
            // store radius y as the larger of 1 and diameter divided by board world size y multiplied by
            // texture height multiplied by 0.5.
            float radiusY = Mathf.Max(1f, (diameter / boardWorldSize.y) * textureHeight * 0.5f);
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
                    // run the following statement only when erasing.
                    if (erasing)
                    // begin the if statement block.
                    {
                        // store erasing existing as the entry at pixel index in pixels.
                        Color32 erasingExisting = pixels[pixelIndex];
                        // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated
                        // value from erasing existing r toward colour r using coverage rounded to the nearest
                        // integer converted to byte, an interpolated value from erasing existing g toward
                        // colour g using coverage rounded to the nearest integer converted to byte, an
                        // interpolated value from erasing existing b toward colour b using coverage rounded to
                        // the nearest integer converted to byte, 255.
                        pixels[pixelIndex] = new Color32(
                            // supply an interpolated value from erasing existing r toward colour r using
                            // coverage rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.r, colour.r, coverage)),
                            // supply an interpolated value from erasing existing g toward colour g using
                            // coverage rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.g, colour.g, coverage)),
                            // supply an interpolated value from erasing existing b toward colour b using
                            // coverage rounded to the nearest integer converted to byte to the enclosing call.
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.b, colour.b, coverage)),
                            // supply 255 to the enclosing call.
                            255);
                        // skip the rest of this loop pass and move to the next entry.
                        continue;
                    // finish the if statement block.
                    }

                    // store existing as the entry at pixel index in pixels.
                    Color32 existing = pixels[pixelIndex];
                    // store opacity as marker opacity limited to the range from zero to one multiplied by
                    // coverage.
                    float opacity = Mathf.Clamp01(markerOpacity) * coverage;
                    // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated
                    // value from existing r toward colour r using opacity rounded to the nearest integer
                    // converted to byte, an interpolated value from existing g toward colour g using opacity
                    // rounded to the nearest integer converted to byte, an interpolated value from existing b
                    // toward colour b using opacity rounded to the nearest integer converted to byte, 255.
                    pixels[pixelIndex] = new Color32(
                        // supply an interpolated value from existing r toward colour r using opacity rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, opacity)),
                        // supply an interpolated value from existing g toward colour g using opacity rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, opacity)),
                        // supply an interpolated value from existing b toward colour b using opacity rounded
                        // to the nearest integer converted to byte to the enclosing call.
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, opacity)),
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
        private void StampSegment(Vector2 startUv, Vector2 endUv)
        // begin the method stamp segment block.
        {
            // run the following statement only when pixels is missing.
            if (pixels == null)
                // call initialize surface: create the runtime texture and pixel buffer if needed, then connect
                // the visible writing surface.
                InitializeSurface();

            // store erasing as manager exists and manager current tool equals whiteboard tool eraser.
            bool erasing = manager != null && manager.CurrentTool == WhiteboardTool.Eraser;
            // store colour as if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if
            // manager exists, use manager: get current ink colour, otherwise use colour black converted to
            // 32-bit colour.
            Color32 colour = erasing
                // if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if manager
                // exists, use manager: get current ink colour, otherwise use colour black converted to 32-bit
                // colour.
                ? new Color32(255, 255, 255, 255)
                // if erasing, use a new 32-bit colour using 255, 255, 255, 255, otherwise use if manager
                // exists, use manager: get current ink colour, otherwise use colour black converted to 32-bit
                // colour.
                : (Color32)(manager != null ? manager.GetCurrentInkColour() : Color.black);
            // store diameter as if erasing, use eraser size, otherwise use marker size.
            float diameter = erasing ? eraserSize : markerSize;
            // store radius x as the larger of 1 and diameter divided by board world size x multiplied by
            // texture width multiplied by 0.5.
            float radiusX = Mathf.Max(1f, (diameter / boardWorldSize.x) * textureWidth * 0.5f);
            // store radius y as the larger of 1 and diameter divided by board world size y multiplied by
            // texture height multiplied by 0.5.
            float radiusY = Mathf.Max(1f, (diameter / boardWorldSize.y) * textureHeight * 0.5f);
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
                    // blend pixel using x, y, colour, coverage, erasing.
                    BlendPixel(x, y, colour, coverage, erasing);
                // finish the for statement block.
                }
            // finish the for statement block.
            }
        // finish the method stamp segment block.
        }

        // method: blend pixel.
        // combine the marker colour or white eraser with an existing pixel using edge coverage.
        private void BlendPixel(int x, int y, Color32 colour, float coverage, bool erasing)
        // begin the method blend pixel block.
        {
            // store pixel index as y multiplied by texture width plus x.
            int pixelIndex = y * textureWidth + x;
            // store existing as the entry at pixel index in pixels.
            Color32 existing = pixels[pixelIndex];
            // run the following statement only when erasing.
            if (erasing)
            // begin the if statement block.
            {
                // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated value
                // from existing r toward colour r using coverage rounded to the nearest integer converted to
                // byte, an interpolated value from existing g toward colour g using coverage rounded to the
                // nearest integer converted to byte, an interpolated value from existing b toward colour b
                // using coverage rounded to the nearest integer converted to byte, 255.
                pixels[pixelIndex] = new Color32(
                    // supply an interpolated value from existing r toward colour r using coverage rounded to
                    // the nearest integer converted to byte to the enclosing call.
                    (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                    // supply an interpolated value from existing g toward colour g using coverage rounded to
                    // the nearest integer converted to byte to the enclosing call.
                    (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                    // supply an interpolated value from existing b toward colour b using coverage rounded to
                    // the nearest integer converted to byte to the enclosing call.
                    (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                    // supply 255 to the enclosing call.
                    255);
                // leave this method now without running the remaining statements.
                return;
            // finish the if statement block.
            }

            // store opacity as marker opacity limited to the range from zero to one multiplied by coverage.
            float opacity = Mathf.Clamp01(markerOpacity) * coverage;
            // set the entry at pixel index in pixels to a new 32-bit colour using an interpolated value from
            // existing r toward colour r using opacity rounded to the nearest integer converted to byte, an
            // interpolated value from existing g toward colour g using opacity rounded to the nearest integer
            // converted to byte, an interpolated value from existing b toward colour b using opacity rounded
            // to the nearest integer converted to byte, 255.
            pixels[pixelIndex] = new Color32(
                // supply an interpolated value from existing r toward colour r using opacity rounded to the
                // nearest integer converted to byte to the enclosing call.
                (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, opacity)),
                // supply an interpolated value from existing g toward colour g using opacity rounded to the
                // nearest integer converted to byte to the enclosing call.
                (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, opacity)),
                // supply an interpolated value from existing b toward colour b using opacity rounded to the
                // nearest integer converted to byte to the enclosing call.
                (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, opacity)),
                // supply 255 to the enclosing call.
                255);
        // finish the method blend pixel block.
        }

        // method: ensure crosshair.
        // create the writing crosshair once and reuse it to show the current writing location.
        private void EnsureCrosshair()
        // begin the method ensure crosshair block.
        {
            // run the following statement only when crosshair exists or surface renderer is missing.
            if (crosshair != null || surfaceRenderer == null)
                // leave this method now without running the remaining statements.
                return;

            // run the following statement only when cursor exists.
            if (cursor != null)
                // hide and disable cursor game object.
                cursor.gameObject.SetActive(false);

            // set crosshair to a new game object using the text "drawingcrosshair" transform.
            crosshair = new GameObject("DrawingCrosshair").transform;
            // parent crosshair under surface renderer transform using local coordinates.
            crosshair.SetParent(surfaceRenderer.transform, false);
            // set crosshair local position to a new three-dimensional vector using 0, 0, get front surface
            // local z.
            crosshair.localPosition = new Vector3(0f, 0f, GetFrontSurfaceLocalZ());

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
            // store inherited scale as surface renderer transform lossy scale.
            Vector3 inheritedScale = surfaceRenderer.transform.lossyScale;
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
            // store inherited scale as surface renderer transform lossy scale.
            Vector3 inheritedScale = surfaceRenderer.transform.lossyScale;
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

        // method: apply texture.
        // copy the pixel buffer into the texture and upload it, then clear the pending-change flag.
        private void ApplyTexture()
        // begin the method apply texture block.
        {
            // run the following statement only when board texture is missing or pixels is missing.
            if (boardTexture == null || pixels == null)
                // leave this method now without running the remaining statements.
                return;

            // copy pixels into board texture before uploading the texture.
            boardTexture.SetPixels32(pixels);
            // upload changes to board texture.
            boardTexture.Apply(false, false);
            // clear the pending texture-change flag because this code is handling the upload directly.
            textureDirty = false;
        // finish the method apply texture block.
        }
    // finish the class whiteboard canvas block.
    }
// finish the namespace block.
}
