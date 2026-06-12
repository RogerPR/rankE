using System;
using System.Collections.Generic;
using RankE.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RankE.Game
{
    /// <summary>
    /// Owns the combat action maps and routes button presses into the player's
    /// intent buffer. Slot order mirrors the loadout: abilities 1–4 are the main
    /// bar, Parry/Kick are the two quick slots (loadout indices 4 and 5).
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        public PlayerIntentBuffer Buffer { get; } = new PlayerIntentBuffer();

        /// <summary>Fired on Start/Esc; MatchController decides what pause means.</summary>
        public event Action PausePressed;

        InputActionMap combat;
        InputActionMap meta;
        readonly string[] slotIds = new string[6];

        static readonly string[] SlotActions =
        {
            CombatInputActions.Ability1, CombatInputActions.Ability2,
            CombatInputActions.Ability3, CombatInputActions.Ability4,
            CombatInputActions.Parry, CombatInputActions.Kick,
        };

        /// <summary>Human-readable key hints per slot, for the ability bar UI.</summary>
        public static readonly string[] SlotKeyHints = { "Q", "W", "E", "R", "SPC", "F" };

        void Awake()
        {
            combat = CombatInputActions.CreateCombatMap();
            meta = CombatInputActions.CreateMetaMap();

            for (int i = 0; i < SlotActions.Length; i++)
            {
                int slot = i;
                combat[SlotActions[i]].performed += _ => OnSlotPressed(slot);
            }
            meta[CombatInputActions.Pause].performed += _ => PausePressed?.Invoke();
        }

        void OnDestroy()
        {
            combat?.Dispose();
            meta?.Dispose();
        }

        /// <summary>Map the player's loadout (4 abilities + parry + kick) onto the slots.</summary>
        public void SetLoadout(IReadOnlyList<AbilityDef> abilities)
        {
            for (int i = 0; i < slotIds.Length; i++)
                slotIds[i] = i < abilities.Count ? abilities[i].Id : null;
        }

        public void SetCombatEnabled(bool on)
        {
            if (on) combat.Enable();
            else combat.Disable();
            if (!on) Buffer.Clear();
        }

        public void SetMetaEnabled(bool on)
        {
            if (on) meta.Enable();
            else meta.Disable();
        }

        void OnSlotPressed(int slot)
        {
            var id = slotIds[slot];
            if (id != null) Buffer.Press(id);
        }
    }
}
