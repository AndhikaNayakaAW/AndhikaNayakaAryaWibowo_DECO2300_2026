// script overview: this script draws the record, play, and stop symbols directly as interface geometry. the
// icon uses vertices and triangles instead of depending on a special character in a font. the control panel
// chooses the symbol that matches the recording state.

// search: recording icon | record circle | play triangle | stop square.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // vector session symbols avoid depending on special glyphs in xr fonts.
    // apply unity or c sharp metadata: require component(typeof(canvas renderer)).
    [RequireComponent(typeof(CanvasRenderer))]
    // declare the study session icon class based on maskable graphic.
    public sealed class StudySessionIcon : MaskableGraphic
    // begin the class study session icon block.
    {
        // define the named kind choices used instead of unexplained numbers.
        public enum Kind { Record, Stop, Play }
        // keep kind as kind for later use.
        private Kind kind;

        // method: set kind.
        // change the symbol choice and request a geometry rebuild.
        public void SetKind(Kind value)
        // begin the method set kind block.
        {
            // set kind to value.
            kind = value;
            // set vertices dirty.
            SetVerticesDirty();
        // finish the method set kind block.
        }

        // method: on populate mesh.
        // generate a circle for record, a triangle for play, or a square for stop using interface vertices and
        // triangles.
        protected override void OnPopulateMesh(VertexHelper vh)
        // begin the method on populate mesh block.
        {
            // empty vh.
            vh.Clear();
            // store rect as rect transform rect.
            Rect rect = rectTransform.rect;
            // run the following statement only when kind equals kind record.
            if (kind == Kind.Record)
            // begin the if statement block.
            {
                // vh: add vert using rect center, colour, the zero two-dimensional vector.
                vh.AddVert(rect.center, color, Vector2.zero);
                // store segments as 24.
                const int segments = 24;
                // repeat this loop while i is at most segments. increase i by one after each pass.
                for (int i = 0; i <= segments; i++)
                // begin the for statement block.
                {
                    // store angle as i multiplied by mathf pi multiplied by 2 divided by segments.
                    float angle = i * Mathf.PI * 2f / segments;
                    // vh: add vert using rect center plus a new two-dimensional vector using mathf: cos using
                    // angle multiplied by rect width multiplied by 0.5, mathf: sin using angle multiplied by
                    // rect height multiplied by 0.5, colour, the zero two-dimensional vector.
                    vh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width * 0.5f, Mathf.Sin(angle) * rect.height * 0.5f), color, Vector2.zero);
                    // run the following statement only when i is greater than 0.
                    if (i > 0)
                        // vh: add triangle using 0, i, i plus 1.
                        vh.AddTriangle(0, i, i + 1);
                // finish the for statement block.
                }
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else if (kind == Kind.Play)
            // begin the if statement block.
            {
                // vh: add vert using a new three-dimensional vector using rect x min, rect y min, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
                // vh: add vert using a new three-dimensional vector using rect x min, rect y max, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
                // vh: add vert using a new three-dimensional vector using rect x max, rect center y, colour,
                // the zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMax, rect.center.y), color, Vector2.zero);
                // vh: add triangle using 0, 1, 2.
                vh.AddTriangle(0, 1, 2);
            // finish the if statement block.
            }
            // handle the alternative when the preceding condition is false.
            else
            // begin the else clause block.
            {
                // vh: add vert using a new three-dimensional vector using rect x min, rect y min, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
                // vh: add vert using a new three-dimensional vector using rect x min, rect y max, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
                // vh: add vert using a new three-dimensional vector using rect x max, rect y max, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
                // vh: add vert using a new three-dimensional vector using rect x max, rect y min, colour, the
                // zero two-dimensional vector.
                vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
                // vh: add triangle using 0, 1, 2.
                vh.AddTriangle(0, 1, 2);
                // vh: add triangle using 2, 3, 0.
                vh.AddTriangle(2, 3, 0);
            // finish the else clause block.
            }
        // finish the method on populate mesh block.
        }
    // finish the class study session icon block.
    }
// finish the namespace block.
}
