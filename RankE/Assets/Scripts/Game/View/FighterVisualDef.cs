using System;
using System.Collections.Generic;
using UnityEngine;

namespace RankE.Game
{
    /// <summary>
    /// Semantic animation verbs the view speaks. Each fighter visual maps these to
    /// whatever Animator state names its own controller happens to use, so the
    /// sim→anim layer never hardcodes a clip name. This is a presentation vocabulary
    /// (not game content), so a closed enum is fine.
    /// </summary>
    public enum AnimAction
    {
        Idle,
        Attack,
        Cast,
        Hit,
        Block,
        Broken,
        Riposte,
        Die,
        Spawn,
        Victory,

        /// <summary>Anticipation/charge pose held during an ability's pre-effect wind-up, so the
        /// visible strike lands on the effect tick rather than on the button press.</summary>
        Windup,

        /// <summary>Sustained "stunned" pose held for the whole duration of a stun/broken
        /// status (clearly reads as helpless). Falls back to Broken→Hit when unmapped.</summary>
        Stunned,
    }

    [Serializable]
    public struct ActionState
    {
        public AnimAction Action;
        public string State;
    }

    /// <summary>Per-ability animation override (ability id → Animator state name).</summary>
    [Serializable]
    public struct AbilityAnim
    {
        public string AbilityId;
        public string State;
    }

    /// <summary>
    /// Presentation-only description of one fighter visual: which prefab to spawn,
    /// which Animator controller drives it (null = keep the prefab's own), and how
    /// semantic actions / ability ids map to that controller's state names.
    /// Built by the editor <c>ArtSetupBuilder</c>; consumed at runtime by
    /// <c>FighterStage</c> + <c>FighterAnimator</c>. Sim never sees this.
    /// </summary>
    [Serializable]
    public sealed class FighterVisualDef
    {
        public string Id;
        public string DisplayName;
        public GameObject Prefab;

        /// <summary>Shared controller to assign (player); null = use the prefab's own (monsters).</summary>
        public RuntimeAnimatorController Controller;

        public bool IsHumanoid;

        /// <summary>Uniform scale + Y offset to seat the model on the ground at the anchor.</summary>
        public float ModelScale = 1f;
        public float ModelYOffset;

        public List<ActionState> Actions = new List<ActionState>();
        public List<AbilityAnim> AbilityStates = new List<AbilityAnim>();

        /// <summary>Animator state name for a semantic action, or <paramref name="fallback"/>.</summary>
        public string StateFor(AnimAction action, string fallback = null)
        {
            for (int i = 0; i < Actions.Count; i++)
                if (Actions[i].Action == action && !string.IsNullOrEmpty(Actions[i].State))
                    return Actions[i].State;
            return fallback;
        }

        /// <summary>Per-ability state override, or null if the ability has none.</summary>
        public string StateForAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return null;
            for (int i = 0; i < AbilityStates.Count; i++)
                if (AbilityStates[i].AbilityId == abilityId && !string.IsNullOrEmpty(AbilityStates[i].State))
                    return AbilityStates[i].State;
            return null;
        }
    }
}
