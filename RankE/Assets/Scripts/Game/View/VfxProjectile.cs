using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Decorative projectile mover: eases a spawned VFX prefab from a start point to its
    /// target (or drops it from above), orients it along travel, then spawns an impact
    /// burst and self-destructs on arrival. <b>Pure presentation — no Rigidbody, no
    /// collider, no physics, no gameplay.</b> The sim already decided the hit; this just
    /// makes it visible and is timed so it lands roughly when the <c>Damaged</c> event
    /// fires (see <see cref="FighterVfx"/>).
    /// </summary>
    public sealed class VfxProjectile : MonoBehaviour
    {
        Vector3 start;        // launch point for Travel
        Transform target;     // re-aimed each frame so it tracks small body movement
        float targetHeight;   // chest offset above the target's feet
        float fallHeight;     // Fall: start this far above the aim point
        ProjectileMode mode;
        float seconds;
        float elapsed;
        GameObject impactPrefab;
        float vfxScale = 1f;

        public void Launch(Vector3 startPos, Transform targetTransform, float chestHeight,
            ProjectileMode projectileMode, float fall, float travelSeconds,
            GameObject impact, float scale)
        {
            start = startPos;
            target = targetTransform;
            targetHeight = chestHeight;
            fallHeight = fall;
            mode = projectileMode;
            seconds = Mathf.Max(0.05f, travelSeconds);
            impactPrefab = impact;
            vfxScale = scale <= 0f ? 1f : scale;
            elapsed = 0f;

            transform.localScale *= vfxScale;
            transform.position = From();
            Orient(AimPoint() - transform.position);
        }

        Vector3 AimPoint() =>
            target != null ? target.position + Vector3.up * targetHeight : start;

        Vector3 From() =>
            mode == ProjectileMode.Fall ? AimPoint() + Vector3.up * fallHeight : start;

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            // Smoothstep ease so it doesn't read as a linear slide.
            float e = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(From(), AimPoint(), e);
            Orient(pos - transform.position);
            transform.position = pos;

            if (t >= 1f) Arrive();
        }

        void Arrive()
        {
            if (impactPrefab != null)
            {
                var fx = Instantiate(impactPrefab, transform.position, Quaternion.identity);
                if (vfxScale != 1f) fx.transform.localScale *= vfxScale;
            }
            Destroy(gameObject);
        }

        void Orient(Vector3 dir)
        {
            if (dir.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
