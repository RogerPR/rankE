using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Spawns one fighter's skill VFX reactively from sim events: muzzle flashes and
    /// projectiles on release, a channel aura while casting, and reaction bursts
    /// (hit/heal/parry/break/riposte/death). Lives on the persistent fighter anchor like
    /// <see cref="FighterViewBody"/> (so it survives model swaps); <see cref="SetModel"/>
    /// re-resolves the hand spawn bone each fight. Looks effects up in the
    /// <see cref="AbilityVfxRegistry"/> by ability id / cue, scales them by the registry's
    /// feel knobs, and times projectiles to land roughly when the hit lands — but it reads
    /// ability ticks only as a knob and changes no sim timing. Pure presentation.
    /// </summary>
    public sealed class FighterVfx : MonoBehaviour
    {
        const string HandBoneName = "+R Hand"; // Meshtint humanoid skeleton; absent on monsters

        BattleDriver driver;
        int index;
        Transform opponent;
        AbilityVfxRegistry reg;

        Transform handBone;     // resolved per model; null on monsters → chest fallback
        GameObject castAura;    // the active channel aura, if any

        public void Bind(BattleDriver driver, int fighterIndex, Transform opponentAnchor,
            AbilityVfxRegistry registry)
        {
            this.driver = driver;
            index = fighterIndex;
            opponent = opponentAnchor;
            reg = registry;
            if (driver != null) driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        /// <summary>Point spawns at a freshly spawned model: find its hand bone.</summary>
        public void SetModel(Transform modelRoot)
        {
            ClearAura();
            handBone = null;
            if (modelRoot == null) return;
            foreach (var tr in modelRoot.GetComponentsInChildren<Transform>(true))
                if (tr.name == HandBoneName) { handBone = tr; break; }
        }

        void OnSimEvent(SimEvent ev)
        {
            if (reg == null) return;

            switch (ev.Type)
            {
                case SimEventType.AbilityUsed when ev.Actor == index:
                    OnRelease(ev.AbilityId);
                    break;
                case SimEventType.CastStarted when ev.Actor == index:
                    OnCastStarted(ev.AbilityId);
                    break;
                case SimEventType.CastCompleted when ev.Actor == index:
                case SimEventType.CastInterrupted when ev.Actor == index:
                    ClearAura();
                    break;

                case SimEventType.Damaged when ev.Target == index && ev.Amount < 0:
                    // Status DoT ticks (poison) arrive as negative-amount Damaged; only
                    // positive amounts are real hits. Suppress so DoT doesn't spam bursts.
                    break;
                case SimEventType.Damaged when ev.Target == index:
                    SpawnCue(VfxCue.Hit);
                    break;
                case SimEventType.Healed when ev.Target == index:
                    SpawnCue(VfxCue.Heal);
                    break;
                case SimEventType.Parried when ev.Actor == index:
                    SpawnCue(VfxCue.Parry);
                    break;
                case SimEventType.Broken when ev.Target == index:
                    SpawnCue(VfxCue.Break);
                    break;
                case SimEventType.RiposteTriggered when ev.Actor == index:
                    SpawnCue(VfxCue.Riposte);
                    break;
                case SimEventType.FighterDied when ev.Actor == index:
                    ClearAura();
                    SpawnCue(VfxCue.Death);
                    break;
            }
        }

        void OnRelease(string abilityId)
        {
            var def = reg.DefFor(abilityId);
            if (def == null) return;

            Vector3 muzzle = HandPoint();
            SpawnOneShot(def.Muzzle, muzzle, handBone);

            if (def.Mode != ProjectileMode.None && def.Projectile != null)
            {
                var go = Instantiate(def.Projectile);
                var vp = go.AddComponent<VfxProjectile>();
                vp.Launch(muzzle, opponent, reg.ChestHeight, def.Mode, reg.FallHeight,
                    TravelSeconds(def, abilityId), def.Impact, reg.VfxScale);
            }
        }

        void OnCastStarted(string abilityId)
        {
            var def = reg.DefFor(abilityId);
            if (def == null || def.CastAura == null) return;
            ClearAura();
            castAura = SpawnOneShot(def.CastAura, HandPoint(), handBone, autoDestroy: false);
        }

        /// <summary>Seconds for a projectile to reach the target. Fall (delayed abilities)
        /// is timed to the sim's DelayTicks so it lands on the delayed hit; everything else
        /// uses a short travel. Reads ability data read-only — never writes sim timing.</summary>
        float TravelSeconds(AbilityVfxDef def, string abilityId)
        {
            if (def.TravelSeconds > 0f) return def.TravelSeconds;
            var ab = Ability(abilityId);
            if (def.Mode == ProjectileMode.Fall && ab != null && ab.DelayTicks > 0)
                return ab.DelayTicks * SimConstants.TickDuration;
            return reg.DefaultTravelSeconds;
        }

        AbilityDef Ability(string abilityId)
        {
            var content = driver != null && driver.Battle != null ? driver.Battle.Content : null;
            if (content == null || string.IsNullOrEmpty(abilityId)) return null;
            return content.Abilities.TryGetValue(abilityId, out var d) ? d : null;
        }

        // --- spawn helpers ---

        Vector3 HandPoint() =>
            handBone != null ? handBone.position : ChestPoint();

        Vector3 ChestPoint() =>
            transform.position + Vector3.up * (reg != null ? reg.ChestHeight : 1.1f);

        void SpawnCue(VfxCue cue) => SpawnOneShot(reg.PrefabFor(cue), ChestPoint(), null);

        GameObject SpawnOneShot(GameObject prefab, Vector3 pos, Transform parent,
            bool autoDestroy = true)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, pos, Quaternion.identity, parent);
            if (reg.VfxScale != 1f) go.transform.localScale *= reg.VfxScale;
            if (autoDestroy) Destroy(go, reg.CueLifetime);
            return go;
        }

        void ClearAura()
        {
            if (castAura != null) Destroy(castAura);
            castAura = null;
        }
    }
}
