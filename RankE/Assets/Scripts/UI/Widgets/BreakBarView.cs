using RankE.Game;
using RankE.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace RankE.UI
{
    /// <summary>
    /// Break bars anchored under each fighter in world space (sketch: bar under
    /// each character). Fill polls Fighter.BreakBar; a white flash marks BROKEN.
    /// </summary>
    public sealed class BreakBarView : MonoBehaviour
    {
        BattleDriver driver;
        Transform[] bodies;
        readonly Image[] fills = new Image[2];
        readonly RectTransform[] roots = new RectTransform[2];
        readonly float[] flash = new float[2];

        static Color BarColor => UiSkin.Palette.BreakFill;

        public void Init(BattleDriver driver, Transform parent, Transform[] fighterBodies)
        {
            this.driver = driver;
            bodies = fighterBodies;

            for (int i = 0; i < 2; i++)
            {
                fills[i] = UiFactory.Bar($"BreakBar{i}", parent,
                    UiSkin.Palette.BarTrough, BarColor, Image.FillMethod.Horizontal, out var bg);
                roots[i] = (RectTransform)bg.transform;
                roots[i].sizeDelta = new Vector2(130f, 14f);
            }

            driver.SimEventEmitted += OnSimEvent;
        }

        void OnDestroy()
        {
            if (driver != null) driver.SimEventEmitted -= OnSimEvent;
        }

        void OnSimEvent(SimEvent ev)
        {
            if (ev.Type == SimEventType.Broken && ev.Target >= 0)
                flash[ev.Target] = 1f;
        }

        void Update()
        {
            var battle = driver != null ? driver.Battle : null;
            var cam = Camera.main;
            if (battle == null || cam == null)
            {
                for (int i = 0; i < 2; i++) roots[i].gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                roots[i].gameObject.SetActive(true);
                if (bodies != null && bodies[i] != null)
                {
                    var world = bodies[i].position + Vector3.down * 1.35f;
                    var screen = cam.WorldToScreenPoint(world);
                    screen.z = 0f;
                    roots[i].position = screen;
                }

                flash[i] = Mathf.Max(0f, flash[i] - Time.deltaTime * 2.5f);
                var f = battle.Fighters[i];
                fills[i].fillAmount = Mathf.Clamp01((float)f.BreakBar / battle.Tuning.BreakMax);
                fills[i].color = Color.Lerp(BarColor, Color.white, flash[i]);
            }
        }
    }
}
