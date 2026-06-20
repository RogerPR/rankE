using System.Collections.Generic;

namespace RankE.UI
{
    /// <summary>
    /// Human-readable, one-line help text for the tunable numbers shown in the control panel
    /// and abilities editor. Keyed by the data field name (for the reflection-built rows over
    /// <c>CombatTuning</c> / <c>StatSheet</c>) or by a row label (for hand-built ability rows).
    /// Lives in the view layer on purpose: these are presentation strings, so the pure Sim
    /// data classes stay free of UI documentation. Missing keys return "" (no help line).
    /// </summary>
    public static class TuningDocs
    {
        static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            // --- CombatTuning (global rules, ticks are 20/s) ---
            { "GcdTicks", "Global cooldown after a normal ability before you can act again." },
            { "QuickGcdTicks", "Shorter global cooldown for quick abilities (Parry/Kick)." },
            { "ParryRiposteGain", "Riposte counter gained each time you parry an attack." },
            { "RiposteCounterMax", "Counter needed to auto-trigger a free Riposte." },
            { "ParryBreakToAttacker", "Break damage dealt to whoever you parry." },
            { "BreakMax", "Break bar capacity; filling it staggers (Broken) the target." },
            { "BreakDecayGraceTicks", "Idle time after taking break damage before the bar decays." },
            { "BreakDecayIntervalTicks", "How often the break bar ticks down once decaying." },
            { "BreakDecayAmount", "Break removed per decay tick." },
            { "BrokenDurationTicks", "How long the Broken stagger lasts (takes +damage, can't act)." },
            { "ComboWindowTicks", "Time allowed between combo steps before the chain resets." },
            { "FinisherEffectMult", "Damage multiplier applied to a combo finisher." },
            { "FinisherBonusBreak", "Extra break damage a finisher deals." },
            { "FinisherGemRefund", "Spell gems refunded on landing a finisher." },
            { "MaxTicks", "Safety cap on fight length (headless sweeps); reached = draw." },

            // --- StatSheet (per-fighter) ---
            { "Attack", "Scales physical & auto-attack damage: x(1 + Attack/100)." },
            { "Magic", "Scales magic ability damage: x(1 + Magic/100)." },
            { "Defense", "Reduces incoming damage: x100/(100+Defense). Can be negative." },
            { "CritChance", "Percent chance to crit (0-100)." },
            { "CritDamage", "Damage multiplier applied on a crit (1.5 = +50%)." },
            { "Haste", "Percent reduction to cooldowns and cast times." },
            { "BreakPower", "Multiplies the break damage this fighter deals." },
            { "GemRegenIntervalTicks", "Restore 1 spell gem every N ticks (0 = off)." },

            // --- Ability parameters (hand-built rows, keyed by label) ---
            { "Cooldown", "Ticks before the ability can be used again." },
            { "Cast", "Cast-bar length; 0 = instant. Long casts can be interrupted." },
            { "Delay", "Ticks between committing and the effect landing." },
            { "Pre-lock", "Animation lock before the effect (can't act)." },
            { "Post-lock", "Animation lock after the effect (can't act)." },
            { "Damage", "Hit point damage dealt (scaled by Attack/Magic)." },
            { "Break amount", "Break-bar damage dealt by this ability." },
            { "Gem cost", "Spell gems consumed when the ability commits." },

            // --- Slot help (control panel) ---
            { "Slot", "Pick the ability in this slot, or (none) to leave it empty." },
        };

        /// <summary>Help text for a field/label key, or "" when undocumented.</summary>
        public static string Field(string key) =>
            key != null && Map.TryGetValue(key, out var d) ? d : "";
    }
}
