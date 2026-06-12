using System.Text;
using RankE.Game;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Enemy intent display (sketch: upcoming-action queue top-right): the
    /// telegraphed next action with a commit countdown bar, plus any pending
    /// delayed abilities (e.g. Falling Star) from either fighter.
    /// </summary>
    public sealed class EnemyIntentView : MonoBehaviour
    {
        BattleDriver driver;
        GameObject intentRoot;
        Text intentLabel;
        Image intentFill;
        Text delayedLabel;
        readonly StringBuilder sb = new StringBuilder();

        public void Init(BattleDriver driver, Transform parent)
        {
            this.driver = driver;

            intentFill = UiFactory.Bar("EnemyIntent", parent,
                new Color(0f, 0f, 0f, 0.6f), new Color(0.9f, 0.35f, 0.2f),
                Image.FillMethod.Horizontal, out var bg);
            intentRoot = bg.gameObject;
            UiFactory.PlaceFixed((RectTransform)bg.transform, new Vector2(1f, 1f),
                new Vector2(-70f, -156f), new Vector2(380f, 26f));

            intentLabel = UiFactory.Label("Label", bg.transform, "", 20, Color.white);
            UiFactory.PlaceStretch((RectTransform)intentLabel.transform);

            delayedLabel = UiFactory.Label("Delayed", parent, "", 20,
                new Color(1f, 0.8f, 0.4f), TextAnchor.UpperRight);
            UiFactory.PlaceFixed((RectTransform)delayedLabel.transform, new Vector2(1f, 1f),
                new Vector2(-70f, -190f), new Vector2(380f, 80f));
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var enemyAi = driver != null ? driver.EnemyBehavior : null;

            string pending = enemyAi != null ? enemyAi.PendingIntent : null;
            if (battle != null && pending != null)
            {
                intentRoot.SetActive(true);
                string name = battle.Content.Abilities.TryGetValue(pending, out var def)
                    ? def.Name : pending;
                intentLabel.text = $"Incoming: {name}";
                intentFill.fillAmount = driver.EnemyTelegraphTicksTotal > 0
                    ? 1f - Mathf.Clamp01((float)enemyAi.TicksUntilCommit / driver.EnemyTelegraphTicksTotal)
                    : 1f;
            }
            else
            {
                intentRoot.SetActive(false);
            }

            // Delayed abilities in flight (both fighters — they hit everyone's plans).
            if (battle == null || battle.Pending.Count == 0)
            {
                delayedLabel.text = "";
                return;
            }
            sb.Length = 0;
            foreach (var p in battle.Pending)
            {
                float secs = (p.FireTick - battle.CurrentTick) * RankE.Sim.SimConstants.TickDuration;
                sb.AppendLine($"{battle.Fighters[p.Source].Name}: {p.Ability.Name} in {Mathf.Max(0f, secs):0.0}s");
            }
            delayedLabel.text = sb.ToString();
        }
    }
}
