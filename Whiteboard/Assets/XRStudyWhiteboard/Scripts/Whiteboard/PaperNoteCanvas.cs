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
        [SerializeField] private int textureWidth = 768;
        [SerializeField] private int textureHeight = 512;
        // Paper uses a fine pencil line, deliberately smaller than the
        // marker used by WhiteboardCanvas. This keeps a table note readable
        // instead of making it look like a second whiteboard.
        [SerializeField] private float pencilSize = 0.01f;
        [SerializeField] private float eraserSize = 0.032f;
        [SerializeField] private float interpolationSpacing = 0.0005f;
        [SerializeField] private int maximumInterpolationSteps = 4096;
        [SerializeField, Range(0.02f, 1f)] private float maximumUvJump = 0.08f;
        [SerializeField, Range(0.35f, 1f)] private float strokePointSmoothing = 0.9f;

        private Texture2D noteTexture;
        private Color32[] pixels;
        private Vector2 previousUv;
        private bool hasPreviousPoint;
        private Vector2 inputBeforeLastUv;
        private Vector2 lastInputUv;
        private Vector2 filteredInputUv;
        private int inputSampleCount;
        private Vector2 reacquireAnchorUv;
        private int reacquireSampleCount;
        private bool previousStrokeWasErasing;
        private bool textureDirty;
        private Renderer writingSurfaceRenderer;
        private Material writingSurfaceMaterial;
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
            if (writingSurfaceMaterial != null)
                Destroy(writingSurfaceMaterial);
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
            {
                EnsureWritingSurface();
                return;
            }

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
            EnsureWritingSurface();
        }

        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        {
            return TryGetPaperIntersection(ray, maxDistance, out _, out uv);
        }

        public void DrawAtUV(Vector2 uv, bool erasing)
        {
            DrawAtUV(uv, erasing, false);
        }

        public void DrawAtUV(Vector2 uv, bool erasing, bool trustedDesktopInput)
        {
            InitializeSurface();
            if (!hasPreviousPoint)
            {
                if (reacquireSampleCount > 0 && !TryReacquireSurface(uv))
                {
                    return;
                }

                previousStrokeWasErasing = erasing;
                inputSampleCount = 0;
                Stamp(uv, erasing);
                previousUv = uv;
                hasPreviousPoint = true;
                inputBeforeLastUv = uv;
                lastInputUv = uv;
                filteredInputUv = uv;
                inputSampleCount = 1;
                reacquireSampleCount = 0;
                textureDirty = true;
                return;
            }

            if (previousStrokeWasErasing != erasing)
            {
                previousStrokeWasErasing = erasing;
                inputSampleCount = 0;
                Stamp(uv, erasing);
                previousUv = uv;
                inputBeforeLastUv = uv;
                lastInputUv = uv;
                filteredInputUv = uv;
                inputSampleCount = 1;
                textureDirty = true;
                return;
            }

            float distance = Vector2.Distance(previousUv, uv);
            // Do not connect two unrelated controller hits with a long
            // segment when the thin paper collider is briefly missed.
            if (!trustedDesktopInput && distance > maximumUvJump)
            {
                hasPreviousPoint = false;
                inputSampleCount = 0;
                reacquireAnchorUv = uv;
                reacquireSampleCount = 1;
                return;
            }

            Vector2 filteredUv = FilterInputPoint(uv);

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
            Vector2 segmentStart = previousUv;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 segmentEnd = Vector2.Lerp(previousUv, uv, i / (float)steps);
                StampSegment(segmentStart, segmentEnd, erasing);
                segmentStart = segmentEnd;
            }

            previousUv = uv;
            textureDirty = true;
        }

        private bool TryReacquireSurface(Vector2 uv)
        {
            if (reacquireSampleCount <= 0)
            {
                reacquireAnchorUv = uv;
                reacquireSampleCount = 1;
                return false;
            }

            if (Vector2.Distance(reacquireAnchorUv, uv) > maximumUvJump)
            {
                reacquireAnchorUv = uv;
                reacquireSampleCount = 1;
                return false;
            }

            reacquireAnchorUv = uv;
            reacquireSampleCount++;
            if (reacquireSampleCount < 3)
                return false;

            reacquireSampleCount = 0;
            return true;
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
            CloseStrokeToLatestInput();
            hasPreviousPoint = false;
            inputSampleCount = 0;
            reacquireSampleCount = 0;
        }

        public void ClearNote()
        {
            InitializeSurface();
            EndStroke();
            Fill(Color.white);
            textureDirty = false;
            ApplyTexture();
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

                if (!candidate.TryGetPaperIntersection(
                        ray,
                        nearestDistance,
                        out float candidateDistance,
                        out Vector2 candidateUv))
                    continue;

                nearestDistance = candidateDistance;
                note = candidate;
                uv = candidateUv;
            }

            return note != null;
        }

        private bool TryGetPaperIntersection(Ray ray, float maxDistance, out float distance, out Vector2 uv)
        {
            distance = 0f;
            uv = default;
            if (paperCollider == null)
                return false;

            // The generated papers use BoxColliders. RaycastHit.textureCoord
            // is only reliable for mesh colliders, so calculate both the
            // nearest hit distance and UV on the top plane. Keeping these
            // values from the same intersection prevents a side-face hit
            // from selecting the paper while the UV comes from its top.
            if (paperCollider is BoxCollider box)
            {
                Vector3 halfSize = box.size * 0.5f;
                Vector3 topLocalPoint = box.center + Vector3.up * halfSize.y;
                Plane paperPlane = new Plane(
                    box.transform.TransformDirection(Vector3.up),
                    box.transform.TransformPoint(topLocalPoint));
                if (!paperPlane.Raycast(ray, out distance)
                    || distance < 0f
                    || distance > maxDistance)
                    return false;

                Vector3 localPoint = box.transform.InverseTransformPoint(ray.GetPoint(distance));
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
                distance = hit.distance;
                uv = hit.textureCoord;
            }

            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        private void Stamp(Vector2 uv, bool erasing)
        {
            if (pixels == null)
                InitializeSurface();

            Color32 colour = erasing ? new Color32(255, 255, 255, 255) : new Color32(20, 25, 35, 255);
            float diameter = erasing ? eraserSize : pencilSize;
            float radiusX = Mathf.Max(1f, (diameter / paperWorldSize.x) * textureWidth * 0.5f);
            float radiusY = Mathf.Max(1f, (diameter / paperWorldSize.y) * textureHeight * 0.5f);
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
                    Color32 existing = pixels[pixelIndex];
                    pixels[pixelIndex] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                        255);
                }
            }
        }

        private void StampSegment(Vector2 startUv, Vector2 endUv, bool erasing)
        {
            if (pixels == null)
                InitializeSurface();

            Color32 colour = erasing ? new Color32(255, 255, 255, 255) : new Color32(20, 25, 35, 255);
            float diameter = erasing ? eraserSize : pencilSize;
            float radiusX = Mathf.Max(1f, (diameter / paperWorldSize.x) * textureWidth * 0.5f);
            float radiusY = Mathf.Max(1f, (diameter / paperWorldSize.y) * textureHeight * 0.5f);
            float startX = Mathf.Clamp01(startUv.x) * (textureWidth - 1);
            float startY = Mathf.Clamp01(startUv.y) * (textureHeight - 1);
            float endX = Mathf.Clamp01(endUv.x) * (textureWidth - 1);
            float endY = Mathf.Clamp01(endUv.y) * (textureHeight - 1);

            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(startX, endX) - radiusX - 1f));
            int maxX = Mathf.Min(textureWidth - 1, Mathf.CeilToInt(Mathf.Max(startX, endX) + radiusX + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(startY, endY) - radiusY - 1f));
            int maxY = Mathf.Min(textureHeight - 1, Mathf.CeilToInt(Mathf.Max(startY, endY) + radiusY + 1f));

            Vector2 start = new Vector2(startX / radiusX, startY / radiusY);
            Vector2 end = new Vector2(endX / radiusX, endY / radiusY);
            Vector2 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 point = new Vector2(x / radiusX, y / radiusY);
                    float t = segmentLengthSquared > 0.000001f
                        ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSquared)
                        : 0f;
                    float normalizedDistance = Vector2.Distance(point, Vector2.Lerp(start, end, t));
                    if (normalizedDistance > 1.05f)
                        continue;

                    float coverage = normalizedDistance <= 0.86f
                        ? 1f
                        : 1f - Mathf.InverseLerp(0.86f, 1.05f, normalizedDistance);
                    int pixelIndex = y * textureWidth + x;
                    Color32 existing = pixels[pixelIndex];
                    if (erasing)
                    {
                        pixels[pixelIndex] = new Color32(
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                            255);
                    }
                    else
                    {
                        pixels[pixelIndex] = new Color32(
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, colour.r, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, colour.g, coverage)),
                            (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, colour.b, coverage)),
                            255);
                    }
                }
            }
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
            Vector2 filtered = Vector2.Lerp(filteredInputUv, candidate, strokePointSmoothing);
            inputBeforeLastUv = lastInputUv;
            lastInputUv = uv;
            filteredInputUv = filtered;
            inputSampleCount = Mathf.Min(inputSampleCount + 1, 3);
            return filtered;
        }

        private void CloseStrokeToLatestInput()
        {
            if (!hasPreviousPoint || inputSampleCount <= 0)
                return;

            // Finish on the latest raw sample. The filtered point can trail a
            // fast final drag event by more than the pencil radius, leaving a
            // small but visible gap at a line or circle endpoint.
            Vector2 endpoint = lastInputUv;
            float distance = Vector2.Distance(previousUv, endpoint);
            if (distance > maximumUvJump)
                return;

            float brushDiameter = previousStrokeWasErasing ? eraserSize : pencilSize;
            float brushRadiusInUv = (brushDiameter * 0.5f)
                / Mathf.Max(paperWorldSize.x, paperWorldSize.y);
            float spacing = Mathf.Min(
                Mathf.Max(0.00025f, interpolationSpacing),
                Mathf.Max(0.00025f, brushRadiusInUv * 0.4f));
            int steps = Mathf.Clamp(
                Mathf.CeilToInt(distance / spacing),
                1,
                Mathf.Max(1, maximumInterpolationSteps));
            Vector2 segmentStart = previousUv;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 segmentEnd = Vector2.Lerp(previousUv, endpoint, i / (float)steps);
                StampSegment(segmentStart, segmentEnd, previousStrokeWasErasing);
                segmentStart = segmentEnd;
            }

            previousUv = endpoint;
            textureDirty = true;
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

        private void EnsureWritingSurface()
        {
            if (writingSurfaceRenderer != null || paperRenderer == null || noteTexture == null)
                return;

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Paper Writing Surface";
            surface.transform.SetParent(paperRenderer.transform, false);
            surface.transform.localPosition = new Vector3(
                0f,
                0.5f + 0.001f / Mathf.Max(0.0001f, paperRenderer.transform.lossyScale.y),
                0f);
            surface.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Vector3 inheritedScale = paperRenderer.transform.lossyScale;
            surface.transform.localScale = new Vector3(
                paperWorldSize.x / Mathf.Max(0.0001f, inheritedScale.x),
                paperWorldSize.y / Mathf.Max(0.0001f, inheritedScale.z),
                1f);

            Collider surfaceCollider = surface.GetComponent<Collider>();
            if (surfaceCollider != null)
            {
                if (Application.isPlaying)
                    Destroy(surfaceCollider);
                else
                    DestroyImmediate(surfaceCollider);
            }

            writingSurfaceRenderer = surface.GetComponent<Renderer>();
            if (writingSurfaceRenderer == null)
                return;

            Material sourceMaterial = Application.isPlaying
                ? paperRenderer.material
                : paperRenderer.sharedMaterial;
            if (sourceMaterial != null)
                writingSurfaceMaterial = new Material(sourceMaterial);
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                writingSurfaceMaterial = shader != null ? new Material(shader) : null;
            }

            if (writingSurfaceMaterial == null)
                return;

            writingSurfaceMaterial.name = "Student Paper Writing Surface";
            writingSurfaceMaterial.mainTexture = noteTexture;
            if (writingSurfaceMaterial.HasProperty("_BaseMap"))
                writingSurfaceMaterial.SetTexture("_BaseMap", noteTexture);
            if (writingSurfaceMaterial.HasProperty("_Cull"))
                writingSurfaceMaterial.SetFloat("_Cull", 0f);
            if (Application.isPlaying)
                writingSurfaceRenderer.material = writingSurfaceMaterial;
            else
                writingSurfaceRenderer.sharedMaterial = writingSurfaceMaterial;
            if (writingSurfaceMaterial.HasProperty("_BaseColor"))
                writingSurfaceMaterial.SetColor("_BaseColor", Color.white);

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
            EnsureWritingSurface();
            if (writingSurfaceMaterial != null)
            {
                writingSurfaceMaterial.mainTexture = noteTexture;
                if (writingSurfaceMaterial.HasProperty("_BaseMap"))
                    writingSurfaceMaterial.SetTexture("_BaseMap", noteTexture);
            }
        }
    }
}
