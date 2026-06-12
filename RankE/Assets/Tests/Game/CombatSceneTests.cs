using NUnit.Framework;
using RankE.Game;
using RankE.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RankE.Game.Tests
{
    /// <summary>Guards the hand-wired scene: the bootstrap entry point and the
    /// objects it looks up by name must exist, or play mode silently breaks.</summary>
    public class CombatSceneTests
    {
        const string ScenePath = "Assets/Scenes/CombatScene.unity";

        [Test]
        public void CombatScene_HasBootstrapHudAndCapsules()
        {
            EditorSceneManager.OpenScene(ScenePath);

            var bootstrap = GameObject.Find("CombatBootstrap");
            Assert.IsNotNull(bootstrap, "CombatBootstrap GameObject missing from scene");
            Assert.IsNotNull(bootstrap.GetComponent<CombatBootstrap>(), "CombatBootstrap component missing");
            Assert.IsNotNull(bootstrap.GetComponent<HudRoot>(), "HudRoot component missing");

            Assert.IsNotNull(GameObject.Find("PlayerCapsule"), "PlayerCapsule missing (bootstrap finds it by name)");
            Assert.IsNotNull(GameObject.Find("EnemyCapsule"), "EnemyCapsule missing (bootstrap finds it by name)");
            Assert.IsNotNull(Object.FindAnyObjectByType<Camera>(), "scene camera missing");
        }
    }
}
