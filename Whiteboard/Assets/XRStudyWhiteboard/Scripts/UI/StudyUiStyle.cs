// script overview: this shared helper gives the classroom menus a consistent appearance. it creates panels,
// buttons, and text with common colours, sizes, spacing, and feedback. it generates the rounded panel sprite
// at runtime. keeping these rules together lets the desk and board controls use the same visual language.

// search: interface style | colour | button | panel | text | rounded corners.
// reading guide: the comment immediately above a code line explains that line. search for method to find the
// larger steps. uv means a normalised two-dimensional position on a writing texture.

// make text mesh pro types available for readable interface labels.
using TMPro;
// make unity scene, component, transform, vector, colour, texture, and other engine types available in this
// file.
using UnityEngine;
// make the types in unity engine.events available to this script.
using UnityEngine.Events;
// make unity interface types such as buttons, images, and graphic controls available.
using UnityEngine.UI;

// group these types under the xr study whiteboard namespace to avoid naming conflicts.
namespace XRStudyWhiteboard
// begin the namespace block.
{
    // shared, lightweight world-space treatment for the desk and board controls.
    // declare the study ui style class.
    public static class StudyUiStyle
    // begin the class study ui style block.
    {
        // keep panel as colour, initially a new colour using 0.075, 0.088, 0.105, 1.
        public static readonly Color Panel = new Color(0.075f, 0.088f, 0.105f, 1f);
        // keep surface as colour, initially a new colour using 0.13, 0.15, 0.18, 1.
        public static readonly Color Surface = new Color(0.13f, 0.15f, 0.18f, 1f);
        // keep accent as colour, initially a new colour using 0.4, 0.8, 0.92, 1.
        public static readonly Color Accent = new Color(0.40f, 0.80f, 0.92f, 1f);
        // keep text as colour, initially a new colour using 0.95, 0.96, 0.98, 1.
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.98f, 1f);
        // keep muted as colour, initially a new colour using 0.66, 0.71, 0.77, 1.
        public static readonly Color Muted = new Color(0.66f, 0.71f, 0.77f, 1f);
        // keep rounded sprite as sprite for later use.
        private static Sprite roundedSprite;

        // expose font as resources: load using the text "fonts & materials/liberationsans sdf".
        public static TMP_FontAsset Font => Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // method: create panel.
        // create a rectangular interface object and apply the shared surface appearance.
        public static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color colour, bool border = false)
        // begin the method create panel block.
        {
            // store item as a new game object using name, the type information for rect transform, the type
            // information for image.
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            // parent item transform under parent using local coordinates.
            item.transform.SetParent(parent, false);
            // set rect using item transform, position, size.
            SetRect(item.transform, position, size);
            // style surface using the the image component attached to this object component on item, colour,
            // border.
            StyleSurface(item.GetComponent<Image>(), colour, border);
            // return item to the caller.
            return item;
        // finish the method create panel block.
        }

        // method: set rect.
        // centre the anchors and pivot, then apply position, size, rotation, and scale.
        public static void SetRect(Transform target, Vector2 position, Vector2 size)
        // begin the method set rect block.
        {
            // run the following statement only when not (target matches rect transform rect).
            if (!(target is RectTransform rect))
                // leave this method now without running the remaining statements.
                return;
            // set rect anchor min to set rect anchor max to set rect pivot to a new two-dimensional vector
            // using 0.5, 0.5.
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            // set rect anchored position to position.
            rect.anchoredPosition = position;
            // set rect size delta to size.
            rect.sizeDelta = size;
            // set rect local rotation to quaternion identity.
            rect.localRotation = Quaternion.identity;
            // set rect local scale to unit scale on all three axes.
            rect.localScale = Vector3.one;
        // finish the method set rect block.
        }

        // method: style surface.
        // apply the rounded panel sprite, colour, and optional border.
        public static void StyleSurface(Image image, Color colour, bool border = false)
        // begin the method style surface block.
        {
            // run the following statement only when image is missing.
            if (image == null)
                // leave this method now without running the remaining statements.
                return;
            // set image sprite to rounded sprite.
            image.sprite = RoundedSprite();
            // set image type to image type sliced.
            image.type = Image.Type.Sliced;
            // set image pixels per unit multiplier to 1.
            image.pixelsPerUnitMultiplier = 1f;
            // set image colour to colour.
            image.color = colour;
            // set image raycast target to false.
            image.raycastTarget = false;
            // run the following statement only when border.
            if (border)
            // begin the if statement block.
            {
                // store outline as the the outline component attached to this object component on image.
                Outline outline = image.GetComponent<Outline>();
                // run the following statement only when outline is missing.
                if (outline == null)
                    // set outline to a newly attached add component<outline> component on image game object.
                    outline = image.gameObject.AddComponent<Outline>();
                // set outline effect colour to a new colour using 0.3, 0.35, 0.4, 0.75.
                outline.effectColor = new Color(0.30f, 0.35f, 0.40f, 0.75f);
                // set outline effect distance to a new two-dimensional vector using 1.2, the negative of 1.2.
                outline.effectDistance = new Vector2(1.2f, -1.2f);
                // set whether outline is enabled to true.
                outline.enabled = true;
            // finish the if statement block.
            }
        // finish the method style surface block.
        }

        // method: create button.
        // build a styled button and label, then connect its action callback.
        public static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityAction action)
        // begin the method create button block.
        {
            // store item as create panel using parent, name, position, size, surface.
            GameObject item = CreatePanel(parent, name, position, size, Surface);
            // store button as a newly attached add component<button> component on item.
            Button button = item.AddComponent<Button>();
            // style button using button.
            StyleButton(button);
            // create text using item transform, the text "label", label, the zero two-dimensional vector, size
            // minus a new two-dimensional vector using 22, 12, 21, text alignment options center, text.
            CreateText(item.transform, "Label", label, Vector2.zero, size - new Vector2(22f, 12f), 21f, TextAlignmentOptions.Center, Text);
            // run the following statement only when action exists.
            if (action != null)
                // register action as a callback on button on click.
                button.onClick.AddListener(action);
            // return button to the caller.
            return button;
        // finish the method create button block.
        }

        // method: style button.
        // configure button colours and transitions for normal, hover, pressed, and selected states.
        public static void StyleButton(Button button, bool selected = false)
        // begin the method style button block.
        {
            // run the following statement only when button is missing.
            if (button == null)
                // leave this method now without running the remaining statements.
                return;
            // store image as the the image component attached to this object component on button.
            Image image = button.GetComponent<Image>();
            // style surface using image, colour white.
            StyleSurface(image, Color.white);
            // set image raycast target to true.
            image.raycastTarget = true;
            // set button target graphic to image.
            button.targetGraphic = image;
            // set button transition to selectable transition colour tint.
            button.transition = Selectable.Transition.ColorTint;
            // store normal as if selected, use a new colour using 0.15, 0.31, 0.36, otherwise use surface.
            Color normal = selected ? new Color(0.15f, 0.31f, 0.36f) : Surface;
            // store colours as button colours.
            ColorBlock colours = button.colors;
            // set colours normal colour to normal.
            colours.normalColor = normal;
            // set colours highlighted colour to an interpolated value from normal toward colour white using
            // 0.17.
            colours.highlightedColor = Color.Lerp(normal, Color.white, 0.17f);
            // set colours pressed colour to an interpolated value from normal toward accent using 0.26.
            colours.pressedColor = Color.Lerp(normal, Accent, 0.26f);
            // set colours selected colour to colours normal colour.
            colours.selectedColor = colours.normalColor;
            // set colours disabled colour to a new colour using 0.105, 0.12, 0.14, 0.52.
            colours.disabledColor = new Color(0.105f, 0.12f, 0.14f, 0.52f);
            // set colours colour multiplier to 1.
            colours.colorMultiplier = 1f;
            // set colours fade duration to 0.08.
            colours.fadeDuration = 0.08f;
            // set button colours to colours.
            button.colors = colours;
            // store outline as the the outline component attached to this object component on button.
            Outline outline = button.GetComponent<Outline>();
            // run the following statement only when outline is missing.
            if (outline == null)
                // set outline to a newly attached add component<outline> component on button game object.
                outline = button.gameObject.AddComponent<Outline>();
            // set outline effect colour to if selected, use accent, otherwise use a new colour using 0.27,
            // 0.31, 0.36, 0.85.
            outline.effectColor = selected ? Accent : new Color(0.27f, 0.31f, 0.36f, 0.85f);
            // set outline effect distance to a new two-dimensional vector using if selected, use 2, otherwise
            // use 1, if selected, use the negative of 2, otherwise use the negative of 1.
            outline.effectDistance = new Vector2(selected ? 2f : 1f, selected ? -2f : -1f);
            // set whether outline is enabled to true.
            outline.enabled = true;
        // finish the method style button block.
        }

        // method: create text.
        // create a text element at the requested location and apply the shared typography.
        public static TMP_Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color colour)
        // begin the method create text block.
        {
            // store item as a new game object using name, the type information for rect transform.
            GameObject item = new GameObject(name, typeof(RectTransform));
            // parent item transform under parent using local coordinates.
            item.transform.SetParent(parent, false);
            // store text as a newly attached add component<text mesh pro ugui> component on item.
            TMP_Text text = item.AddComponent<TextMeshProUGUI>();
            // set rect using item transform, position, size.
            SetRect(item.transform, position, size);
            // style text using text, value, font size, alignment, colour.
            StyleText(text, value, fontSize, alignment, colour);
            // return text to the caller.
            return text;
        // finish the method create text block.
        }

        // method: style text.
        // apply font, text, size, alignment, colour, and wrapping settings.
        public static void StyleText(TMP_Text text, string value, float fontSize, TextAlignmentOptions alignment, Color colour)
        // begin the method style text block.
        {
            // run the following statement only when text is missing.
            if (text == null)
                // leave this method now without running the remaining statements.
                return;
            // set text text to value.
            text.text = value;
            // set text font to font.
            text.font = Font;
            // set text font size to font size.
            text.fontSize = fontSize;
            // set text font style to font styles normal.
            text.fontStyle = FontStyles.Normal;
            // set text colour to colour.
            text.color = colour;
            // set text alignment to alignment.
            text.alignment = alignment;
            // set text enable auto sizing to false.
            text.enableAutoSizing = false;
            // set text text wrapping mode to text wrapping modes normal.
            text.textWrappingMode = TextWrappingModes.Normal;
            // set text overflow mode to text overflow modes overflow.
            text.overflowMode = TextOverflowModes.Overflow;
            // set text raycast target to false.
            text.raycastTarget = false;
        // finish the method style text block.
        }

        // one shared nine-slice sprite gives all world canvases real rounded
        // corners without new assets, shaders, masks or external ui packages.
        // method: rounded sprite.
        // create and cache a small rounded rectangle texture used by sliced panel images.
        private static Sprite RoundedSprite()
        // begin the method rounded sprite block.
        {
            // run the following statement only when rounded sprite exists.
            if (roundedSprite != null)
                // return rounded sprite to the caller.
                return roundedSprite;
            // store size as 64.
            const int size = 64;
            // store radius as 14.
            const float radius = 14f;
            // store texture as a new texture2 d using size, size, texture format rgba32, false.
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // set texture name to the text "study ui rounded corners".
            texture.name = "Study UI Rounded Corners";
            // set texture wrap mode to texture wrap mode clamp.
            texture.wrapMode = TextureWrapMode.Clamp;
            // set texture filter mode to filter mode bilinear.
            texture.filterMode = FilterMode.Bilinear;
            // set texture hide flags to hide flags hide and dont save.
            texture.hideFlags = HideFlags.HideAndDontSave;
            // store pixels as a new 32-bit colour[size * size] array.
            Color32[] pixels = new Color32[size * size];
            // repeat this loop while y is less than size. increase y by one after each pass.
            for (int y = 0; y < size; y++)
            // repeat this loop while x is less than size. increase x by one after each pass.
            for (int x = 0; x < size; x++)
            // begin the for statement block.
            {
                // store dx as the larger of the absolute value of x plus 0.5 minus size multiplied by 0.5
                // minus size multiplied by 0.5 minus radius and 0.
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                // store dy as the larger of the absolute value of y plus 0.5 minus size multiplied by 0.5
                // minus size multiplied by 0.5 minus radius and 0.
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                // store alpha as radius plus 0.5 minus mathf: sqrt using dx multiplied by dx plus dy
                // multiplied by dy limited to the range from zero to one multiplied by 255 converted to byte.
                byte alpha = (byte)(Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                // set the entry at y multiplied by size plus x in pixels to a new 32-bit colour using 255,
                // 255, 255, alpha.
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            // finish the for statement block.
            }
            // copy pixels into texture before uploading the texture.
            texture.SetPixels32(pixels);
            // upload changes to texture.
            texture.Apply(false, true);
            // set rounded sprite to sprite: create using texture, a new rect using 0, 0, size, size, a new
            // two-dimensional vector using 0.5, 0.5, 100, 0, sprite mesh type full rect, a new vector4 using
            // 16, 16, 16, 16.
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
            // set rounded sprite name to the text "study ui rounded".
            roundedSprite.name = "Study UI Rounded";
            // set rounded sprite hide flags to hide flags hide and dont save.
            roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            // return rounded sprite to the caller.
            return roundedSprite;
        // finish the method rounded sprite block.
        }
    // finish the class study ui style block.
    }
// finish the namespace block.
}
