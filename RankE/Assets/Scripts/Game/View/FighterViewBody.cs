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
        float flashT;
        Color flashColor;
        float shakeT;
        float spread;

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
                }
            }
            ResetVisual();
        }

        /// <summary>Back to the idle pose for a new fight.</summary>
        public void ResetVisual()
        {
            lungeT = flashT = shakeT = spread = 0f;
            transform.position = basePos;
            ApplyTint();
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.AbilityUsed && ev.Actor == index)
                lungeT = 1f;
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

        void Update()
        {
            var fighter = driver != null && driver.Battle != null ? driver.Battle.Fighters[index] : null;

            lungeT = Mathf.Max(0f, lungeT - Time.deltaTime / LungeSeconds);
            flashT = Mathf.Max(0f, flashT - Time.deltaTime / FlashSeconds);
            shakeT = Mathf.Max(0f, shakeT - Time.deltaTime / ShakeSeconds);

            var pos = basePos;

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
                if (flashT > 0f)
                    c = Color.Lerp(c, flashColor, flashT);
                r.GetPropertyBlock(mpb);
                mpb.SetColor(colorIds[i], c);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
