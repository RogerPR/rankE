using System.Collections.Generic;
using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Event-driven floating combat text over the fighters (as in the PoC):
    /// damage/heal numbers, ability names, PARRY!/WHIFF/BROKEN!/RIPOSTE!/COMBO!.
    /// Entries are pooled; each rises and fades out.
    /// </summary>
    public sealed class FloatingCombatTextSpawner : MonoBehaviour
    {
        sealed class Entry
        {
            public Text Text;
            public float Age;
            public float Lifetime;
            public Vector2 Velocity;
        }

        BattleDriver driver;
        Transform[] bodies;
        Transform parent;
        readonly List<Entry> active = new List<Entry>();
        readonly Stack<Text> pool = new Stack<Text>();

        public void Init(BattleDriver driver, Transform parent, Transform[] fighterBodies)
        {
            this.driver = driver;
            this.parent = parent;
            bodies = fighterBodies;
            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            var battle = driver.Battle;
            switch (ev.Type)
            {
                case SimEventType.Damaged:
                    Spawn(ev.Target, $"-{ev.Amount}", new Color(1f, 0.35f, 0.3f),
                        ev.StatusId != null ? 22 : 30);
                    break;
                case SimEventType.Healed:
                    if (ev.Amount > 0)
                        Spawn(ev.Target, $"+{ev.Amount}", new Color(0.35f, 1f, 0.4f),
                            ev.StatusId != null ? 22 : 30);
                    break;
                case SimEventType.AbilityUsed:
                    if (battle != null && battle.Content.Abilities.TryGetValue(ev.AbilityId, out var def)
                        && def.Id != PocContent.AutoAttackId)
                        Spawn(ev.Actor, def.Name, new Color(1f, 1f, 1f, 0.85f), 22);
                    break;
                case SimEventType.Parried:
                    Spawn(ev.Actor, "PARRY!", Color.white, 40);
                    break;
                case SimEventType.AbilityWhiffed:
                    Spawn(ev.Actor, "WHIFF", new Color(0.7f, 0.7f, 0.7f), 24);
                    break;
                case SimEventType.Broken:
                    Spawn(ev.Target, "BROKEN!", new Color(1f, 0.55f, 0.1f), 44);
                    break;
                case SimEventType.RiposteTriggered:
                    Spawn(ev.Actor, "RIPOSTE!", new Color(1f, 0.95f, 0.3f), 40);
                    break;
                case SimEventType.ComboCompleted:
                    Spawn(ev.Actor, "COMBO!", new Color(0.85f, 0.5f, 1f), 36);
                    break;
                case SimEventType.CastInterrupted:
                    Spawn(ev.Target, "Interrupted", new Color(1f, 0.5f, 0.4f), 24);
                    break;
            }
        }

        void Spawn(int fighterIndex, string message, Color color, int size)
        {
            if (fighterIndex < 0 || bodies == null || bodies[fighterIndex] == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var text = pool.Count > 0 ? pool.Pop()
                : UiFactory.Label("Float", parent, "", 30, Color.white);
            text.gameObject.SetActive(true);
            text.text = message;
            text.color = color;
            text.fontSize = size;
            var rt = (RectTransform)text.transform;
            rt.sizeDelta = new Vector2(300f, 40f);

            var world = bodies[fighterIndex].position + Vector3.up * 1.4f;
            var screen = cam.WorldToScreenPoint(world);
            screen.z = 0f;
            rt.position = screen + new Vector3(Random.Range(-30f, 30f), Random.Range(0f, 20f));

            active.Add(new Entry
            {
                Text = text,
                Age = 0f,
                Lifetime = 0.9f,
                Velocity = new Vector2(Random.Range(-12f, 12f), 90f),
            });
        }

        void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                e.Age += Time.deltaTime;
                if (e.Age >= e.Lifetime)
                {
                    e.Text.gameObject.SetActive(false);
                    pool.Push(e.Text);
                    active.RemoveAt(i);
                    continue;
                }
                e.Text.transform.position += (Vector3)(e.Velocity * Time.deltaTime);
                var c = e.Text.color;
                c.a = 1f - Mathf.SmoothStep(0f, 1f, e.Age / e.Lifetime);
                e.Text.color = c;
            }
        }

        /// <summary>Clear leftovers when a new fight starts.</summary>
        public void ClearAll()
        {
            foreach (var e in active)
            {
                e.Text.gameObject.SetActive(false);
                pool.Push(e.Text);
            }
            active.Clear();
        }
    }
}
