using System.Text;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// One fighter's timing bar (player top-left, enemy top-right). A single bar reads out the
    /// whole anatomy of an action through distinct, colour-coded phases so the four timing
    /// concepts are unmistakable:
    ///   • TELEGRAPH (enemy only) — orange, the fill <i>recedes</i> (edge moves left) as the
    ///     wind-up commits, the visual opposite of a cast, so "attack incoming" never reads as
    ///     a spell channel.
    ///   • CAST — yellow, fills left→right; shows a kick hint while interruptible.
    ///   • LOCK (pre/post animation lock) — grey, the committed/recovery frames.
    ///   • DELAY — a pip line under the bar for this fighter's in-flight delayed effects.
    /// A red flash marks an interrupted cast. Pure view: polls sim/telegraph state, never decides.
    /// </summary>
    public sealed class CastBarView : MonoBehaviour
    {
        BattleDriver driver;
        int index;
        GameObject root;
        Image fill;
        Image icon;
        Text label;
        Text delayLabel;
        float interruptedFlash;
        int lockTotal;
        readonly StringBuilder sb = new StringBuilder();

        static readonly Color CastColor = new Color(0.95f, 0.85f, 0.3f);
        static readonly Color TelegraphColor = new Color(1f, 0.5f, 0.15f);
        static readonly Color LockColor = new Color(0.55f, 0.55f, 0.62f);

        public void Init(BattleDriver driver, Transform parent, int fighterIndex, HudPlacement placement)
        {
            this.driver = driver;
            index = fighterIndex;
            bool left = fighterIndex == 0;

            var group = UiFactory.Rect($"CastGroup{fighterIndex}", parent);
            placement.Apply(group);
            root = group.gameObject;

            var iconFrame = UiFactory.Frame("CastIcon", group);
            UiFactory.PlaceFixed((RectTransform)iconFrame.transform, new Vector2(left ? 0f : 1f, 0.5f),
                Vector2.zero, new Vector2(56f, 56f));
            icon = UiFactory.Icon("Icon", iconFrame.transform, null);
            UiFactory.PlaceFixed((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(44f, 44f));

            fill = UiFactory.Bar($"CastBar{fighterIndex}", group,
                new Color(0f, 0f, 0f, 0.6f), CastColor, Image.FillMethod.Horizontal, out var bg);
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(left ? 66f : -66f, 0f), new Vector2(354f, 28f));

            label = UiFactory.Label("CastLabel", bg.transform, "", 20, Color.white);
            UiFactory.PlaceStretch((RectTransform)label.transform);

            // Delay pip line, just under the bar.
            delayLabel = UiFactory.Label("DelayLabel", group, "", 15,
                new Color(0.8f, 0.6f, 1f), left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            UiFactory.PlaceFixed((RectTransform)delayLabel.transform, new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(left ? 66f : -66f, -26f), new Vector2(354f, 18f));

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.CastInterrupted && ev.Target == index)
                interruptedFlash = 0.5f;
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var f = battle != null ? battle.Fighters[index] : null;
            interruptedFlash = Mathf.Max(0f, interruptedFlash - Time.deltaTime);

            if (battle == null)
            {
                root.SetActive(false);
                return;
            }

            // The delay pip is independent of the main phase (the fighter is free to act while a
            // delayed effect is in flight), so update it every frame.
            UpdateDelay(battle);

            if (interruptedFlash > 0f)
            {
                Show(1f, Color.red, "INTERRUPTED", null);
                return;
            }

            // Phase priority: telegraph (enemy wind-up) → pre-lock → cast → post-lock.
            var tele = driver.EnemyBehavior;
            string pending = index == 1 && tele != null ? tele.PendingIntent : null;

            if (pending != null && !f.IsCasting && !f.IsWindingUp)
            {
                // TELEGRAPH: fill = ticks remaining, so the edge recedes left as it commits —
                // the deliberate opposite of a cast's growing fill.
                int total = Mathf.Max(1, driver.EnemyTelegraphTicksTotal);
                float remaining = Mathf.Clamp01((float)tele.TicksUntilCommit / total);
                Show(remaining, TelegraphColor, "! " + AbilityName(battle, pending), pending);
                return;
            }

            if (f.IsWindingUp)
            {
                int total = Mathf.Max(1, f.Windup.Def.PreLockTicks);
                float progress = 1f - Mathf.Clamp01((float)f.WindupRemaining / total);
                lockTotal = 0;
                Show(progress, LockColor, f.Windup.Def.Name, f.Windup.Def.Id);
                return;
            }

            if (f.IsCasting)
            {
                int total = f.Casting.EffCastTicks;
                float progress = total > 0 ? 1f - Mathf.Clamp01((float)f.CastRemaining / total) : 1f;
                string name = f.Casting.Def.Name;
                if (f.Casting.Def.Interruptible) name += "  (kick!)";
                lockTotal = 0;
                Show(progress, CastColor, name, f.Casting.Def.Id);
                return;
            }

            if (f.LockRemaining > 0)
            {
                // POST-LOCK: capture the peak on entry, then drain.
                if (f.LockRemaining > lockTotal) lockTotal = f.LockRemaining;
                float remaining = lockTotal > 0 ? (float)f.LockRemaining / lockTotal : 1f;
                Show(remaining, LockColor, "recover", null);
                return;
            }

            lockTotal = 0;
            root.SetActive(delayLabel.text.Length > 0); // keep the group alive only for a delay pip
            if (root.activeSelf) { fill.fillAmount = 0f; label.text = ""; icon.enabled = false; }
        }

        void Show(float fillAmount, Color color, string text, string iconId)
        {
            root.SetActive(true);
            fill.fillAmount = fillAmount;
            fill.color = color;
            label.text = text;
            var skin = UiFactory.Skin;
            var sprite = iconId != null && skin != null ? skin.IconFor(iconId) : null;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        static string AbilityName(Battle battle, string id) =>
            battle.Content.Abilities.TryGetValue(id, out var def) ? def.Name : id;

        void UpdateDelay(Battle battle)
        {
            sb.Length = 0;
            foreach (var p in battle.Pending)
            {
                if (p.Source != index) continue;
                float secs = (p.FireTick - battle.CurrentTick) * SimConstants.TickDuration;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append($"◇ {p.Ability.Name} {Mathf.Max(0f, secs):0.0}s");
            }
            delayLabel.text = sb.ToString();
        }
    }
}
