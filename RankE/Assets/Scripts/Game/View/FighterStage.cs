using RankE.Sim;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Spawns the chosen 3D models for each fight. The two scene capsules become
    /// persistent invisible anchors (UI bars and the body lunge/flash ride on them);
    /// the selected character/monster prefab is parented under its anchor at countdown,
    /// gets the right Animator controller, and is wired to a <see cref="FighterAnimator"/>.
    /// If the visual registry hasn't been built yet, it falls back to the visible
    /// capsules so the fight still runs. Pure presentation.
    /// </summary>
    public sealed class FighterStage : MonoBehaviour
    {
        [SerializeField] float groundY = 0f;
        [SerializeField] float playerYaw = 90f;  // face +X toward the enemy
        [SerializeField] float enemyYaw = -90f;   // face -X toward the player

        BattleDriver driver;
        MatchController match;
        Transform playerAnchor, enemyAnchor;
        FighterVisualRegistry registry;

        FighterViewBody playerBody, enemyBody;
        GameObject playerModel, enemyModel;

        public bool HasContent =>
            registry != null && registry.Players.Count > 0 && registry.Monsters.Count > 0;

        public void Init(BattleDriver driver, MatchController match,
            Transform playerAnchor, Transform enemyAnchor)
        {
            this.driver = driver;
            this.match = match;
            this.playerAnchor = playerAnchor;
            this.enemyAnchor = enemyAnchor;
            registry = FighterVisualRegistry.Load();

            playerBody = playerAnchor.gameObject.AddComponent<FighterViewBody>();
            playerBody.Bind(driver, 0, enemyAnchor);
            enemyBody = enemyAnchor.gameObject.AddComponent<FighterViewBody>();
            enemyBody.Bind(driver, 1, playerAnchor);

            if (HasContent)
            {
                StripPlaceholder(playerAnchor);
                StripPlaceholder(enemyAnchor);
            }
            else
            {
                // No art yet: flash the capsule itself, no model/animator.
                Debug.LogWarning("FighterStage: visual registry not found/empty — " +
                    "using placeholder capsules. Run Tools ▸ RANK E ▸ Build Art Setup.");
                playerBody.SetModel(playerAnchor);
                enemyBody.SetModel(enemyAnchor);
            }

            match.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (match != null) match.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(MatchState state)
        {
            if (state != MatchState.Countdown) return;
            if (HasContent) SpawnForMatch();
            else { playerBody.ResetVisual(); enemyBody.ResetVisual(); }
        }

        void SpawnForMatch()
        {
            var pl = registry.PlayerAt(match.Loadout.PlayerVisualIndex);
            var en = registry.MonsterAt(match.Loadout.EnemyVisualIndex);
            playerModel = Spawn(pl, playerAnchor, playerYaw, playerModel, 0, playerBody);
            enemyModel = Spawn(en, enemyAnchor, enemyYaw, enemyModel, 1, enemyBody);
        }

        GameObject Spawn(FighterVisualDef def, Transform anchor, float yaw,
            GameObject previous, int index, FighterViewBody body)
        {
            if (previous != null) Destroy(previous);
            if (def == null || def.Prefab == null) return null;

            var go = Instantiate(def.Prefab);
            go.name = def.Id;
            go.transform.SetParent(anchor, false);
            go.transform.localScale = Vector3.one * (def.ModelScale > 0f ? def.ModelScale : 1f);
            go.transform.position = new Vector3(anchor.position.x, groundY + def.ModelYOffset, anchor.position.z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var animator = go.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                if (def.Controller != null) animator.runtimeAnimatorController = def.Controller;
                var fa = animator.gameObject.AddComponent<FighterAnimator>();
                fa.Bind(driver, index, def, animator);
            }

            body.SetModel(go.transform);
            return go;
        }

        static void StripPlaceholder(Transform anchor)
        {
            if (anchor.TryGetComponent<MeshRenderer>(out var mr)) Destroy(mr);
            if (anchor.TryGetComponent<MeshFilter>(out var mf)) Destroy(mf);
            if (anchor.TryGetComponent<Collider>(out var col)) Destroy(col);
            anchor.rotation = Quaternion.identity; // models set their own facing
        }
    }
}
