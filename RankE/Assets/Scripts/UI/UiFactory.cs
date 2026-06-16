using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Helpers for building the whole HUD programmatically (Phase 2 decision: UI
    /// lives in code, not scene YAML — placeholder layout, Phase 3 owns polish).
    /// Uses legacy uGUI Text with the built-in font so nothing needs importing.
    /// </summary>
    public static class UiFactory
    {
        static Sprite white;
        static Font font;

        public static Sprite WhiteSprite
        {
            get
            {
                if (white == null)
                {
                    var tex = Texture2D.whiteTexture;
                    white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
                }
                return white;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>The wooden-UI theme, or null until the builder has produced it.</summary>
        public static UiSkin Skin => UiSkin.Load();

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor min=max=anchor; pos/size in reference pixels.</summary>
        public static void PlaceFixed(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        /// <summary>Stretch across the whole parent.</summary>
        public static void PlaceStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Apply a 9-sliced skin sprite to an image, or leave it flat if absent.</summary>
        static void ApplySliced(Image img, Sprite sprite)
        {
            if (sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            var skin = Skin;
            img.pixelsPerUnitMultiplier = skin != null ? Mathf.Max(0.01f, skin.PixelsPerUnitMultiplier) : 1f;
        }

        /// <summary>
        /// A framed container panel: uses the skin's 9-slice frame when built, otherwise a
        /// flat dark panel so layouts read the same before the skin exists. Pass null
        /// <paramref name="frameSprite"/> to use the default PanelFrame.
        /// </summary>
        public static Image Frame(string name, Transform parent, Sprite frameSprite = null)
        {
            var skin = Skin;
            var sprite = frameSprite != null ? frameSprite : skin != null ? skin.PanelFrame : null;
            var img = Panel(name, parent, sprite != null
                ? (skin != null ? skin.FrameTint : Color.white)
                : new Color(0.08f, 0.08f, 0.12f, 0.96f));
            ApplySliced(img, sprite);
            return img;
        }

        /// <summary>A non-interactive icon image (e.g. an ability glyph). No-op visual if null.</summary>
        public static Image Icon(string name, Transform parent, Sprite sprite)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = sprite != null;
            return img;
        }

        /// <summary>A bar fill on top of a background panel; returns the fill image.</summary>
        public static Image Bar(string name, Transform parent, Color bg, Color fill,
            Image.FillMethod method, out Image background)
        {
            var skin = Skin;

            background = Panel(name, parent, bg);
            if (skin != null && skin.BarBackground != null)
            {
                background.sprite = skin.BarBackground;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }

            var fillImg = Panel("Fill", background.transform, fill);
            PlaceStretch((RectTransform)fillImg.transform);
            if (skin != null && skin.BarFill != null)
                fillImg.sprite = skin.BarFill; // neutral strip; keeps the runtime fill tint
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = method;
            fillImg.fillAmount = 1f;
            if (method == Image.FillMethod.Vertical) fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            if (method == Image.FillMethod.Horizontal) fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            return fillImg;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button TextButton(string name, Transform parent, string label,
            int fontSize, UnityAction onClick)
        {
            var skin = Skin;
            bool themed = skin != null && skin.Button != null;

            var img = Panel(name, parent, themed ? Color.white : new Color(0.22f, 0.22f, 0.28f, 0.95f));
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            if (themed)
            {
                ApplySliced(img, skin.Button);
                btn.transition = Selectable.Transition.SpriteSwap;
                var sprites = btn.spriteState;
                sprites.highlightedSprite = skin.ButtonHover != null ? skin.ButtonHover : skin.Button;
                sprites.selectedSprite = sprites.highlightedSprite;
                sprites.pressedSprite = skin.ButtonPressed != null ? skin.ButtonPressed : skin.Button;
                btn.spriteState = sprites;
            }
            else
            {
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.45f, 0.45f, 0.6f);
                colors.selectedColor = new Color(0.45f, 0.45f, 0.6f);
                colors.pressedColor = new Color(0.6f, 0.6f, 0.8f);
                btn.colors = colors;
            }

            if (onClick != null) btn.onClick.AddListener(onClick);
            var text = Label("Label", img.transform, label, fontSize, Color.white);
            PlaceStretch((RectTransform)text.transform);
            return btn;
        }
    }
}
