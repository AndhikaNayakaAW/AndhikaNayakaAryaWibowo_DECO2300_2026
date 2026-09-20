using UnityEngine;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    /// <summary>Vector session symbols avoid depending on special glyphs in XR fonts.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StudySessionIcon : MaskableGraphic
    {
        public enum Kind { Record, Stop, Play }
        private Kind kind;

        public void SetKind(Kind value)
        {
            kind = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            if (kind == Kind.Record)
            {
                vh.AddVert(rect.center, color, Vector2.zero);
                const int segments = 24;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    vh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width * 0.5f, Mathf.Sin(angle) * rect.height * 0.5f), color, Vector2.zero);
                    if (i > 0)
                        vh.AddTriangle(0, i, i + 1);
                }
            }
            else if (kind == Kind.Play)
            {
                vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, rect.center.y), color, Vector2.zero);
                vh.AddTriangle(0, 1, 2);
            }
            else
            {
                vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
                vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
            }
        }
    }
}
