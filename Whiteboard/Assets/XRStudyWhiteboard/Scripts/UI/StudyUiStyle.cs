using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace XRStudyWhiteboard
{
    /// <summary>Shared, lightweight world-space treatment for the desk and board controls.</summary>
    public static class StudyUiStyle
    {
        public static readonly Color Panel = new Color(0.075f, 0.088f, 0.105f, 1f);
        public static readonly Color Surface = new Color(0.13f, 0.15f, 0.18f, 1f);
        public static readonly Color Accent = new Color(0.40f, 0.80f, 0.92f, 1f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color Muted = new Color(0.66f, 0.71f, 0.77f, 1f);
        private static Sprite roundedSprite;

        public static TMP_FontAsset Font => Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        public static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color colour, bool border = false)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            SetRect(item.transform, position, size);
            StyleSurface(item.GetComponent<Image>(), colour, border);
            return item;
        }

        public static void SetRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void StyleSurface(Image image, Color colour, bool border = false)
        {
            if (image == null)
                return;
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = colour;
            image.raycastTarget = false;
            if (border)
            {
                Outline outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.30f, 0.35f, 0.40f, 0.75f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);
                outline.enabled = true;
            }
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityAction action)
        {
            GameObject item = CreatePanel(parent, name, position, size, Surface);
            Button button = item.AddComponent<Button>();
            StyleButton(button);
            CreateText(item.transform, "Label", label, Vector2.zero, size - new Vector2(22f, 12f), 21f, TextAlignmentOptions.Center, Text);
            if (action != null)
                button.onClick.AddListener(action);
            return button;
        }

        public static void StyleButton(Button button, bool selected = false)
        {
            if (button == null)
                return;
            Image image = button.GetComponent<Image>();
            StyleSurface(image, Color.white);
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            Color normal = selected ? new Color(0.15f, 0.31f, 0.36f) : Surface;
            ColorBlock colours = button.colors;
            colours.normalColor = normal;
            colours.highlightedColor = Color.Lerp(normal, Color.white, 0.17f);
            colours.pressedColor = Color.Lerp(normal, Accent, 0.26f);
            colours.selectedColor = colours.normalColor;
            colours.disabledColor = new Color(0.105f, 0.12f, 0.14f, 0.52f);
            colours.colorMultiplier = 1f;
            colours.fadeDuration = 0.08f;
            button.colors = colours;
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? Accent : new Color(0.27f, 0.31f, 0.36f, 0.85f);
            outline.effectDistance = new Vector2(selected ? 2f : 1f, selected ? -2f : -1f);
            outline.enabled = true;
        }

        public static TMP_Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color colour)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            TMP_Text text = item.AddComponent<TextMeshProUGUI>();
            SetRect(item.transform, position, size);
            StyleText(text, value, fontSize, alignment, colour);
            return text;
        }

        public static void StyleText(TMP_Text text, string value, float fontSize, TextAlignmentOptions alignment, Color colour)
        {
            if (text == null)
                return;
            text.text = value;
            text.font = Font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = colour;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
        }

        // One shared nine-slice sprite gives all world canvases real rounded
        // corners without new assets, shaders, masks or external UI packages.
        private static Sprite RoundedSprite()
        {
            if (roundedSprite != null)
                return roundedSprite;
            const int size = 64;
            const float radius = 14f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Study UI Rounded Corners";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                byte alpha = (byte)(Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy)) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
            roundedSprite.name = "Study UI Rounded";
            roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return roundedSprite;
        }
    }
}
