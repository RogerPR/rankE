using UnityEngine.InputSystem;

namespace RankE.Game
{
    /// <summary>
    /// Combat action maps built in code (GAME_DESIGN §2), kept out of .inputactions
    /// assets so the bindings live in git as reviewable C#. Binding overrides keep
    /// them rebindable for Phase 7. Potion/item actions are deliberately absent —
    /// there is no item mechanic in the sim until Phase 4.
    /// </summary>
    public static class CombatInputActions
    {
        // "Combat" map: enabled only while actually fighting (the south button would
        // otherwise collide with UI Submit on menus).
        public const string Ability1 = "Ability1";
        public const string Ability2 = "Ability2";
        public const string Ability3 = "Ability3";
        public const string Ability4 = "Ability4";
        public const string Parry = "Parry";
        public const string Kick = "Kick";

        // "Meta" map: enabled while fighting or paused.
        public const string Pause = "Pause";

        public static InputActionMap CreateCombatMap()
        {
            var map = new InputActionMap("Combat");
            AddButton(map, Ability1, "<Gamepad>/buttonNorth", "<Keyboard>/q");
            AddButton(map, Ability2, "<Gamepad>/buttonWest", "<Keyboard>/w");
            AddButton(map, Ability3, "<Gamepad>/buttonEast", "<Keyboard>/e");
            AddButton(map, Ability4, "<Gamepad>/buttonSouth", "<Keyboard>/r");
            AddButton(map, Parry, "<Gamepad>/rightShoulder", "<Keyboard>/space");
            AddButton(map, Kick, "<Gamepad>/leftShoulder", "<Keyboard>/f");
            return map;
        }

        public static InputActionMap CreateMetaMap()
        {
            var map = new InputActionMap("Meta");
            AddButton(map, Pause, "<Gamepad>/start", "<Keyboard>/escape");
            return map;
        }

        static void AddButton(InputActionMap map, string name, params string[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button);
            foreach (var b in bindings)
                action.AddBinding(b);
        }
    }
}
