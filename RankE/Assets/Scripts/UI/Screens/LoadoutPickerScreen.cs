using RankE.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Debug loadout picker (Phase 2 checkbox): cycle stance/weapon/armor and the
    /// four main ability slots, then start the fight. Placeholder quality — real
    /// acquisition arrives with run rewards in Phase 4.
    /// </summary>
    public sealed class LoadoutPickerScreen : MonoBehaviour
    {
        MatchController match;
        CharacterCreatorScreen creator;
        GameObject root;
        Button startButton;
        readonly Text[] values = new Text[9];

        public void SetCreator(CharacterCreatorScreen creator) => this.creator = creator;

        public void Init(MatchController match, Transform parent)
        {
            this.match = match;

            var panel = UiFactory.Panel("LoadoutPicker", parent, new Color(0.06f, 0.06f, 0.1f, 0.98f));
            UiFactory.PlaceStretch((RectTransform)panel.transform);
            root = panel.gameObject;

            var title = UiFactory.Label("Title", panel.transform, "DEBUG LOADOUT", 52, Color.white);
            UiFactory.PlaceFixed((RectTransform)title.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -60f), new Vector2(700f, 60f));

            string[] rowNames =
            {
                "Character", "Monster", "Stance", "Weapon", "Armor",
                "Ability 1", "Ability 2", "Ability 3", "Ability 4",
            };
            for (int i = 0; i < rowNames.Length; i++)
            {
                int row = i;
                float y = -140f - i * 66f;

                var label = UiFactory.Label($"RowName{i}", panel.transform, rowNames[i], 28,
                    new Color(0.8f, 0.8f, 0.9f), TextAnchor.MiddleRight);
                UiFactory.PlaceFixed((RectTransform)label.transform, new Vector2(0.5f, 1f),
                    new Vector2(-330f, y), new Vector2(260f, 40f));

                var prev = UiFactory.TextButton($"Prev{i}", panel.transform, "<", 30, () => Cycle(row, -1));
                UiFactory.PlaceFixed((RectTransform)prev.transform, new Vector2(0.5f, 1f),
                    new Vector2(-150f, y), new Vector2(60f, 56f));

                values[i] = UiFactory.Label($"Value{i}", panel.transform, "", 30, Color.white);
                UiFactory.PlaceFixed((RectTransform)values[i].transform, new Vector2(0.5f, 1f),
                    new Vector2(40f, y), new Vector2(300f, 40f));

                var next = UiFactory.TextButton($"Next{i}", panel.transform, ">", 30, () => Cycle(row, +1));
                UiFactory.PlaceFixed((RectTransform)next.transform, new Vector2(0.5f, 1f),
                    new Vector2(230f, y), new Vector2(60f, 56f));
            }

            var hint = UiFactory.Label("QuickHint", panel.transform,
                "Quick slots are fixed: Parry (SPACE / RB) and Kick (F / LB)", 22,
                new Color(0.7f, 0.7f, 0.8f));
            UiFactory.PlaceFixed((RectTransform)hint.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -760f), new Vector2(900f, 30f));

            var customize = UiFactory.TextButton("Customize", panel.transform, "CUSTOMIZE CHARACTER", 26,
                OpenCreator);
            UiFactory.PlaceFixed((RectTransform)customize.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -800f), new Vector2(360f, 54f));

            startButton = UiFactory.TextButton("Start", panel.transform, "START FIGHT", 34,
                () => match.StartMatch());
            UiFactory.PlaceFixed((RectTransform)startButton.transform, new Vector2(0.5f, 1f),
                new Vector2(0f, -866f), new Vector2(360f, 70f));
        }

        void OpenCreator()
        {
            if (creator == null) return;
            root.SetActive(false);
            creator.Open(() => Show(true));
        }

        void Cycle(int row, int dir)
        {
            switch (row)
            {
                case 0:
                    match.Loadout.UseCustomAppearance = false; // cycling a sample leaves custom mode
                    match.Loadout.CyclePlayerVisual(dir);
                    break;
                case 1: match.Loadout.CycleEnemyVisual(dir); break;
                case 2: match.Loadout.CycleStance(dir); break;
                case 3: match.Loadout.CycleWeapon(dir); break;
                case 4: match.Loadout.CycleArmor(dir); break;
                default: match.Loadout.CycleAbility(row - 5, dir); break;
            }
            Refresh();
        }

        void Refresh()
        {
            var l = match.Loadout;
            values[0].text = l.UseCustomAppearance ? "(custom)" : l.PlayerVisualName;
            values[1].text = l.EnemyVisualName;
            values[2].text = l.StanceName;
            values[3].text = l.WeaponName;
            values[4].text = l.ArmorName;
            for (int s = 0; s < 4; s++)
                values[5 + s].text = l.AbilityName(s);
        }

        public void Show(bool on)
        {
            root.SetActive(on);
            if (!on) return;
            Refresh();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
    }
}
