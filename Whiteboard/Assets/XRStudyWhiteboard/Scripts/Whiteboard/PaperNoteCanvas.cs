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
        [SerializeField] private float pencilSize = 0.018f;
        [SerializeField] private float eraserSize = 0.055f;
        [SerializeField] private float interpolationSpacing = 0.001f;

        private Texture2D noteTexture;
        private Color32[] pixels;
        private Vector2 previousUv;
        private bool hasPreviousPoint;
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
                paperRenderer.material.mainTexture = noteTexture;
        }

        public bool TryGetUV(Ray ray, float maxDistance, out Vector2 uv)
        {
            uv = default;
            if (paperCollider == null)
                return false;

            if (!paperCollider.Raycast(ray, out RaycastHit hit, maxDistance))
                return false;

            // The generated papers use BoxColliders.  RaycastHit.textureCoord
            // is only reliable for mesh colliders, so calculate the point on
            // the paper's local X/Z plane for the box case.  This keeps
            // writing correct even after a paper has been grabbed and moved.
            if (paperCollider is BoxCollider box)
            {
                Vector3 localPoint = box.transform.InverseTransformPoint(hit.point);
                Vector3 halfSize = box.size * 0.5f;
                uv = new Vector2(
                    Mathf.InverseLerp(box.center.x - halfSize.x, box.center.x + halfSize.x, localPoint.x),
                    Mathf.InverseLerp(box.center.z - halfSize.z, box.center.z + halfSize.z, localPoint.z));
            }
            else
            {
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
                Stamp(uv, erasing);
                previousUv = uv;
                hasPreviousPoint = true;
                textureDirty = true;
                return;
            }

            float distance = Vector2.Distance(previousUv, uv);
            float brushDiameter = erasing ? eraserSize : pencilSize;
            float brushRadiusInUv = (brushDiameter * 0.5f)
                / Mathf.Max(paperWorldSize.x, paperWorldSize.y);
            float spacing = Mathf.Min(
                Mathf.Max(0.0005f, interpolationSpacing),
                Mathf.Max(0.0005f, brushRadiusInUv * 0.4f));
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));
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
        }

        public void EndStroke()
        {
            hasPreviousPoint = false;
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
                Destroy(collider);
            Renderer renderer = bar.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (material != null)
                    renderer.material = material;
                crosshairRenderers.Add(renderer);
            }
        }

        private void Fill(Color colour)
        {
            Color32 fill = colour;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = fill;
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
