using System.Collections.Generic;
using UnityEngine;

namespace XRStudyWhiteboard
{
    /// <summary>
    /// Small Quest-friendly writing surface used by the student desk papers.
    /// It intentionally shares the same ray/trigger workflow as the main
    /// whiteboard, but keeps each student's notes on a separate texture.
    /// </summary>
    public sealed class PaperNoteCanvas : MonoBehaviour
    {
        private static readonly List<PaperNoteCanvas> ActiveNotes = new List<PaperNoteCanvas>();

        [SerializeField] private Renderer paperRenderer;
        [SerializeField] private Collider paperCollider;
        [SerializeField] private Vector2 paperWorldSize = new Vector2(0.55f, 0.38f);
        [SerializeField] private int textureWidth = 384;
        [SerializeField] private int textureHeight = 256;
        // Paper uses a fine pencil line, deliberately smaller than the
        // marker used by WhiteboardCanvas. This keeps a table note readable
        // instead of making it look like a second whiteboard.
        [SerializeField] private float pencilSize = 0.009f;
        [SerializeField] private float eraserSize = 0.032f;
        [SerializeField] private float interpolationSpacing = 0.00075f;
        [SerializeField] private int maximumInterpolationSteps = 1024;
        [SerializeField, Range(0.02f, 1f)] private float maximumUvJump = 0.18f;

        private Texture2D noteTexture;
        private Color32[] pixels;
        private Vector2 previousUv;
        private bool hasPreviousPoint;
        private Vector2 inputBeforeLastUv;
        private Vector2 lastInputUv;
        private int inputSampleCount;
        private bool previousStrokeWasErasing;
        private bool textureDirty;
        private Transform crosshair;
        private readonly List<Renderer> crosshairRenderers = new List<Renderer>();

        public static IReadOnlyList<PaperNoteCanvas> Notes => ActiveNotes;

        public void Configure(Renderer renderer, Collider collider, Vector2 worldSize)
        {
            paperRenderer = renderer;
            paperCollider = collider;
            paperWorldSize = new Vector2(Mathf.Max(0.05f, worldSize.x), Mathf.Max(0.05f, worldSize.y));
            EnsureCrosshair();
            InitializeSurface();
        }

        public void ConfigureWritingSizes(float finePencilSize, float wideEraserSize)
        {
            pencilSize = Mathf.Clamp(finePencilSize, 0.001f, 0.05f);
            eraserSize = Mathf.Clamp(wideEraserSize, pencilSize, 0.15f);
        }

        private void OnEnable()
        {
            if (!ActiveNotes.Contains(this))
                ActiveNotes.Add(this);
        }

        private void OnDisable()
        {
            ActiveNotes.Remove(this);
            EndStroke();
        }

        private void Awake()
        {
            if (paperRenderer == null)
                paperRenderer = GetComponent<Renderer>();
            if (paperCollider == null)
                paperCollider = GetComponent<Collider>();

            EnsureCrosshair();
            InitializeSurface();
        }

        private void OnDestroy()
        {
            if (noteTexture != null)
                Destroy(noteTexture);
        }

        private void LateUpdate()
        {
            if (textureDirty)
                ApplyTexture();
        }

        public void InitializeSurface()
        {
            if (noteTexture != null)
                return;

            textureWidth = Mathf.Max(128, textureWidth);
            textureHeight = Mathf.Max(128, textureHeight);
            noteTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, false)
            {
                name = "Student Paper Note Runtime Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            pixels = new Color32[textureWidth * textureHeight];
            Fill(Color.white);
            ApplyTexture();

            if (paperRenderer != null)
            {
                Material targetMaterial = Application.isPlaying
                    ? paperRenderer.material
                    : paperRenderer.sharedMaterial;
                if (targetMaterial != null)
                    targetMaterial.mainTexture = noteTexture;
            }
        }

        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        {
            uv = default;
            if (paperCollider == null)
                return false;

            // The generated papers use BoxColliders.  RaycastHit.textureCoord
            // is only reliable for mesh colliders, so calculate the point on
            // the paper's local X/Z plane for the box case.  This keeps
            // writing correct even after a paper has been grabbed and moved.
            if (paperCollider is BoxCollider box)
            {
                Vector3 halfSize = box.size * 0.5f;
                // Intersect the ray with the paper's top plane instead of
                // accepting a side face of the very thin box. A controller
                // ray that grazes an edge can otherwise alternate between
                // top and side hits, which turns a smooth stroke into dots
                // and near-vertical jumps.
                Vector3 topLocalPoint = box.center + Vector3.up * halfSize.y;
                Plane paperPlane = new Plane(
                    box.transform.TransformDirection(Vector3.up),
                    box.transform.TransformPoint(topLocalPoint));
                if (!paperPlane.Raycast(ray, out float planeDistance)
                    || planeDistance < 0f
                    || planeDistance > maxDistance)
                    return false;

                Vector3 localPoint = box.transform.InverseTransformPoint(ray.GetPoint(planeDistance));
                if (localPoint.x < box.center.x - halfSize.x
                    || localPoint.x > box.center.x + halfSize.x
                    || localPoint.z < box.center.z - halfSize.z
                    || localPoint.z > box.center.z + halfSize.z)
                    return false;

                uv = new Vector2(
                    Mathf.InverseLerp(box.center.x - halfSize.x, box.center.x + halfSize.x, localPoint.x),
                    Mathf.InverseLerp(box.center.z - halfSize.z, box.center.z + halfSize.z, localPoint.z));
            }
            else
            {
                if (!paperCollider.Raycast(ray, out RaycastHit hit, maxDistance))
                    return false;
                uv = hit.textureCoord;
            }

            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        public void DrawAtUV(Vector2 uv, bool erasing)
        {
            InitializeSurface();
            if (!hasPreviousPoint || previousStrokeWasErasing != erasing)
            {
                previousStrokeWasErasing = erasing;
                inputSampleCount = 0;
                Stamp(uv, erasing);
                previousUv = uv;
                hasPreviousPoint = true;
                inputBeforeLastUv = uv;
                lastInputUv = uv;
                inputSampleCount = 1;
                textureDirty = true;
                return;
            }

            Vector2 filteredUv = FilterInputPoint(uv);
            float distance = Vector2.Distance(previousUv, uv);
            // Do not connect two unrelated controller hits with a long
            // segment when the thin paper collider is briefly missed.
            if (distance > maximumUvJump)
            {
                hasPreviousPoint = false;
                inputSampleCount = 0;
                return;
            }

            distance = Vector2.Distance(previousUv, filteredUv);
            uv = filteredUv;

            float brushDiameter = erasing ? eraserSize : pencilSize;
            float brushRadiusInUv = (brushDiameter * 0.5f)
                / Mathf.Max(paperWorldSize.x, paperWorldSize.y);
            float spacing = Mathf.Min(
                Mathf.Max(0.00025f, interpolationSpacing),
                Mathf.Max(0.00025f, brushRadiusInUv * 0.4f));
            int steps = Mathf.Clamp(
                Mathf.CeilToInt(distance / spacing),
                1,
                Mathf.Max(1, maximumInterpolationSteps));
            for (int i = 1; i <= steps; i++)
                Stamp(Vector2.Lerp(previousUv, uv, i / (float)steps), erasing);

            previousUv = uv;
            textureDirty = true;
        }

        public void UpdateCursor(Vector2 uv)
        {
            EnsureCrosshair();
            if (crosshair == null)
                return;

            Vector3 inheritedScale = paperRenderer != null
                ? paperRenderer.transform.lossyScale
                : Vector3.one;
            float localPaperWidth = paperWorldSize.x / Mathf.Max(0.0001f, inheritedScale.x);
            float localPaperDepth = paperWorldSize.y / Mathf.Max(0.0001f, inheritedScale.z);
            crosshair.localPosition = new Vector3(
                (uv.x - 0.5f) * localPaperWidth,
                0.65f,
                (uv.y - 0.5f) * localPaperDepth);

            Color colour = new Color(0.05f, 0.9f, 1f, 1f);
            for (int i = 0; i < crosshairRenderers.Count; i++)
                if (crosshairRenderers[i] != null)
                    crosshairRenderers[i].material.color = colour;

            float diameter = PaperTool.IsEraserActive ? eraserSize : pencilSize;
            float span = Mathf.Max(0.018f, diameter * 2.2f);
            float thickness = Mathf.Max(0.0015f, diameter * 0.18f);
            SetCrosshairBarScale(0, span, thickness, 0.002f);
            SetCrosshairBarScale(1, thickness, 0.002f, span);
        }

        public void EndStroke()
        {
            hasPreviousPoint = false;
            inputSampleCount = 0;
        }

        public void ClearNote()
        {
            InitializeSurface();
            Fill(Color.white);
            textureDirty = false;
            ApplyTexture();
            EndStroke();
        }

        public static bool TryGetNearest(Ray ray, float maxDistance, out PaperNoteCanvas note, out Vector2 uv)
        {
            note = null;
            uv = default;
            float nearestDistance = maxDistance;

            for (int i = ActiveNotes.Count - 1; i >= 0; i--)
            {
                PaperNoteCanvas candidate = ActiveNotes[i];
                if (candidate == null)
                {
                    ActiveNotes.RemoveAt(i);
                    continue;
                }

                if (!candidate.TryGetUV(ray, nearestDistance, out Vector2 candidateUv))
                    continue;

                if (!candidate.paperCollider.Raycast(ray, out RaycastHit hit, nearestDistance))
                    continue;

                nearestDistance = hit.distance;
                note = candidate;
                uv = candidateUv;
            }

            return note != null;
        }

        private void Stamp(Vector2 uv, bool erasing)
        {
            if (pixels == null)
                InitializeSurface();

            Color32 colour = erasing ? new Color32(255, 255, 255, 255) : new Color32(20, 25, 35, 255);
            float diameter = erasing ? eraserSize : pencilSize;
            int radiusX = Mathf.Max(1, Mathf.RoundToInt((diameter / paperWorldSize.x) * textureWidth * 0.5f));
            int radiusY = Mathf.Max(1, Mathf.RoundToInt((diameter / paperWorldSize.y) * textureHeight * 0.5f));
            int centerX = Mathf.RoundToInt(Mathf.Clamp01(uv.x) * (textureWidth - 1));
            int centerY = Mathf.RoundToInt(Mathf.Clamp01(uv.y) * (textureHeight - 1));

            int minX = Mathf.Max(0, centerX - radiusX);
            int maxX = Mathf.Min(textureWidth - 1, centerX + radiusX);
            int minY = Mathf.Max(0, centerY - radiusY);
            int maxY = Mathf.Min(textureHeight - 1, centerY + radiusY);
            float radiusXSquared = radiusX * radiusX;
            float radiusYSquared = radiusY * radiusY;

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - centerY;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    if ((dx * dx) / radiusXSquared + (dy * dy) / radiusYSquared <= 1f)
                        pixels[y * textureWidth + x] = colour;
                }
            }
        }

        private Vector2 FilterInputPoint(Vector2 uv)
        {
            if (inputSampleCount <= 0)
            {
                inputBeforeLastUv = uv;
                lastInputUv = uv;
                inputSampleCount = 1;
                return uv;
            }

            if (inputSampleCount == 1)
            {
                inputBeforeLastUv = lastInputUv;
                lastInputUv = uv;
                inputSampleCount = 2;
                return uv;
            }

            Vector2 filtered = new Vector2(
                Median(inputBeforeLastUv.x, lastInputUv.x, uv.x),
                Median(inputBeforeLastUv.y, lastInputUv.y, uv.y));
            inputBeforeLastUv = lastInputUv;
            lastInputUv = uv;
            return filtered;
        }

        private static float Median(float a, float b, float c)
        {
            return a + b + c - Mathf.Min(a, Mathf.Min(b, c)) - Mathf.Max(a, Mathf.Max(b, c));
        }

        private void EnsureCrosshair()
        {
            if (crosshair != null || paperRenderer == null)
                return;

            crosshair = new GameObject("PaperWritingCrosshair").transform;
            crosshair.SetParent(paperRenderer.transform, false);

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
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
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

        private void Fill(Color colour)
        {
            Color32 fill = colour;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;
        }

        private void SetCrosshairBarScale(int index, float x, float y, float z)
        {
            if (index < 0 || index >= crosshairRenderers.Count || crosshairRenderers[index] == null)
                return;

            Transform bar = crosshairRenderers[index].transform;
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
            bar.localScale = new Vector3(
                x / Mathf.Max(0.0001f, inheritedScale.x),
                y / Mathf.Max(0.0001f, inheritedScale.y),
                z / Mathf.Max(0.0001f, inheritedScale.z));
        }

        private void ApplyTexture()
        {
            if (noteTexture == null || pixels == null)
                return;

            noteTexture.SetPixels32(pixels);
            noteTexture.Apply(false, false);
            textureDirty = false;
        }
    }
}
