using System.Collections.Generic;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Bottom-center ability bar: 4 main slots + 2 quick slots (Parry/Kick), each
    /// with a radial cooldown sweep, GCD sweep, key hint, gem cost and a dimmed
    /// state while the fighter can't act (stun, animation lock, missing gems).
    /// Rebind() rebuilds the slots from the player's current loadout.
    /// </summary>
    public sealed class AbilityBarView : MonoBehaviour
    {
        sealed class Slot
        {
            public AbilityState State;
            public Image Background;
            public Image CooldownSweep;
            public Text CooldownText;
        }

        const float SlotSize = 86f;

        BattleDriver driver;
        RectTransform barRoot;
        readonly List<Slot> slots = new List<Slot>();

        // Flat-look slot tints (no skin); themed slots use the lighter pair so the
        // wooden frame sprite shows through.
        static readonly Color ReadyBg = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        static readonly Color BlockedBg = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        static readonly Color ReadyBgThemed = Color.white;
        static readonly Color BlockedBgThemed = new Color(1f, 0.55f, 0.55f, 1f);

        bool themed;
        Color readyColor;
        Color blockedColor;

        public void Init(BattleDriver driver, Transform parent)
        {
            this.driver = driver;
            barRoot = UiFactory.Rect("AbilityBar", parent);
            UiFactory.PlaceFixed(barRoot, new Vector2(0.5f, 0f), new Vector2(0f, 24f),
                new Vector2(7f * (SlotSize + 10f), SlotSize + 30f));
        }

        /// <summary>Rebuild the slots from the current battle's player loadout.</summary>
        public void Rebind()
        {
            foreach (Transform child in barRoot)
                Destroy(child.gameObject);
            slots.Clear();

            var battle = driver != null ? driver.Battle : null;
            if (battle == null) return;

            var skin = UiFactory.Skin;
            var slotFrame = skin != null ? skin.SlotFrame : null;
            themed = slotFrame != null;
            readyColor = themed ? ReadyBgThemed : ReadyBg;
            blockedColor = themed ? BlockedBgThemed : BlockedBg;

            var abilities = battle.Fighters[0].Abilities;
            for (int i = 0; i < abilities.Count && i < 6; i++)
            {
                // Visual gap between the 4 main slots and the 2 quick slots.
                float x = (i - 2.5f) * (SlotSize + 10f) + (i >= 4 ? 24f : 0f);
                var bg = themed
                    ? UiFactory.Frame($"Slot{i}", barRoot, slotFrame)
                    : UiFactory.Panel($"Slot{i}", barRoot, ReadyBg);
                bg.color = readyColor;
                UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(0.5f, 0f),
                    new Vector2(x, 30f), new Vector2(SlotSize, SlotSize));

                var def = abilities[i].Def;

                // Icon if mapped; otherwise fall back to the ability name label.
                var icon = skin != null ? skin.IconFor(def.Id) : null;
                if (icon != null)
                {
                    var iconImg = UiFactory.Icon("Icon", bg.transform, icon);
                    UiFactory.PlaceFixed((RectTransform)iconImg.transform, new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(SlotSize - 18f, SlotSize - 18f));
                }
                else
                {
                    var name = UiFactory.Label("Name", bg.transform, def.Name, 16, Color.white);
                    UiFactory.PlaceFixed((RectTransform)name.transform, new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(SlotSize, 20f));
                }

                if (def.GemCost > 0)
                {
                    var gems = UiFactory.Label("Gems", bg.transform, $"{def.GemCost}◆", 14,
                        new Color(0.55f, 0.75f, 1f), TextAnchor.LowerRight);
                    UiFactory.PlaceFixed((RectTransform)gems.transform, new Vector2(1f, 0f),
                        new Vector2(-4f, 4f), new Vector2(30f, 16f));
                }

                var sweep = UiFactory.Panel("Cooldown", bg.transform, new Color(0f, 0f, 0f, 0.72f));
                UiFactory.PlaceStretch((RectTransform)sweep.transform);
                sweep.type = Image.Type.Filled;
                sweep.fillMethod = Image.FillMethod.Radial360;
                sweep.fillOrigin = (int)Image.Origin360.Top;
                sweep.fillClockwise = false;
                sweep.fillAmount = 0f;

                var cdText = UiFactory.Label("CdText", bg.transform, "", 22,
                    new Color(1f, 1f, 1f, 0.9f));
                UiFactory.PlaceStretch((RectTransform)cdText.transform);

                var hint = UiFactory.Label("Key", bg.transform,
                    i < PlayerInputController.SlotKeyHints.Length
                        ? PlayerInputController.SlotKeyHints[i] : "", 16,
                    new Color(1f, 1f, 0.6f), TextAnchor.UpperLeft);
                UiFactory.PlaceFixed((RectTransform)hint.transform, new Vector2(0f, 1f),
                    new Vector2(5f, -3f), new Vector2(50f, 18f));

                slots.Add(new Slot
                {
                    State = abilities[i],
                    Background = bg,
                    CooldownSweep = sweep,
                    CooldownText = cdText,
                });
            }
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            if (battle == null || slots.Count == 0) return;
            var f = battle.Fighters[0];

            foreach (var slot in slots)
            {
                var def = slot.State.Def;

                // Cooldown fraction (cooldowns are set scaled by CooldownUseMult at use time).
                int maxCd = Mathf.Max(1, (int)(slot.State.EffCooldownTicks * f.CooldownUseMult));
                float cdFrac = Mathf.Clamp01((float)slot.State.CooldownRemaining / maxCd);

                // GCD sweep applies to normal-GCD abilities only (quick actions bypass it).
                float gcdFrac = def.Gcd == GcdClass.Normal && battle.Tuning.GcdTicks > 0
                    ? Mathf.Clamp01((float)f.GcdRemaining / battle.Tuning.GcdTicks)
                    : 0f;

                slot.CooldownSweep.fillAmount = Mathf.Max(cdFrac, gcdFrac);
                slot.CooldownText.text = slot.State.CooldownRemaining > 0
                    ? Mathf.Ceil(slot.State.CooldownRemaining * SimConstants.TickDuration).ToString()
                    : "";

                bool blocked = !f.CanAct
                    || f.LockRemaining > 0
                    || f.IsWindingUp
                    || f.SpellGems < def.GemCost
                    || (f.IsCasting && !def.UsableWhileCasting);
                slot.Background.color = blocked ? blockedColor : readyColor;
            }
        }
    }
}
