using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Bounds → (uniform scale, ground Y-offset) fit math, shared by the editor
    /// <c>ArtSetupBuilder</c> (measuring sample/monster prefabs) and the runtime
    /// <see cref="CharacterAssembler"/> (measuring a freshly assembled model). Runtime
    /// safe — only reads <c>Renderer.bounds</c>.
    /// </summary>
    public static class ModelFit
    {
        /// <summary>
        /// Measure an instance that is at the origin with identity rotation and unit
        /// scale, then derive a uniform scale that makes it <paramref name="targetHeight"/>
        /// tall plus a Y offset that seats its lowest renderer point on the ground (y=0).
        /// </summary>
        public static void Measure(GameObject instance, float targetHeight,
            out float scale, out float yOffset)
        {
            scale = 1f;
            yOffset = 0f;

            bool any = false;
            Bounds b = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            if (any && b.size.y > 0.0001f)
            {
                scale = Mathf.Clamp(targetHeight / b.size.y, 0.1f, 8f);
                yOffset = -scale * b.min.y;
            }
        }
    }
}
