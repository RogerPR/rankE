using System.Collections.Generic;
using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Body-level presentation that complements the rigged animation: a short lunge
    /// toward the opponent on attack, colour flashes on hit/parry/heal, a shake while
    /// Broken, a casting tint, and the slide-apart while a Distance status is active.
    /// Lives on the persistent fighter anchor (the model is parented under it and
    /// supplied via <see cref="SetModel"/>), so it survives model swaps between fights.
    /// Pure presentation — reads events and state, computes nothing.
    /// </summary>
    public sealed class FighterViewBody : MonoBehaviour
    {
        const float LungeDistance = 0.4f;
        const float LungeSeconds = 0.25f;
        const float FlashSeconds = 0.18f;
        const float ShakeSeconds = 0.5f;
        const float DistanceSpread = 1.4f;

        // Procedural "stunned" indicator: a ring of small spheres orbiting above the head while
        // any action-blocking status (stun/broken) is active — works with no authored art.
        const int StarCount = 4;
        const float StarRadius = 0.28f;
        const float StarSpinDegPerSec = 220f;
        const float StarHeadGap = 0.35f;
        static readonly Color StunTint = new Color(0.92f, 0.85f, 0.35f); // sickly yellow

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/Lit
        static readonly int ColorId = Shader.PropertyToID("_Color");         // Standard

        int index;
        BattleDriver driver;
        Transform opponent;
        Vector3 basePos;

        MaterialPropertyBlock mpb;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<int> colorIds = new List<int>();
        readonly List<Color> baseColors = new List<Color>();

        float lungeT;
        float pendingLungeAt = -1f; // delays the lunge to align with a wind-up ability's effect
        float flashT;
        Color flashColor;
        float shakeT;
        float spread;

        GameObject starsRoot;
        float modelTopY = 2f; // world-space head height above the anchor, from model bounds

        public void Bind(BattleDriver driver, int fighterIndex, Transform opponent)
        {
            this.driver = driver;
            index = fighterIndex;
            this.opponent = opponent;
            basePos = transform.position;
            mpb = new MaterialPropertyBlock();
            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        /// <summary>Point the flash/tint at a freshly spawned model's renderers.</summary>
        public void SetModel(Transform modelRoot)
        {
            renderers.Clear();
            colorIds.Clear();
            baseColors.Clear();
            float top = 0f;
            if (modelRoot != null)
            {
                modelRoot.GetComponentsInChildren(true, renderers);
                foreach (var r in renderers)
                {
                    var m = r.sharedMaterial;
                    int id = m != null && m.HasProperty(BaseColorId) ? BaseColorId
                        : m != null && m.HasProperty(ColorId) ? ColorId : BaseColorId;
                    colorIds.Add(id);
                    baseColors.Add(m != null && m.HasProperty(id) ? m.GetColor(id) : Color.white);
                    top = Mathf.Max(top, r.bounds.max.y - transform.position.y);
                }
            }
            modelTopY = top > 0.1f ? top : 2f;
            BuildStars();
            ResetVisual();
        }

        /// <summary>(Re)build the orbiting "stunned" stars above the head — a handful of small
        /// emissive-ish spheres, hidden until a stun/broken status is active.</summary>
        void BuildStars()
        {
            if (starsRoot != null) Destroy(starsRoot);
            starsRoot = new GameObject("StunStars");
            starsRoot.transform.SetParent(transform, false);
            starsRoot.transform.localPosition = Vector3.up * (modelTopY + StarHeadGap);
            for (int i = 0; i < StarCount; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = s.GetComponent<Collider>();
                if (col != null) Destroy(col);
                s.transform.SetParent(starsRoot.transform, false);
                float a = i * Mathf.PI * 2f / StarCount;
                s.transform.localPosition = new Vector3(Mathf.Cos(a) * StarRadius, 0f, Mathf.Sin(a) * StarRadius);
                s.transform.localScale = Vector3.one * 0.12f;
                var rend = s.GetComponent<Renderer>();
                if (rend != null) rend.material.color = StunTint;
            }
            starsRoot.SetActive(false);
        }

        /// <summary>Back to the idle pose for a new fight.</summary>
        public void ResetVisual()
        {
            lungeT = flashT = shakeT = spread = 0f;
            pendingLungeAt = -1f;
            if (starsRoot != null) starsRoot.SetActive(false);
            transform.position = basePos;
            ApplyTint();
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.AbilityUsed && ev.Actor == index)
            {
                // Delay the lunge to land with a wind-up ability's effect (instant abilities lunge now).
                float lead = EffectLeadSeconds(ev.AbilityId);
                if (lead > 0.1f) pendingLungeAt = Time.time + lead - 0.1f;
                else lungeT = 1f;
            }
            else if (ev.Type == SimEventType.Damaged && ev.Target == index)
                Flash(new Color(1f, 0.25f, 0.25f));
            else if (ev.Type == SimEventType.Healed && ev.Target == index)
                Flash(new Color(0.3f, 1f, 0.3f));
            else if (ev.Type == SimEventType.Parried && ev.Actor == index)
                Flash(Color.white);
            else if (ev.Type == SimEventType.Broken && ev.Target == index)
                shakeT = 1f;
        }

        void Flash(Color c)
        {
            flashColor = c;
            flashT = 1f;
        }

        /// <summary>Seconds between an AbilityUsed event and its effect landing (PreLock/Delay),
        /// from the sim's ability data — read-only, mirrors <see cref="FighterAnimator"/>.</summary>
        float EffectLeadSeconds(string abilityId)
        {
            var content = driver != null && driver.Battle != null ? driver.Battle.Content : null;
            if (content == null || string.IsNullOrEmpty(abilityId)) return 0f;
            return content.Abilities.TryGetValue(abilityId, out var ab) && ab != null
                ? (ab.PreLockTicks + ab.DelayTicks) * SimConstants.TickDuration
                : 0f;
        }

        void Update()
        {
            var fighter = driver != null && driver.Battle != null ? driver.Battle.Fighters[index] : null;

            if (pendingLungeAt >= 0f && Time.time >= pendingLungeAt)
            {
                lungeT = 1f;
                pendingLungeAt = -1f;
            }

            lungeT = Mathf.Max(0f, lungeT - Time.deltaTime / LungeSeconds);
            flashT = Mathf.Max(0f, flashT - Time.deltaTime / FlashSeconds);
            shakeT = Mathf.Max(0f, shakeT - Time.deltaTime / ShakeSeconds);

            bool stunned = fighter != null && fighter.HasBlockingStatus;
            if (starsRoot != null)
            {
                if (starsRoot.activeSelf != stunned) starsRoot.SetActive(stunned);
                if (stunned) starsRoot.transform.Rotate(Vector3.up, StarSpinDegPerSec * Time.deltaTime, Space.Self);
            }

            var pos = basePos;
            if (stunned) // a woozy side-to-side sway reads as "stunned" even without a clip
                pos += Vector3.right * (Mathf.Sin(Time.time * 7f) * 0.06f);

            bool distanceActive = false;
            if (fighter != null)
                foreach (var f in driver.Battle.Fighters)
                    foreach (var s in f.Statuses)
                        if (s.Def.IsDistance) distanceActive = true;

            spread = Mathf.MoveTowards(spread, distanceActive ? DistanceSpread : 0f, Time.deltaTime * 4f);
            var away = opponent != null ? (basePos - opponent.position).normalized : Vector3.zero;
            pos += away * spread;

            if (lungeT > 0f && opponent != null)
            {
                var toward = (opponent.position - transform.position).normalized;
                pos += toward * (Mathf.Sin((1f - lungeT) * Mathf.PI) * LungeDistance);
            }
            if (shakeT > 0f)
                pos += (Vector3)(UnityEngine.Random.insideUnitCircle * 0.08f * shakeT);

            transform.position = pos;
            ApplyTint(fighter);
        }

        void ApplyTint(Fighter fighter = null)
        {
            if (renderers.Count == 0 || mpb == null) return;
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                Color c = baseColors[i];
                if (fighter != null && fighter.IsCasting)
                    c = Color.Lerp(c, new Color(0.4f, 0.6f, 1f),
                        0.4f * (0.5f + 0.5f * Mathf.Sin(Time.time * 12f)));
                if (fighter != null && fighter.HasBlockingStatus) // desaturated, pulsing yellow while stunned
                    c = Color.Lerp(c, StunTint, 0.45f + 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)));
                if (flashT > 0f)
                    c = Color.Lerp(c, flashColor, flashT);
                r.GetPropertyBlock(mpb);
                mpb.SetColor(colorIds[i], c);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
