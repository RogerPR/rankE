using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Crude procedural capsule animation (Phase 2 checkbox): lunge on attack,
    /// color flashes on hit/parry/heal, shake on break, casting pulse, tip over on
    /// death, slide apart while Distance is active. Pure presentation — reads
    /// events and state, computes nothing. Real animation packs arrive in Phase 3.
    /// </summary>
    public sealed class FighterViewBody : MonoBehaviour
    {
        const float LungeDistance = 0.6f;
        const float LungeSeconds = 0.25f;
        const float FlashSeconds = 0.18f;
        const float ShakeSeconds = 0.5f;
        const float DistanceSpread = 1.4f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        int index;
        BattleDriver driver;
        Transform opponent;

        Vector3 basePos;
        Quaternion baseRot;
        Renderer rend;
        MaterialPropertyBlock mpb;
        Color baseColor;

        float lungeT;
        float flashT;
        Color flashColor;
        float shakeT;
        float deathT;
        bool dead;
        float spread; // current Distance separation, smoothed

        public void Bind(BattleDriver driver, int fighterIndex, Transform opponent)
        {
            this.driver = driver;
            index = fighterIndex;
            this.opponent = opponent;

            basePos = transform.position;
            baseRot = transform.rotation;
            rend = GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();
            baseColor = rend != null && rend.sharedMaterial != null
                && rend.sharedMaterial.HasProperty(BaseColorId)
                ? rend.sharedMaterial.GetColor(BaseColorId)
                : Color.white;

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        /// <summary>Back to the idle pose for a new fight.</summary>
        public void ResetVisual()
        {
            lungeT = flashT = shakeT = deathT = spread = 0f;
            dead = false;
            transform.position = basePos;
            transform.rotation = baseRot;
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
            else if (ev.Type == SimEventType.FighterDied && ev.Actor == index)
                dead = true;
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
            if (dead) deathT = Mathf.Min(1f, deathT + Time.deltaTime * 2f);

            // Position: lunge toward the opponent, shake while broken, drift apart on Distance.
            var pos = basePos;

            bool distanceActive = false;
            if (fighter != null)
            {
                foreach (var f in driver.Battle.Fighters)
                    foreach (var s in f.Statuses)
                        if (s.Def.IsDistance) distanceActive = true;
            }
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

            // Death: tip over sideways.
            transform.rotation = dead
                ? baseRot * Quaternion.Euler(0f, 0f, 90f * Mathf.SmoothStep(0f, 1f, deathT))
                : baseRot;

            // Tint: flash > casting pulse > base.
            if (rend != null)
            {
                Color c = baseColor;
                if (fighter != null && fighter.IsCasting)
                    c = Color.Lerp(baseColor, new Color(0.4f, 0.6f, 1f),
                        0.5f + 0.5f * Mathf.Sin(Time.time * 12f));
                if (flashT > 0f)
                    c = Color.Lerp(c, flashColor, flashT);
                rend.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, c);
                rend.SetPropertyBlock(mpb);
            }
        }
    }
}
