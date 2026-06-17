using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RankE.Editor
{
    /// <summary>
    /// Dresses the combat stage with a readable arena backdrop so feel-tuning happens against
    /// an arena instead of the default grey test scene. The combat camera is ORTHOGRAPHIC and
    /// side-on, so a skybox barely reads — instead we layer flat 2.5D elements: a vertical
    /// gradient sky, two colonnade (arched-wall) silhouettes for depth, a horizon contact band,
    /// a tinted ground, and a camera-space vignette. All generated (no imported art). Pure
    /// presentation: no sim/gameplay touched.
    ///
    /// Re-runnable and additive like <see cref="UiSkinBuilder"/> — materials/textures are
    /// created only if missing (hand-tuning survives) while the scene wiring is refreshed.
    /// Run with the CombatScene open: Tools ▸ RANK E ▸ Build Arena, then save the scene.
    /// </summary>
    public static class ArenaBuilder
    {
        const string OutDir = "Assets/Resources/RankE";
        const string GradientPath = OutDir + "/ArenaBackdrop.png";
        const string BackdropMatPath = OutDir + "/ArenaBackdrop.mat";
        const string GroundMatPath = OutDir + "/ArenaGround.mat";
        const string ColonnadeFarTex = OutDir + "/ArenaColonnadeFar.png";
        const string ColonnadeNearTex = OutDir + "/ArenaColonnadeNear.png";
        const string ColonnadeFarMat = OutDir + "/ArenaColonnadeFar.mat";
        const string ColonnadeNearMat = OutDir + "/ArenaColonnadeNear.mat";
        const string HorizonMatPath = OutDir + "/ArenaHorizon.mat";
        const string VignetteTex = OutDir + "/ArenaVignette.png";
        const string VignetteMatPath = OutDir + "/ArenaVignette.mat";

        // Tunable defaults (override per-asset in the Inspector after building).
        static readonly Color SkyTop = new Color(0.16f, 0.17f, 0.36f);   // deep indigo
        static readonly Color SkyHorizon = new Color(0.96f, 0.66f, 0.50f); // warm dusk
        static readonly Color GroundColor = new Color(0.26f, 0.36f, 0.28f); // muted green
        static readonly Color Ambient = new Color(0.55f, 0.55f, 0.62f);
        static readonly Color ColonnadeFarTint = new Color(0.34f, 0.30f, 0.42f); // hazy, distant
        static readonly Color ColonnadeNearTint = new Color(0.07f, 0.06f, 0.11f); // near-black
        static readonly Color HorizonBand = new Color(0.04f, 0.04f, 0.08f, 0.85f);

        [MenuItem("Tools/RANK E/Build Arena")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            var gradient = EnsureGradientTexture();
            var backdropMat = EnsureBackdropMaterial(gradient);
            var groundMat = EnsureGroundMaterial();
            var farMat = EnsureSilhouetteMaterial(ColonnadeFarMat,
                EnsureColonnadeTexture(ColonnadeFarTex, 9, 0.55f, ColonnadeFarTint), ColonnadeFarTint, 0.9f);
            var nearMat = EnsureSilhouetteMaterial(ColonnadeNearMat,
                EnsureColonnadeTexture(ColonnadeNearTex, 5, 0.82f, ColonnadeNearTint), ColonnadeNearTint, 1f);
            var horizonMat = EnsureFlatTransparentMaterial(HorizonMatPath, HorizonBand);
            var vignetteMat = EnsureSilhouetteMaterial(VignetteMatPath, EnsureVignetteTexture(), Color.white, 1f);

            // --- scene wiring (active scene) ---
            // Behind the fighters (z=0); camera at z=-10 looking +Z, centred on y≈1.5. Larger z =
            // farther. Material is double-sided so facing is moot. Ortho ⇒ z affects only draw
            // order, not size — layers are separated by scale/position, not perspective.
            var backdrop = EnsureQuad("Backdrop", new Vector3(0f, 1.5f, 9f),
                new Vector3(26f, 15f, 1f), backdropMat);
            EnsureQuad("ArenaColonnadeFar", new Vector3(0f, 4.6f, 8f),
                new Vector3(30f, 8f, 1f), farMat);
            EnsureQuad("ArenaColonnadeNear", new Vector3(0f, 3.6f, 7f),
                new Vector3(28f, 8.5f, 1f), nearMat);
            EnsureQuad("ArenaHorizon", new Vector3(0f, 0.15f, 6.5f),
                new Vector3(32f, 1.6f, 1f), horizonMat);

            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                var mr = ground.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = groundMat;
            }

            // Solid-colour clear (matches the sky top) so any gap reads clean, not default sky.
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SkyTop;
                WireVignette(cam, vignetteMat);
            }

            // Predictable flat ambient (we no longer rely on the skybox for lighting).
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[ArenaBuilder] Arena dressed (sky + colonnades + horizon + ground + vignette) "
                + $"on scene '{EditorSceneManager.GetActiveScene().name}'. Save the scene to keep it; "
                + "tune colours on the materials in Assets/Resources/RankE.");
        }

        // --- scene helpers ---

        static GameObject EnsureQuad(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = name;
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col); // no physics in this project
            }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // The vignette quad rides the camera so it always frames the view. Ortho ⇒ size it from
        // orthographicSize × aspect (current Game-view aspect; tweak the scale if you change it).
        static void WireVignette(Camera cam, Material mat)
        {
            var vg = GameObject.Find("ArenaVignette");
            if (vg == null)
            {
                vg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                vg.name = "ArenaVignette";
                var col = vg.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            vg.transform.SetParent(cam.transform, false);
            float halfH = cam.orthographic ? cam.orthographicSize : 6f;
            float aspect = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
            vg.transform.localPosition = new Vector3(0f, 0f, 1f); // just in front of the camera
            vg.transform.localRotation = Quaternion.identity;
            vg.transform.localScale = new Vector3(halfH * 2f * aspect, halfH * 2f, 1f);
            vg.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // --- generated textures (create-if-missing) ---

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
            return SavePng(tex, GradientPath);
        }

        // A colonnade silhouette: a solid wall with arched openings carved out, transparent above
        // the wall and through the arches, so the sky shows through. <paramref name="arches"/>
        // sets how many bays; <paramref name="wallTopFrac"/> how tall the wall is in the texture.
        static Texture2D EnsureColonnadeTexture(string path, int arches, float wallTopFrac, Color tint)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            int w = 1024, h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float pitch = w / (float)arches;
            float openHalf = pitch * 0.30f;       // arch opening half-width (piers fill the rest)
            float r = openHalf;                    // arch radius = opening half-width (semicircle top)
            float wallTop = h * wallTopFrac;
            float spring = wallTop - r;            // arches spring from here up to wallTop
            var clear = new Color(tint.r, tint.g, tint.b, 0f);
            var solid = new Color(tint.r, tint.g, tint.b, 1f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool filled = false;
                if (y < wallTop)
                {
                    float cx = (Mathf.Floor(x / pitch) + 0.5f) * pitch;
                    float dx = Mathf.Abs(x - cx);
                    float dy = y - spring;
                    bool inOpening = dx < openHalf &&
                        (y < spring || dx * dx + dy * dy < r * r);
                    filled = !inOpening;
                }
                tex.SetPixel(x, y, filled ? solid : clear);
            }
            return SavePng(tex, path);
        }

        // Radial vignette: transparent centre fading to dark at the corners.
        static Texture2D EnsureVignetteTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(VignetteTex);
            if (existing != null) return existing;

            int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var c = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
            float corner = c.magnitude;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x - c.x, y - c.y).magnitude / corner; // 0 centre … 1 corner
                float a = Mathf.SmoothStep(0.55f, 1f, d) * 0.55f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0.02f, a));
            }
            return SavePng(tex, VignetteTex);
        }

        static Texture2D SavePng(Texture2D tex, string path)
        {
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // --- materials (create-if-missing) ---

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
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // double-sided
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

        // Unlit, double-sided, alpha-blended — for the silhouette/vignette quads.
        static Material EnsureSilhouetteMaterial(string path, Texture2D tex, Color tint, float alpha)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(UnlitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            MakeTransparent(mat);
            var col = new Color(tint.r, tint.g, tint.b, alpha);
            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // A flat (no texture) transparent colour — the horizon contact band.
        static Material EnsureFlatTransparentMaterial(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(UnlitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            MakeTransparent(mat);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void MakeTransparent(Material mat)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);   // URP: transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);       // alpha blend
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);         // double-sided
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
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
