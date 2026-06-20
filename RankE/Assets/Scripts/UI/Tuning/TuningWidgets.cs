using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Reusable row builders for the tuning UIs (the pre-fight control panel and the abilities
    /// editor). Each field can carry a small grey description line beneath it (from
    /// <see cref="TuningDocs"/>). Every value row registers a refresher closure that re-reads
    /// its source, so a screen can call <see cref="RefreshAll"/> after a Reset/Load to update
    /// the whole list at once. Pure view: reads/writes whatever getters/setters it's handed.
    /// </summary>
    public sealed class TuningWidgets
    {
        readonly List<Action> refreshers = new List<Action>();

        public static readonly Color HeaderColor = new Color(1f, 0.86f, 0.5f);
        public static readonly Color LabelColor = new Color(0.86f, 0.86f, 0.9f);
        public static readonly Color SubHeaderColor = new Color(0.7f, 0.85f, 1f);
        public static readonly Color DocColor = new Color(0.6f, 0.6f, 0.72f);

        public void RefreshAll()
        {
            for (int i = 0; i < refreshers.Count; i++) refreshers[i]();
        }

        // --- headers / notes ---

        public void AddHeader(Transform parent, string text)
        {
            var row = AddControlRow(parent, 56f);
            Flexible(UiFactory.Label("H", row, text.ToUpperInvariant(), 30, HeaderColor,
                TextAnchor.MiddleLeft).rectTransform);
        }

        public void AddSubHeader(Transform parent, string text)
        {
            var row = AddControlRow(parent, 40f);
            Flexible(UiFactory.Label("S", row, text, 26, SubHeaderColor, TextAnchor.MiddleLeft).rectTransform);
        }

        public void AddNote(Transform parent, string text)
        {
            var row = AddControlRow(parent, 30f);
            Flexible(UiFactory.Label("N", row, text, 20, DocColor, TextAnchor.MiddleLeft).rectTransform);
        }

        // --- value rows ---

        public void AddIntField(Transform parent, string label, string doc, Func<int> get, Action<int> set) =>
            AddStepper(parent, label, doc, () => get(), v => set((int)Math.Round(v)), 1, true);

        public void AddFloatField(Transform parent, string label, string doc, Func<float> get,
            Action<float> set, float step) =>
            AddStepper(parent, label, doc, () => get(), v => set((float)v), step, false);

        public void AddStepper(Transform parent, string label, string doc, Func<double> get,
            Action<double> set, double step, bool isInt)
        {
            var row = BeginField(parent, doc);
            var lbl = UiFactory.Label("L", row, label, 24, LabelColor, TextAnchor.MiddleLeft);
            Flexible(lbl.rectTransform);

            var dec = StepButton(row, "−"); // minus sign
            var valT = UiFactory.Label("V", row, "", 26, Color.white);
            FixedWidth(valT.rectTransform, 150f);
            var inc = StepButton(row, "+");

            Action refresh = () => valT.text = isInt
                ? ((int)Math.Round(get())).ToString()
                : get().ToString("0.###");
            Action down = () => { set(get() - step); refresh(); };
            Action up = () => { set(get() + step); refresh(); };

            dec.onClick.AddListener(() => down());
            inc.onClick.AddListener(() => up());
            dec.gameObject.AddComponent<HoldRepeat>().OnRepeat = down;
            inc.gameObject.AddComponent<HoldRepeat>().OnRepeat = up;

            refresh();
            refreshers.Add(refresh);
        }

        /// <summary>A discrete left/right selector: [label] [&lt;] name [&gt;].</summary>
        public void AddCycler(Transform parent, string label, string doc, Func<string> getName, Action<int> cycle)
        {
            var row = BeginField(parent, doc);
            var lbl = UiFactory.Label("L", row, label, 24, LabelColor, TextAnchor.MiddleLeft);
            Flexible(lbl.rectTransform);

            var dec = StepButton(row, "<");
            var valT = UiFactory.Label("V", row, "", 22, Color.white);
            FixedWidth(valT.rectTransform, 280f);
            var inc = StepButton(row, ">");

            Action refresh = () => valT.text = getName();
            dec.onClick.AddListener(() => { cycle(-1); refresh(); });
            inc.onClick.AddListener(() => { cycle(+1); refresh(); });

            refresh();
            refreshers.Add(refresh);
        }

        /// <summary>Emit a stepper per public int/double field of a plain-data object. The target
        /// is fetched lazily so a Reset/Load that swaps in fresh objects is picked up by every row.
        /// Descriptions come from <paramref name="docFor"/> (keyed by field name).</summary>
        public void ReflectNumericFields(Transform parent, Type type, Func<object> target, Func<string, string> docFor)
        {
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string doc = docFor != null ? docFor(f.Name) : "";
                if (f.FieldType == typeof(int))
                    AddStepper(parent, Nicify(f.Name), doc,
                        () => (int)f.GetValue(target()),
                        v => f.SetValue(target(), (int)Math.Round(v)), 1, true);
                else if (f.FieldType == typeof(double))
                    AddStepper(parent, Nicify(f.Name), doc,
                        () => (double)f.GetValue(target()),
                        v => f.SetValue(target(), v), 0.05, false);
                // string ids aren't feel knobs — skipped, like the editor window.
            }
        }

        // --- layout helpers ---

        // A field column: the label+control row on top, plus an optional grey doc line below.
        // Returns the horizontal control row for the caller to fill.
        RectTransform BeginField(Transform parent, string doc)
        {
            bool hasDoc = !string.IsNullOrEmpty(doc);
            if (!hasDoc) return AddControlRow(parent, 46f);

            var col = UiFactory.Rect("Field", parent);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.spacing = 0f;
            var le = col.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 70f;
            le.preferredHeight = 70f;

            var row = AddControlRow(col, 46f);
            var d = UiFactory.Label("Doc", col, doc, 18, DocColor, TextAnchor.MiddleLeft);
            var dle = d.gameObject.AddComponent<LayoutElement>();
            dle.minHeight = 22f;
            dle.preferredHeight = 22f;
            return row;
        }

        static RectTransform AddControlRow(Transform parent, float height)
        {
            var row = UiFactory.Rect("Row", parent);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return row;
        }

        Button StepButton(Transform parent, string glyph)
        {
            var b = UiFactory.TextButton("Step", parent, glyph, 30, null);
            FixedWidth((RectTransform)b.transform, 64f);
            return b;
        }

        static void Flexible(RectTransform rt)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
        }

        static void FixedWidth(RectTransform rt, float w)
        {
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.minWidth = w;
            le.flexibleWidth = 0f;
        }

        // Tiny runtime copy of UnityEditor.ObjectNames.NicifyVariableName: "GcdTicks" -> "Gcd Ticks".
        public static string Nicify(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new System.Text.StringBuilder(name.Length + 4);
            sb.Append(char.ToUpperInvariant(name[0]));
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && !char.IsUpper(name[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
