using System.Collections.Generic;
using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Owns the lightweight CPU texture used by the board. The texture is
    /// intentionally small enough for Quest while still producing connected,
    /// readable strokes.
    /// </summary>
    public sealed class WhiteboardCanvas : MonoBehaviour
    {
        [Header("Board surface")]
        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private Collider surfaceCollider;
        [SerializeField] private Transform cursor;
        [SerializeField] private Renderer cursorRenderer;
        [SerializeField] private Vector2 boardWorldSize = new Vector2(3f, 1.5f);
        [SerializeField] private bool flipHorizontalTextureUv = true;

        [Header("Drawing")]
        [SerializeField] private int textureWidth = 768;
        [SerializeField] private int textureHeight = 384;
        [SerializeField] private float markerSize = 0.028f;
        [SerializeField] private float eraserSize = 0.06f;
        [SerializeField, Range(0.1f, 1f)] private float markerOpacity = 0.82f;
        [SerializeField] private float interpolationSpacing = 0.0006f;
        [SerializeField] private int maximumInterpolationSteps = 2048;
        [SerializeField, Range(0.02f, 1f)] private float maximumUvJump = 0.18f;
        [SerializeField, Range(0.35f, 1f)] private float strokePointSmoothing = 0.68f;

        private Texture2D boardTexture;
        private Color32[] pixels;
        private XRStudyWhiteboardManager manager;
        private Vector2 previousUv;
        private bool hasPreviousPoint;
        private Vector2 inputBeforeLastUv;
        private Vector2 lastInputUv;
        private Vector2 filteredInputUv;
        private int inputSampleCount;
        private bool textureDirty;
        private Transform crosshair;
        private readonly List<Renderer> crosshairRenderers = new List<Renderer>();

        public float MarkerSize => markerSize;
        public float EraserSize => eraserSize;
        public Vector2 BoardWorldSize => boardWorldSize;

        private void Awake()
        {
            InitializeSurface();
        }

        private void OnDestroy()
        {
            if (boardTexture != null)
                Destroy(boardTexture);
        }

        private void LateUpdate()
        {
            // Upload one completed brush buffer per frame. Applying a full
            // CPU texture from every mouse/controller sample was the source
            // of the editor lag and separated dot-like strokes.
            if (textureDirty)
                ApplyTexture();
        }

        public void SetManager(XRStudyWhiteboardManager whiteboardManager)
        {
            manager = whiteboardManager;
        }

        public void ConfigureBoardWorldSize(Vector2 worldSize)
        {
            boardWorldSize = new Vector2(Mathf.Max(0.1f, worldSize.x), Mathf.Max(0.1f, worldSize.y));
        }

        public void InitializeSurface()
        {
            if (surfaceRenderer == null)
                surfaceRenderer = GetComponent<Renderer>();
            if (surfaceCollider == null)
                surfaceCollider = GetComponent<Collider>();

            EnsureCrosshair();

            if (boardTexture != null)
                return;

            textureWidth = Mathf.Max(256, textureWidth);
            textureHeight = Mathf.Max(128, textureHeight);
            boardTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, false)
            {
                name = "XR Study Whiteboard Runtime Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            pixels = new Color32[textureWidth * textureHeight];
            Fill(Color.white);
            ApplyTexture();

            if (surfaceRenderer != null)
            {
                // A material instance keeps the runtime texture local to this board.
                Material targetMaterial = Application.isPlaying
                    ? surfaceRenderer.material
                    : surfaceRenderer.sharedMaterial;
                if (targetMaterial != null)
                    targetMaterial.mainTexture = boardTexture;
            }
        }

        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        {
            uv = default;
            if (surfaceCollider == null)
                return false;

            // DrawingSurface is a very thin generated BoxCollider. A ray
            // grazing its edge can hit a side face for one frame; treating
            // that side point as a front-face UV creates the characteristic
            // vertical spikes and long joins in an otherwise good circle.
            // Intersect only the visible +Z face, matching the cursor plane,
            // and reject every side/back hit.
            if (surfaceCollider is BoxCollider box)
            {
                Vector3 halfSize = box.size * 0.5f;
                Vector3 frontLocalPoint = box.center + Vector3.forward * halfSize.z;
                Plane boardPlane = new Plane(
                    box.transform.TransformDirection(Vector3.forward),
                    box.transform.TransformPoint(frontLocalPoint));
                if (!boardPlane.Raycast(ray, out float planeDistance)
                    || planeDistance < 0f
                    || planeDistance > maxDistance)
                    return false;

                Vector3 localPoint = box.transform.InverseTransformPoint(ray.GetPoint(planeDistance));
                if (localPoint.x < box.center.x - halfSize.x
                    || localPoint.x > box.center.x + halfSize.x
                    || localPoint.y < box.center.y - halfSize.y
                    || localPoint.y > box.center.y + halfSize.y)
                    return false;

                float physicalX = Mathf.InverseLerp(
                    box.center.x - halfSize.x,
                    box.center.x + halfSize.x,
                    localPoint.x);
                float physicalY = Mathf.InverseLerp(
                    box.center.y - halfSize.y,
                    box.center.y + halfSize.y,
                    localPoint.y);
                // The imported cube's visible +Z face mirrors its texture
                // horizontally. Return texture UVs in the same direction as
                // the visible board so a left-to-right hand movement is also
                // drawn left-to-right.
                uv = new Vector2(
                    flipHorizontalTextureUv ? 1f - physicalX : physicalX,
                    physicalY);
            }
            else
            {
                if (!surfaceCollider.Raycast(ray, out RaycastHit hit, maxDistance))
                    return false;
                uv = hit.textureCoord;
            }
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        public void BeginStroke(Vector2 uv)
        {
            hasPreviousPoint = false;
            inputSampleCount = 0;
            DrawPoint(uv);
        }

        public void ContinueStroke(Vector2 uv)
        {
            if (!hasPreviousPoint)
            {
                DrawPoint(uv);
                return;
            }

            Vector2 filteredUv = FilterInputPoint(uv);

            float distance = Vector2.Distance(previousUv, uv);
            // A missed or re-aimed controller ray must not be joined with a
            // long straight segment. That segment is what appears as the
            // unwanted vertical line after a simulator pose jump.
            if (distance > maximumUvJump)
            {
                hasPreviousPoint = false;
                inputSampleCount = 0;
                return;
            }

            distance = Vector2.Distance(previousUv, filteredUv);
            uv = filteredUv;

            float brushDiameter = manager != null && manager.CurrentTool == WhiteboardTool.Eraser
                ? eraserSize
                : markerSize;
            float brushRadiusInUv = (brushDiameter * 0.5f)
                / Mathf.Max(boardWorldSize.x, boardWorldSize.y);
            float spacing = Mathf.Min(
                Mathf.Max(0.00025f, interpolationSpacing),
                Mathf.Max(0.00025f, brushRadiusInUv * 0.4f));
            int steps = Mathf.Clamp(
                Mathf.CeilToInt(distance / spacing),
                1,
                Mathf.Max(1, maximumInterpolationSteps));
            // Stamp every point along the segment, not only the sampled
            // controller positions. This closes gaps when the ray moves
            // quickly and makes a full circle read as one continuous stroke.
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Stamp(Vector2.Lerp(previousUv, uv, t));
            }

            previousUv = uv;
            textureDirty = true;
        }

        public void EndStroke()
        {
            hasPreviousPoint = false;
            inputSampleCount = 0;
        }

        public void ClearBoard()
        {
            InitializeSurface();
            Fill(Color.white);
            textureDirty = false;
            ApplyTexture();
            EndStroke();
        }

        public void UpdateCursor(Vector2 uv)
        {
            EnsureCrosshair();
            if (crosshair == null)
                return;

            float physicalX = flipHorizontalTextureUv ? 1f - uv.x : uv.x;
            Vector3 inheritedScale = surfaceRenderer != null
                ? surfaceRenderer.transform.lossyScale
                : Vector3.one;
            float localBoardWidth = boardWorldSize.x / Mathf.Max(0.0001f, inheritedScale.x);
            float localBoardHeight = boardWorldSize.y / Mathf.Max(0.0001f, inheritedScale.y);
            Vector3 localPosition = new Vector3(
                (physicalX - 0.5f) * localBoardWidth,
                (uv.y - 0.5f) * localBoardHeight,
                GetFrontSurfaceLocalZ());
            if (cursor != null)
                cursor.localPosition = localPosition;

            float diameter = manager != null && manager.CurrentTool == WhiteboardTool.Eraser ? eraserSize : markerSize;
            if (cursor != null)
                cursor.localScale = Vector3.one * diameter;

            if (crosshair != null)
            {
                crosshair.localPosition = localPosition;
                float span = Mathf.Max(0.045f, diameter * 1.8f);
                float thickness = Mathf.Max(0.0025f, diameter * 0.12f);
                SetCrosshairBarScale(0, span, thickness, 0.003f);
                SetCrosshairBarScale(1, thickness, 0.003f, span);
            }

            if (cursorRenderer != null)
                cursorRenderer.material.color = manager != null ? manager.GetCurrentInkColour() : Color.black;
            Color cursorColour = new Color(0.05f, 0.9f, 1f, 1f);
            for (int i = 0; i < crosshairRenderers.Count; i++)
                if (crosshairRenderers[i] != null)
                    crosshairRenderers[i].material.color = cursorColour;
        }

        private float GetFrontSurfaceLocalZ()
        {
            float surfaceScaleZ = surfaceRenderer != null
                ? Mathf.Abs(surfaceRenderer.transform.lossyScale.z)
                : 1f;
            // The generated surface is a unit cube scaled very thin in Z.
            // Put the cursor just in front of its +Z face so it is visible
            // without changing the ray/collider geometry.
            return 0.5f + 0.003f / Mathf.Max(0.0001f, surfaceScaleZ);
        }

        private void DrawPoint(Vector2 uv)
        {
            Stamp(uv);
            previousUv = uv;
            hasPreviousPoint = true;
            inputBeforeLastUv = uv;
            lastInputUv = uv;
            filteredInputUv = uv;
            inputSampleCount = 1;
            textureDirty = true;
        }

        private Vector2 FilterInputPoint(Vector2 uv)
        {
            if (inputSampleCount <= 0)
            {
                inputBeforeLastUv = uv;
                lastInputUv = uv;
                filteredInputUv = uv;
                inputSampleCount = 1;
                return uv;
            }

            Vector2 candidate = inputSampleCount == 1
                ? uv
                : new Vector2(
                    Median(inputBeforeLastUv.x, lastInputUv.x, uv.x),
                    Median(inputBeforeLastUv.y, lastInputUv.y, uv.y));
            // Median filtering removes one-frame ray outliers; the bounded
            // low-pass step removes the remaining controller/cursor jitter
            // without reconnecting a missed surface hit.
            Vector2 filtered = Vector2.Lerp(filteredInputUv, candidate, strokePointSmoothing);
            inputBeforeLastUv = lastInputUv;
            lastInputUv = uv;
            filteredInputUv = filtered;
            inputSampleCount = Mathf.Min(inputSampleCount + 1, 3);
            return filtered;
        }

        private static float Median(float a, float b, float c)
        {
            return a + b + c - Mathf.Min(a, Mathf.Min(b, c)) - Mathf.Max(a, Mathf.Max(b, c));
        }

        private void Stamp(Vector2 uv)
        {
            if (pixels == null)
                InitializeSurface();

            bool erasing = manager != null && manager.CurrentTool == WhiteboardTool.Eraser;
            Color32 colour = erasing
                ? new Color32(255, 255, 255, 255)
                : (Color32)(manager != null ? manager.GetCurrentInkColour() : Color.black);
            float diameter = erasing ? eraserSize : markerSize;
            float radiusX = Mathf.Max(1f, (diameter / boardWorldSize.x) * textureWidth * 0.5f);
            float radiusY = Mathf.Max(1f, (diameter / boardWorldSize.y) * textureHeight * 0.5f);
            float centerX = Mathf.Clamp01(uv.x) * (textureWidth - 1);
            float centerY = Mathf.Clamp01(uv.y) * (textureHeight - 1);

            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX - 1f));
            int maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(centerX + radiusX + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY - 1f));
            int maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(centerY + radiusY + 1f));

            float radiusXSquared = radiusX * radiusX;
            float radiusYSquared = radiusY * radiusY;
            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    float normalizedDistance = Mathf.Sqrt(
                        (dx * dx) / radiusXSquared + (dy * dy) / radiusYSquared);
                    if (normalizedDistance > 1.05f)
                        continue;

                    float coverage = normalizedDistance <= 0.86f
                        ? 1f
                        : 1f - Mathf.InverseLerp(0.86f, 1.05f, normalizedDistance);

                    int pixelIndex = y * textureWidth + x;
                    if (erasing)
                    {
                        Color32 erasingExisting = pixels[pixelIndex];
                        pixels[pixelIndex] = new Color32(
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.r, colour.r, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.g, colour.g, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(erasingExisting.b, colour.b, coverage)),
                            255);
                        continue;
                    }

                    Color32 existing = pixels[pixelIndex];
                    float opacity = Mathf.Clamp01(markerOpacity) * coverage;
                    pixels[pixelIndex] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, opacity)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, opacity)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, opacity)),
                        255);
                }
            }
        }

        private void EnsureCrosshair()
        {
            if (crosshair != null || surfaceRenderer == null)
                return;

            if (cursor != null)
                cursor.gameObject.SetActive(false);

            crosshair = new GameObject("DrawingCrosshair").transform;
            crosshair.SetParent(surfaceRenderer.transform, false);
            crosshair.localPosition = new Vector3(0f, 0f, GetFrontSurfaceLocalZ());

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            Material material = shader != null ? new Material(shader) : null;
            CreateCrosshairBar("Horizontal", new Vector3(0.07f, 0.005f, 0.005f), material);
            CreateCrosshairBar("Vertical", new Vector3(0.005f, 0.005f, 0.07f), material);
        }

        private void CreateCrosshairBar(string name, Vector3 worldScale, Material material)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(crosshair, false);
            Vector3 inheritedScale = surfaceRenderer.transform.lossyScale;
            bar.transform.localScale = new Vector3(
                worldScale.x / Mathf.Max(0.0001f, inheritedScale.x),
                worldScale.y / Mathf.Max(0.0001f, inheritedScale.y),
                worldScale.z / Mathf.Max(0.0001f, inheritedScale.z));
            Collider collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }
            Renderer renderer = bar.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null)
                {
                    if (Application.isPlaying)
                        renderer.material = material;
                    else
                        renderer.sharedMaterial = material;
                }
                crosshairRenderers.Add(renderer);
            }
        }

        private void SetCrosshairBarScale(int index, float x, float y, float z)
        {
            if (index < 0 || index >= crosshairRenderers.Count || crosshairRenderers[index] == null)
                return;

            Transform bar = crosshairRenderers[index].transform;
            Vector3 inheritedScale = surfaceRenderer.transform.lossyScale;
            bar.localScale = new Vector3(
                x / Mathf.Max(0.0001f, inheritedScale.x),
                y / Mathf.Max(0.0001f, inheritedScale.y),
                z / Mathf.Max(0.0001f, inheritedScale.z));
        }

        private void Fill(Color colour)
        {
            Color32 fill = colour;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;
        }

        private void ApplyTexture()
        {
            if (boardTexture == null || pixels == null)
                return;

            boardTexture.SetPixels32(pixels);
            boardTexture.Apply(false, false);
            textureDirty = false;
        }
    }
}
