using System;
using System.Collections.Generic;

namespace RankE.Sim
{
    /// <summary>A delayed ability waiting to land (visible timer in the UI later).</summary>
    public sealed class PendingDelayed
    {
        public int FireTick;
        public int Source;
        public AbilityDef Ability;
    }

    /// <summary>
    /// Deterministic, tick-based 1v1 battle. Per tick: advance timers → tick statuses →
    /// break decay → auto-attacks → resolve both fighters' actions (gather, then apply,
    /// so simultaneous actions don't shadow each other — PoC parity) → delayed effects →
    /// death check. All randomness flows through Rng (AI only; resolution is random-free).
    /// </summary>
    public sealed class Battle
    {
        public readonly Fighter[] Fighters;
        public readonly ContentDb Content;
        public readonly CombatTuning Tuning;
        public readonly Random Rng;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public readonly List<PendingDelayed> Pending = new List<PendingDelayed>();

        public int CurrentTick { get; private set; }
        public bool IsOver { get; private set; }

        /// <summary>Winning fighter index; -1 while running or on a timeout draw.</summary>
        public int Winner { get; private set; } = -1;

        readonly string[] intents = new string[2];
        readonly AbilityDef riposteDef;

        public Battle(FighterConfig a, FighterConfig b, ContentDb content, CombatTuning tuning, int seed)
        {
            Content = content;
            Tuning = tuning;
            Rng = new Random(seed);
            Fighters = new[] { new Fighter(0, a, content), new Fighter(1, b, content) };
            content.Abilities.TryGetValue(tuning.RiposteAbilityId, out riposteDef);
        }

        /// <summary>Queue an ability for the upcoming Step (null = do nothing).</summary>
        public void SubmitIntent(int fighter, string abilityId) => intents[fighter] = abilityId;

        public void Step()
        {
            if (IsOver) return;

            foreach (var f in Fighters) AdvanceTimers(f);
            foreach (var f in Fighters) TickGemRegen(f);
            foreach (var f in Fighters) TickStatuses(f);
            foreach (var f in Fighters) TickBreakDecay(f);

            // Auto-attacks land before deliberate actions (PoC loop order).
            var auto = new AbilityDef[2];
            for (int i = 0; i < 2; i++) auto[i] = TryAutoAttack(Fighters[i]);
            for (int i = 0; i < 2; i++)
                if (auto[i] != null) ApplyAbility(i, auto[i]);

            // Gather both committed actions, then apply both.
            var acts = new AbilityDef[2];
            for (int i = 0; i < 2; i++) acts[i] = ResolveAction(Fighters[i], intents[i]);
            for (int i = 0; i < 2; i++)
                if (acts[i] != null) ApplyAbility(i, acts[i]);

            // Delayed abilities whose timer expired.
            for (int p = 0; p < Pending.Count; )
            {
                if (Pending[p].FireTick <= CurrentTick)
                {
                    var pd = Pending[p];
                    Pending.RemoveAt(p);
                    Emit(SimEventType.DelayedFired, pd.Source, ability: pd.Ability.Id);
                    ApplyEffectsNow(pd.Source, pd.Ability);
                }
                else p++;
            }

            CheckDeaths();

            intents[0] = intents[1] = null;
            CurrentTick++;
        }

        // ---- per-tick upkeep ----

        static void AdvanceTimers(Fighter f)
        {
            if (f.GcdRemaining > 0) f.GcdRemaining--;
            if (f.LockRemaining > 0) f.LockRemaining--;
            if (f.IsCasting) f.CastRemaining--;
            if (f.IsWindingUp) f.WindupRemaining--;
            if (f.AutoAttack != null && f.AutoAttackRemaining > 0) f.AutoAttackRemaining--;
            foreach (var a in f.Abilities)
                if (a.CooldownRemaining > 0) a.CooldownRemaining--;
            f.TicksSinceBreakDamage++;
        }

        void TickStatuses(Fighter f)
        {
            for (int s = 0; s < f.Statuses.Count; )
            {
                var st = f.Statuses[s];
                var def = st.Def;

                // PoC parity: applies whenever remaining % interval == 0, so a fresh
                // status with duration divisible by the interval ticks immediately.
                if (def.HpPerInterval != 0 && def.IntervalTicks > 0 && st.Remaining % def.IntervalTicks == 0)
                {
                    if (def.HpPerInterval > 0)
                    {
                        int heal = Math.Min(def.HpPerInterval, f.MaxHp - f.Hp);
                        f.Hp += heal;
                        Emit(SimEventType.Healed, f.Index, target: f.Index, status: def.Id, amount: heal);
                    }
                    else
                    {
                        f.Hp += def.HpPerInterval;
                        Emit(SimEventType.Damaged, f.Index, target: f.Index, status: def.Id, amount: -def.HpPerInterval);
                    }
                }

                if (def.CancelsCast && (f.IsCasting || f.IsWindingUp))
                {
                    CancelCastAndWindup(f);
                    Emit(SimEventType.CastInterrupted, f.Index, target: f.Index, status: def.Id);
                }

                st.Remaining--;
                if (st.Remaining <= 0)
                {
                    f.Statuses.RemoveAt(s);
                    Emit(SimEventType.StatusExpired, f.Index, target: f.Index, status: def.Id);
                }
                else s++;
            }
        }

        void TickGemRegen(Fighter f)
        {
            if (f.Stats.GemRegenIntervalTicks <= 0 || f.SpellGems >= f.MaxSpellGems) return;
            if (--f.GemRegenRemaining > 0) return;
            f.SpellGems = Math.Min(f.MaxSpellGems, f.SpellGems + 1);
            f.GemRegenRemaining = f.Stats.GemRegenIntervalTicks;
            Emit(SimEventType.GemRegenerated, f.Index, target: f.Index, amount: f.SpellGems);
        }

        void TickBreakDecay(Fighter f)
        {
            if (f.BreakBar > 0
                && f.TicksSinceBreakDamage >= Tuning.BreakDecayGraceTicks
                && (f.TicksSinceBreakDamage - Tuning.BreakDecayGraceTicks) % Tuning.BreakDecayIntervalTicks == 0)
            {
                f.BreakBar = Math.Max(0, f.BreakBar - Tuning.BreakDecayAmount);
            }
        }

        AbilityDef TryAutoAttack(Fighter f)
        {
            if (f.AutoAttack == null || f.AutoAttackRemaining > 0) return null;
            if (!f.CanAct || f.IsCasting || f.IsWindingUp || f.LockRemaining > 0) return null;
            f.AutoAttackRemaining = f.AutoAttackInterval;
            Emit(SimEventType.AbilityUsed, f.Index, ability: f.AutoAttack.Def.Id);
            return f.AutoAttack.Def;
        }

        // ---- action resolution ----

        /// <summary>Returns the ability committed this tick, or null.</summary>
        AbilityDef ResolveAction(Fighter f, string intent)
        {
            // Cast completion takes the slot; input is ignored that tick (PoC act()).
            if (f.IsCasting && f.CastRemaining <= 0)
            {
                var st = f.Casting;
                f.Casting = null;
                f.CastRemaining = 0;
                st.CooldownRemaining = (int)(st.EffCooldownTicks * f.CooldownUseMult);
                f.SpellGems -= st.Def.GemCost;
                CommitColorCombo(f, st.Def);
                f.LockRemaining = Math.Max(f.LockRemaining, st.Def.PostLockTicks);
                Emit(SimEventType.CastCompleted, f.Index, ability: st.Def.Id);
                Emit(SimEventType.AbilityUsed, f.Index, ability: st.Def.Id);
                return st.Def;
            }

            // Wind-up completion: effects fire at the effect frame.
            if (f.IsWindingUp && f.WindupRemaining <= 0)
            {
                var st = f.Windup;
                f.Windup = null;
                f.WindupRemaining = 0;
                f.LockRemaining = Math.Max(f.LockRemaining, st.Def.PostLockTicks);
                return st.Def;
            }

            return TryUse(f, intent);
        }

        AbilityDef TryUse(Fighter f, string abilityId)
        {
            if (abilityId == null) return null;
            if (!f.CanAct) return null;
            if (f.LockRemaining > 0 || f.IsWindingUp) return null;

            var st = f.GetAbility(abilityId);
            if (st == null) return null;
            if (f.IsCasting && !st.Def.UsableWhileCasting) return null;
            if (!st.IsReady) return null;
            if (f.SpellGems < st.Def.GemCost) return null;

            switch (st.Def.Gcd)
            {
                case GcdClass.Quick:
                    // Quick actions ignore the GCD but trigger a short one. PoC overwrote
                    // the GCD outright; max() avoids quick actions *shortening* it.
                    f.GcdRemaining = Math.Max(f.GcdRemaining, Tuning.QuickGcdTicks);
                    break;
                case GcdClass.Normal:
                    if (f.GcdRemaining > 0) return null;
                    f.GcdRemaining = Tuning.GcdTicks;
                    break;
                default:
                    return null; // GcdClass.None is not player-usable
            }

            if (st.Def.Gcd == GcdClass.Normal && st.EffCastTicks > 0)
            {
                f.Casting = st;
                f.CastRemaining = st.EffCastTicks;
                Emit(SimEventType.CastStarted, f.Index, ability: st.Def.Id, amount: st.EffCastTicks);
                return null;
            }

            // Instant: commit now (cooldown, gems, combo), effects now or after wind-up.
            st.CooldownRemaining = (int)(st.EffCooldownTicks * f.CooldownUseMult);
            f.SpellGems -= st.Def.GemCost;
            CommitColorCombo(f, st.Def);
            Emit(SimEventType.AbilityUsed, f.Index, ability: st.Def.Id);

            if (st.Def.PreLockTicks > 0)
            {
                f.Windup = st;
                f.WindupRemaining = st.Def.PreLockTicks;
                return null;
            }

            f.LockRemaining = Math.Max(f.LockRemaining, st.Def.PostLockTicks);
            return st.Def;
        }

        /// <summary>The player's colour-sequence combo (player only). Pressing the displayed
        /// colours in order advances the chain; finishing it grants the empowered status (the
        /// next damaging hit is doubled). A wrong colour reshuffles into a fresh random sequence.
        /// Combo-neutral abilities (no <see cref="AbilityDef.ComboColor"/>, and all quick
        /// actions) are ignored — they neither advance nor reset.</summary>
        void CommitColorCombo(Fighter f, AbilityDef def)
        {
            if (!f.UsesComboSystem || def.Gcd != GcdClass.Normal || def.ComboColor == null) return;

            if (f.ComboSequence.Count == 0)
                GenerateComboSequence(f);
            if (f.ComboSequence.Count == 0) return; // no coloured abilities → nothing to match

            if (def.ComboColor == f.ComboSequence[f.ComboProgress])
            {
                f.ComboProgress++;
                if (f.ComboProgress >= f.ComboSequence.Count)
                {
                    // Completed: arm the empowered grant (applied after this ability resolves so
                    // the completing hit isn't the doubled one — the *next* ability is) and
                    // reshuffle a fresh sequence to chase.
                    f.PendingEmpowerGrant = true;
                    Emit(SimEventType.ComboCompleted, f.Index, ability: def.Id);
                    GenerateComboSequence(f);
                }
                else
                {
                    Emit(SimEventType.ComboAdvanced, f.Index, ability: def.Id, amount: f.ComboProgress);
                }
                return;
            }

            // Wrong colour: reshuffle. If the new sequence happens to open on this colour, count it.
            Emit(SimEventType.ComboReset, f.Index, ability: def.Id);
            GenerateComboSequence(f);
            if (f.ComboSequence.Count > 0 && def.ComboColor == f.ComboSequence[0])
            {
                f.ComboProgress = 1;
                Emit(SimEventType.ComboAdvanced, f.Index, ability: def.Id, amount: 1);
            }
        }

        /// <summary>Builds a fresh random target sequence (length ComboMin..ComboMax) drawn only
        /// from the distinct colours present on the fighter's abilities, and resets progress.
        /// Uses the seeded <see cref="Rng"/>, so combos stay deterministic/replayable.</summary>
        void GenerateComboSequence(Fighter f)
        {
            f.ComboSequence.Clear();
            f.ComboProgress = 0;

            var colors = new List<string>();
            foreach (var a in f.Abilities)
            {
                var c = a.Def.ComboColor;
                if (c != null && a.Def.Gcd == GcdClass.Normal && !colors.Contains(c))
                    colors.Add(c);
            }
            if (colors.Count == 0) return;

            int len = Tuning.ComboMinLen;
            if (Tuning.ComboMaxLen > Tuning.ComboMinLen)
                len += Rng.Next(Tuning.ComboMaxLen - Tuning.ComboMinLen + 1);
            for (int i = 0; i < len; i++)
                f.ComboSequence.Add(colors[Rng.Next(colors.Count)]);
        }

        // ---- effect application ----

        void ApplyAbility(int src, AbilityDef def)
        {
            if (def.DelayTicks > 0)
            {
                Pending.Add(new PendingDelayed
                {
                    FireTick = CurrentTick + def.DelayTicks,
                    Source = src,
                    Ability = def,
                });
                Emit(SimEventType.DelayedScheduled, src, ability: def.Id, amount: def.DelayTicks);
                return;
            }
            ApplyEffectsNow(src, def);
        }

        void ApplyEffectsNow(int src, AbilityDef def)
        {
            var attacker = Fighters[src];
            var defender = Fighters[1 - src];

            bool whiffed = def.IsMelee && DistanceActive();
            if (whiffed)
                Emit(SimEventType.AbilityWhiffed, src, ability: def.Id);

            bool parried = false;
            if (!whiffed && def.Parriable && defender.HasStatus(Tuning.ParryStatusId))
            {
                parried = true;
                Emit(SimEventType.Parried, defender.Index, target: src, ability: def.Id);
                ApplyBreak(attacker, Tuning.ParryBreakToAttacker, defender.Index);
                defender.RiposteCounter = Math.Min(
                    defender.RiposteCounter + Tuning.ParryRiposteGain, Tuning.RiposteCounterMax);
                if (defender.RiposteCounter >= Tuning.RiposteCounterMax && riposteDef != null)
                {
                    // Automatic riposte the moment the counter fills (PROPOSED; the PoC
                    // hijacked the next pressed ability instead).
                    defender.RiposteCounter = 0;
                    Emit(SimEventType.RiposteTriggered, defender.Index, target: src);
                    Emit(SimEventType.AbilityUsed, defender.Index, ability: riposteDef.Id);
                    ApplyEffectsNow(defender.Index, riposteDef);
                }
            }

            bool negated = whiffed || parried;
            bool dealtDamage = false;
            foreach (var e in def.Effects)
            {
                bool toSelf = e.Target == EffectTarget.Self;
                if (!toSelf && negated) continue;
                ApplyEffect(src, def, e, toSelf ? attacker : defender);
                if (e.Kind == EffectKinds.Damage && !toSelf) dealtDamage = true;
            }

            // Spend the empowered hit once it lands, then grant a freshly-earned one — so a combo
            // completed by *this* ability empowers the next ability, not this one.
            if (dealtDamage) attacker.ConsumeDamageDealtBuffs();
            if (attacker.PendingEmpowerGrant)
            {
                attacker.PendingEmpowerGrant = false;
                ApplyStatus(attacker, Tuning.EmpoweredStatusId, Tuning.EmpoweredDurationTicks, src);
            }
        }

        void ApplyEffect(int src, AbilityDef def, EffectDef e, Fighter target)
        {
            switch (e.Kind)
            {
                case EffectKinds.Damage:
                {
                    // Derived damage (GAME_DESIGN §1): offense-scaled & crit core, then
                    // defense and damage-taken mults. Neutral stats reproduce flat PoC numbers.
                    // DamageDealtMult folds in the empowered (×2) combo reward.
                    var attacker = Fighters[src];
                    int offense = OffenseFor(attacker, e.School);
                    double crit = RollCrit(attacker) ? attacker.Stats.CritDamage : 1.0;
                    double core = Math.Round(
                        e.Amount * attacker.DamageDealtMult() * (1 + offense / 100.0) * crit,
                        MidpointRounding.AwayFromZero);
                    double defMult = 100.0 / Math.Max(1, 100 + target.Stats.Defense);
                    int dmg = (int)(core * defMult * target.DamageTakenMult * target.StatusDamageTakenMult(e.School));
                    // Absorb shields (e.g. Block's physical shield) soak what's left after reduction.
                    int soaked = target.AbsorbDamage(e.School, dmg);
                    if (soaked > 0)
                    {
                        dmg -= soaked;
                        Emit(SimEventType.ShieldAbsorbed, src, target: target.Index, ability: def.Id, amount: soaked);
                    }
                    target.Hp -= dmg;
                    Emit(SimEventType.Damaged, src, target: target.Index, ability: def.Id, amount: dmg);
                    break;
                }
                case EffectKinds.BreakDamage:
                    ApplyBreak(target, e.Amount, src);
                    break;
                case EffectKinds.ApplyStatus:
                    ApplyStatus(target, e.StatusId, e.DurationTicks, src);
                    break;
                case EffectKinds.ApplyShield:
                    ApplyStatus(target, e.StatusId, e.DurationTicks, src, absorbOverride: e.Amount);
                    break;
                case EffectKinds.ClearStatus:
                {
                    for (int s = target.Statuses.Count - 1; s >= 0; s--)
                    {
                        if (target.Statuses[s].Def.Id == e.StatusId)
                        {
                            target.Statuses.RemoveAt(s);
                            Emit(SimEventType.StatusExpired, src, target: target.Index, status: e.StatusId);
                        }
                    }
                    break;
                }
                case EffectKinds.InterruptCast:
                {
                    bool self = target.Index == src;
                    if (target.IsCasting && (self || target.Casting.Def.Interruptible))
                    {
                        CancelCast(target);
                        Emit(SimEventType.CastInterrupted, src, target: target.Index, ability: def.Id);
                        if (e.StatusId != null)
                            ApplyStatus(target, e.StatusId, e.DurationTicks, src);
                    }
                    break;
                }
            }
        }

        void ApplyStatus(Fighter target, string statusId, int durationTicks, int src, int absorbOverride = -1)
        {
            var def = Content.Status(statusId);
            // Shield effects supply the pool size per-ability; plain statuses use the def default.
            int absorb = absorbOverride >= 0 ? absorbOverride : def.AbsorbAmount;

            StatusInstance existing = null;
            foreach (var s in target.Statuses)
                if (s.Def.Id == statusId) { existing = s; break; }

            if (existing != null)
            {
                existing.Remaining = durationTicks; // reapply refreshes (PoC parity)
                existing.AbsorbRemaining = absorb;  // refresh the absorb pool too
            }
            else
                target.Statuses.Add(new StatusInstance
                {
                    Def = def,
                    Remaining = durationTicks,
                    AbsorbRemaining = absorb,
                });

            Emit(SimEventType.StatusApplied, src, target: target.Index, status: statusId, amount: durationTicks);

            if (def.CancelsCast && (target.IsCasting || target.IsWindingUp))
            {
                CancelCastAndWindup(target);
                Emit(SimEventType.CastInterrupted, src, target: target.Index, status: statusId);
            }
        }

        /// <summary>Offense stat that scales an effect's damage by its school.</summary>
        static int OffenseFor(Fighter attacker, string school)
        {
            switch (school)
            {
                case Schools.Magic:
                case Schools.Support:
                    return attacker.Stats.Magic;
                case Schools.True:
                    return 0;
                default: // physical
                    return attacker.Stats.Attack;
            }
        }

        /// <summary>Seeded crit roll. Draws no RNG when CritChance is 0, so fights without
        /// crit stay byte-identical to the pre-stat-sheet sim.</summary>
        bool RollCrit(Fighter attacker)
        {
            int c = attacker.Stats.CritChance;
            return c > 0 && Rng.Next(100) < c;
        }

        void ApplyBreak(Fighter target, int amount, int src)
        {
            if (amount <= 0) return;
            amount = (int)(amount * Fighters[src].Stats.BreakPower); // Break Power (×1.0 neutral)
            if (amount <= 0) return;
            target.BreakBar += amount;
            target.TicksSinceBreakDamage = 0;
            Emit(SimEventType.BreakDamaged, src, target: target.Index, amount: amount);
            if (target.BreakBar >= Tuning.BreakMax)
            {
                target.BreakBar = 0;
                Emit(SimEventType.Broken, src, target: target.Index);
                ApplyStatus(target, Tuning.BrokenStatusId, Tuning.BrokenDurationTicks, src);
            }
        }

        static void CancelCast(Fighter f)
        {
            f.Casting = null;
            f.CastRemaining = 0;
        }

        static void CancelCastAndWindup(Fighter f)
        {
            CancelCast(f);
            f.Windup = null;
            f.WindupRemaining = 0;
        }

        bool DistanceActive()
        {
            foreach (var f in Fighters)
                foreach (var s in f.Statuses)
                    if (s.Def.IsDistance) return true;
            return false;
        }

        void CheckDeaths()
        {
            // Fighter 0 loses simultaneous deaths (PoC tie rule).
            if (Fighters[0].Hp <= 0)
            {
                Winner = 1;
                IsOver = true;
                Emit(SimEventType.FighterDied, 0);
            }
            else if (Fighters[1].Hp <= 0)
            {
                Winner = 0;
                IsOver = true;
                Emit(SimEventType.FighterDied, 1);
            }
        }

        void Emit(SimEventType type, int actor, int target = -1, string ability = null,
            string status = null, int amount = 0)
        {
            Events.Add(new SimEvent
            {
                Tick = CurrentTick,
                Type = type,
                Actor = actor,
                Target = target,
                AbilityId = ability,
                StatusId = status,
                Amount = amount,
            });
        }
    }
}
