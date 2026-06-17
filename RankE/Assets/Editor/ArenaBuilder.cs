using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RankE.Editor
{
    /// <summary>
    /// Dresses the combat stage with a simple, readable backdrop so feel-tuning happens
    /// against an arena instead of the default grey test scene. The combat camera is
    /// ORTHOGRAPHIC and side-on, so a skybox barely reads — instead we use a generated
    /// vertical-gradient quad behind the fighters plus a tinted ground, a classic 2.5D
    /// arena. Pure presentation: no sim/gameplay touched.
    ///
    /// Re-runnable and additive like <see cref="UiSkinBuilder"/> — materials/texture are
    /// created only if missing (hand-tuning survives) while the scene wiring is refreshed.
    /// Run with the CombatScene open: Tools ▸ RANK E ▸ Build Arena, then save the scene.
    /// </summary>
    public static class ArenaBuilder
    {
        const string OutDir = "Assets/Resources/RankE";
        const string GradientPath = OutDir + "/ArenaBackdrop.png";
        const string BackdropMatPath = OutDir + "/ArenaBackdrop.mat";
        const string GroundMatPath = OutDir + "/ArenaGround.mat";

        // Tunable defaults (override per-asset in the Inspector after building).
        static readonly Color SkyTop = new Color(0.18f, 0.20f, 0.42f);   // indigo
        static readonly Color SkyHorizon = new Color(0.96f, 0.70f, 0.55f); // warm dusk
        static readonly Color GroundColor = new Color(0.30f, 0.42f, 0.32f); // muted green
        static readonly Color Ambient = new Color(0.55f, 0.55f, 0.62f);

        [MenuItem("Tools/RANK E/Build Arena")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            var gradient = EnsureGradientTexture();
            var backdropMat = EnsureBackdropMaterial(gradient);
            var groundMat = EnsureGroundMaterial();

            // --- scene wiring (active scene) ---
            var backdrop = GameObject.Find("Backdrop");
            if (backdrop == null)
            {
                backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = "Backdrop";
                var col = backdrop.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col); // no physics in this project
            }
            // Behind the fighters (z=0), tall/wide enough to fill any aspect; camera is at
            // z=-10 looking +Z, centred on y=1.5. Material is double-sided so facing is moot.
            backdrop.transform.position = new Vector3(0f, 1.5f, 8f);
            backdrop.transform.rotation = Quaternion.identity;
            backdrop.transform.localScale = new Vector3(24f, 14f, 1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;

            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                var mr = ground.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = groundMat;
            }

            // Solid-colour clear (matches the horizon) so any gap reads clean, not default sky.
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SkyHorizon;
            }

            // Predictable flat ambient (we no longer rely on the skybox for lighting).
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[ArenaBuilder] Arena dressed (backdrop + ground + ambient) on scene "
                + $"'{EditorSceneManager.GetActiveScene().name}'. Save the scene to keep it; "
                + "tune colours on the materials in the Inspector.");
        }

        // --- assets (create-if-missing) ---

        static Texture2D EnsureGradientTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientPath);
            if (existing != null) return existing;

            const int h = 256;
            var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                float t = Mathf.SmoothStep(0f, 1f, y / (float)(h - 1)); // 0 = bottom (horizon)
                var c = Color.Lerp(SkyHorizon, SkyTop, t);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(GradientPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(GradientPath);

            var importer = AssetImporter.GetAtPath(GradientPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GradientPath);
        }

        static Material EnsureBackdropMaterial(Texture2D gradient)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(BackdropMatPath);
            if (mat == null)
            {
                mat = new Material(UnlitShader());
                AssetDatabase.CreateAsset(mat, BackdropMatPath);
            }
            mat.mainTexture = gradient;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", gradient);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // double-sided — facing-proof
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material EnsureGroundMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GroundMatPath);
            if (mat == null)
            {
                mat = new Material(LitShader());
                AssetDatabase.CreateAsset(mat, GroundMatPath);
                mat.color = GroundColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", GroundColor);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        static Shader UnlitShader() =>
            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");

        static Shader LitShader() =>
            Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
