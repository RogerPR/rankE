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

        /// <summary>A bar fill on top of a background panel; returns the fill image.</summary>
        public static Image Bar(string name, Transform parent, Color bg, Color fill,
            Image.FillMethod method, out Image background)
        {
            background = Panel(name, parent, bg);
            var fillImg = Panel("Fill", background.transform, fill);
            PlaceStretch((RectTransform)fillImg.transform);
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
            var img = Panel(name, parent, new Color(0.22f, 0.22f, 0.28f, 0.95f));
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.45f, 0.45f, 0.6f);
            colors.selectedColor = new Color(0.45f, 0.45f, 0.6f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.8f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var text = Label("Label", img.transform, label, fontSize, Color.white);
            PlaceStretch((RectTransform)text.transform);
            return btn;
        }
    }
}
