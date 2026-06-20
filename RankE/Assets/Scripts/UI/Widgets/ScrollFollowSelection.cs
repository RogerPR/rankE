using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Keeps the currently-selected control visible inside a <see cref="ScrollRect"/> when the
    /// user navigates with keyboard/controller (uGUI doesn't auto-scroll to the focused widget).
    /// Only acts when the selection changes and only if it's outside the viewport, so manual
    /// wheel scrolling isn't fought. Vertical only — the tuning lists scroll vertically.
    /// </summary>
    public sealed class ScrollFollowSelection : MonoBehaviour
    {
        ScrollRect scrollRect;
        RectTransform viewport;
        RectTransform content;
        GameObject last;

        public void Init(ScrollRect sr)
        {
            scrollRect = sr;
            viewport = sr != null ? sr.viewport : null;
            content = sr != null ? sr.content : null;
        }

        void Update()
        {
            if (scrollRect == null || content == null || viewport == null) return;
            var es = EventSystem.current;
            if (es == null) return;

            var sel = es.currentSelectedGameObject;
            if (sel == last) return; // only react to selection changes
            last = sel;
            if (sel == null) return;

            var rt = sel.transform as RectTransform;
            if (rt == null || !rt.IsChildOf(content)) return; // ignore footer/preset-bar buttons
            EnsureVisible(rt);
        }

        void EnsureVisible(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();

            var v = new Vector3[4];
            viewport.GetWorldCorners(v);
            float viewTop = v[1].y, viewBottom = v[0].y;

            var t = new Vector3[4];
            target.GetWorldCorners(t);
            float targetTop = t[1].y, targetBottom = t[0].y;

            float pad = (viewTop - viewBottom) * 0.12f; // keep a little margin above/below
            float deltaY = 0f;
            if (targetTop > viewTop - pad) deltaY = targetTop - (viewTop - pad);            // above view
            else if (targetBottom < viewBottom + pad) deltaY = targetBottom - (viewBottom + pad); // below view
            if (Mathf.Abs(deltaY) < 0.5f) return;

            float scale = content.lossyScale.y;
            if (Mathf.Approximately(scale, 0f)) scale = 1f;

            var ap = content.anchoredPosition;
            ap.y -= deltaY / scale; // content is top-anchored: raising y moves it up in world
            float max = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            ap.y = Mathf.Clamp(ap.y, 0f, max);
            content.anchoredPosition = ap;
        }
    }
}
