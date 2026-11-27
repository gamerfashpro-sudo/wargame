using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using WG.World;

namespace WG.EditorTools
{
    /// <summary>
    /// Настройка проекта WARGAME в редакторе: URP, качество, PlayerSettings, материалы, TMP, сцены, Build Settings, реестр моделей.
    /// Запускается сам при первом открытии (если чего-то не хватает) и вручную: WARGAME/Setup Project. Все шаги идемпотентны.
    /// </summary>
    [InitializeOnLoad]
    public static class WargameProjectSetup
    {
        const string SettingsDir = "Assets/_Project/Settings";
        const string MaterialsDir = "Assets/_Project/Art/Materials";
        const string ResMaterialsDir = "Assets/_Project/Resources/Materials";
        const string ModelsDir = "Assets/_Project/Art/Models";
        const string RegistryPath = "Assets/_Project/Resources/ModelRegistry.asset";
        const string RendererPath = SettingsDir + "/WG_Renderer.asset";
        public const string MenuScenePath = "Assets/_Project/Scenes/MainMenu/MainMenu.unity";
        public const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        // Палитра = BlenderAssets/Scripts/wg_pipeline.py PALETTE (линейные цвета, шероховатость, металличность)
        static readonly (string name, Color linear, float rough, float metal)[] Palette =
        {
            ("WG_Olive", new Color(0.155f, 0.170f, 0.085f), 0.70f, 0f),
            ("WG_OliveDark", new Color(0.075f, 0.085f, 0.045f), 0.75f, 0f),
            ("WG_Sand", new Color(0.430f, 0.360f, 0.220f), 0.80f, 0f),
            ("WG_Graphite", new Color(0.030f, 0.032f, 0.036f), 0.55f, 0.6f),
            ("WG_Steel", new Color(0.300f, 0.300f, 0.310f), 0.40f, 1f),
            ("WG_Rubber", new Color(0.018f, 0.018f, 0.018f), 0.90f, 0f),
            ("WG_Glass", new Color(0.020f, 0.040f, 0.050f), 0.08f, 0f),
            ("WG_Concrete", new Color(0.380f, 0.370f, 0.350f), 0.90f, 0f),
            ("WG_Brick", new Color(0.300f, 0.110f, 0.070f), 0.85f, 0f),
            ("WG_RoofDark", new Color(0.050f, 0.055f, 0.060f), 0.70f, 0.2f),
            ("WG_Accent", new Color(0.900f, 0.480f, 0.040f), 0.50f, 0f),
            ("WG_TeamColor", new Color(0.800f, 0.800f, 0.800f), 0.60f, 0f),
            ("WG_Foliage", new Color(0.040f, 0.090f, 0.025f), 0.85f, 0f),
            ("WG_Bark", new Color(0.090f, 0.060f, 0.035f), 0.90f, 0f),
        };

        static WargameProjectSetup()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (NeedsSetup())
            {
                Debug.Log("[WARGAME] Первичная настройка проекта…");
                Run();
            }
        }

        static bool NeedsSetup() =>
            !File.Exists(MenuScenePath) || !File.Exists(GameplayScenePath) || !File.Exists(RendererPath) ||
            !File.Exists(RegistryPath) || GraphicsSettings.defaultRenderPipeline == null ||
            !File.Exists(ResMaterialsDir + "/Lit_Base.mat");

        [MenuItem("WARGAME/Setup Project", priority = 0)]
        public static void Run()
        {
            Step("Папки", EnsureFolders);
            Step("URP и уровни качества", SetupRenderPipeline);
            Step("Player Settings", SetupPlayer);
            Step("Материалы палитры", SetupPaletteMaterials);
            Step("Материалы Resources", SetupResourceMaterials);
            Step("TextMeshPro", SetupTMP);
            Step("Сцены и Build Settings", SetupScenes);
            Step("Реестр моделей", RefreshModelRegistry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WARGAME] Настройка проекта завершена. Откройте сцену MainMenu и нажмите Play.");
        }

        static void Step(string name, Action action)
        {
            try { action(); Debug.Log($"[WARGAME] ✔ {name}"); }
            catch (Exception e) { Debug.LogError($"[WARGAME] ✘ {name}: {e}"); }
        }

        // ---------------------------------------------------------------------------------------------
        static void EnsureFolders()
        {
            foreach (var f in new[] { SettingsDir, MaterialsDir, ResMaterialsDir, "Assets/_Project/Scenes/MainMenu", "Assets/_Project/Scenes/Gameplay" })
                EnsureFolder(f);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------------------------------------------------------------------------------------------
        static void SetupRenderPipeline()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            if (renderer.postProcessData == null)
            {
                var guid = AssetDatabase.FindAssets("t:PostProcessData", new[] { "Packages/com.unity.render-pipelines.universal" }).FirstOrDefault();
                if (guid != null) renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(AssetDatabase.GUIDToAssetPath(guid));
                EditorUtility.SetDirty(renderer);
            }

            // name, shadowDistance, shadowRes, msaa, cascades
            var tiers = new (string name, float shadow, int res, int msaa, int cascades)[]
            {
                ("Low", 150f, 1024, 1, 1), ("Medium", 300f, 2048, 2, 2), ("High", 500f, 2048, 4, 4), ("Ultra", 800f, 4096, 4, 4)
            };
            var assets = new List<UniversalRenderPipelineAsset>();
            foreach (var t in tiers)
            {
                string path = $"{SettingsDir}/WG_URP_{t.name}.asset";
                var a = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (a == null)
                {
                    a = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(a, path);
                }
                a.shadowDistance = t.shadow;
                a.msaaSampleCount = t.msaa;
                a.shadowCascadeCount = t.cascades;
                a.supportsHDR = true;
                a.renderScale = 1f;
                var so = new SerializedObject(a);
                SetInt(so, "m_MainLightShadowmapResolution", t.res);
                SetInt(so, "m_AdditionalLightsShadowmapResolution", Math.Min(t.res, 2048));
                SetBool(so, "m_SoftShadowsSupported", t.name != "Low");
                SetBool(so, "m_SupportsTerrainHoles", false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
                assets.Add(a);
            }

            GraphicsSettings.defaultRenderPipeline = assets[2];

            // распределить ассеты по существующим уровням качества
            var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (qs == null) return;
            var qso = new SerializedObject(qs);
            var levels = qso.FindProperty("m_QualitySettings");
            if (levels == null || !levels.isArray) return;
            int n = levels.arraySize;
            for (int i = 0; i < n; i++)
            {
                int tier = n <= 1 ? 2 : Mathf.Clamp(Mathf.RoundToInt(i * 3f / (n - 1)), 0, 3);
                var rp = levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");
                if (rp != null) rp.objectReferenceValue = assets[tier];
            }
            qso.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.intValue = v; }
        static void SetBool(SerializedObject so, string prop, bool v) { var p = so.FindProperty(prop); if (p != null) p.boolValue = v; }

        // ---------------------------------------------------------------------------------------------
        static void SetupPlayer()
        {
            PlayerSettings.companyName = "WARGAME";
            PlayerSettings.productName = "WARGAME";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.bundleVersion = WG.Core.GameBootstrap.Version;
        }

        // ---------------------------------------------------------------------------------------------
        static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit");

        static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader && shader != null) m.shader = shader;
            return m;
        }

        static void SetupPaletteMaterials()
        {
            var lit = LitShader;
            if (lit == null) throw new Exception("Шейдер URP Lit не найден — URP не установлен?");
            foreach (var (name, linear, rough, metal) in Palette)
            {
                var m = GetOrCreateMaterial($"{MaterialsDir}/{name}.mat", lit);
                m.SetColor("_BaseColor", linear.gamma);
                m.SetFloat("_Smoothness", 1f - rough);
                m.SetFloat("_Metallic", metal);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            var camo = GetOrCreateMaterial($"{MaterialsDir}/WG_Camo_Woodland.mat", lit);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Shared/T_Camo_Woodland_A.png");
            if (tex != null) camo.SetTexture("_BaseMap", tex);
            camo.SetColor("_BaseColor", Color.white);
            camo.SetFloat("_Smoothness", 0.28f);
            camo.enableInstancing = true;
            EditorUtility.SetDirty(camo);
        }

        static void SetupResourceMaterials()
        {
            var lit = LitShader;
            var baseMat = GetOrCreateMaterial($"{ResMaterialsDir}/Lit_Base.mat", lit);
            baseMat.SetColor("_BaseColor", Color.white);
            baseMat.SetFloat("_Smoothness", 0.25f);
            baseMat.enableInstancing = true;

            var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (terrainShader != null) GetOrCreateMaterial($"{ResMaterialsDir}/Terrain_Lit.mat", terrainShader);

            var water = GetOrCreateMaterial($"{ResMaterialsDir}/Water.mat", lit);
            MakeTransparent(water, new Color(0.06f, 0.22f, 0.32f, 0.82f));
            water.SetFloat("_Smoothness", 0.95f);
            water.SetFloat("_Metallic", 0.1f);

            MakeTransparent(GetOrCreateMaterial($"{ResMaterialsDir}/Ghost_Valid.mat", lit), new Color(0.3f, 0.95f, 0.45f, 0.45f));
            MakeTransparent(GetOrCreateMaterial($"{ResMaterialsDir}/Ghost_Invalid.mat", lit), new Color(0.95f, 0.3f, 0.25f, 0.45f));

            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                var unlit = GetOrCreateMaterial($"{ResMaterialsDir}/Unlit_Color.mat", unlitShader);
                unlit.SetColor("_BaseColor", Color.white);
            }

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = GetOrCreateMaterial($"{ResMaterialsDir}/Sky.mat", skyShader);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 1.05f);
                sky.SetFloat("_Exposure", 1.25f);
                sky.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.55f));
                sky.SetColor("_GroundColor", new Color(0.32f, 0.33f, 0.34f));
                EditorUtility.SetDirty(sky);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { ResMaterialsDir }))
                EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        /// <summary>URP Lit → прозрачный (Alpha blend). Через BaseShaderGUI.SetMaterialKeywords, если доступен.</summary>
        static void MakeTransparent(Material m, Color c)
        {
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            var gui = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.BaseShaderGUI", false)).FirstOrDefault(t => t != null);
            var method = gui?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(mi => mi.Name == "SetMaterialKeywords" && mi.GetParameters().Length >= 1);
            if (method != null)
            {
                var args = new object[method.GetParameters().Length];
                args[0] = m;
                try { method.Invoke(null, args); } catch (Exception e) { Debug.LogWarning($"[WARGAME] SetMaterialKeywords: {e.Message}"); }
            }
            EditorUtility.SetDirty(m);
        }

        // ---------------------------------------------------------------------------------------------
        static void SetupTMP()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            var importer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("TMPro.TMP_PackageResourceImporter", false)).FirstOrDefault(t => t != null);
            var method = importer?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
            if (method != null)
            {
                method.Invoke(null, new object[] { true, false, false });
                Debug.Log("[WARGAME] Импортируются TMP Essential Resources.");
            }
            else if (!EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources"))
                Debug.LogWarning("[WARGAME] Импортируйте вручную: Window > TextMeshPro > Import TMP Essential Resources.");
        }

        // ---------------------------------------------------------------------------------------------
        static void SetupScenes()
        {
            bool created = false;
            foreach (var path in new[] { MenuScenePath, GameplayScenePath })
            {
                if (File.Exists(path)) continue;
                var active = SceneManager.GetActiveScene();
                bool untitled = string.IsNullOrEmpty(active.path);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, untitled ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, path);
                if (!untitled) EditorSceneManager.CloseScene(scene, true);
                created = true;
            }
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
            };
            if (created || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        }

        // ---------------------------------------------------------------------------------------------
        [MenuItem("WARGAME/Refresh Model Registry", priority = 1)]
        public static void RefreshModelRegistry()
        {
            EnsureFolder("Assets/_Project/Resources");
            var reg = AssetDatabase.LoadAssetAtPath<ModelRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ModelRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }
            reg.entries.Clear();
            if (AssetDatabase.IsValidFolder(ModelsDir))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsDir }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;
                    var rel = Path.GetDirectoryName(path).Replace('\\', '/').Substring(ModelsDir.Length).Trim('/');
                    reg.entries.Add(new ModelRegistry.Entry { id = Path.GetFileNameWithoutExtension(path), category = rel, model = go });

                    // материалы WG_* из FBX → проектные .mat
                    // только один раз (иначе SaveAndReimport → постпроцессор → снова сюда)
                    if (AssetImporter.GetAtPath(path) is ModelImporter mi && mi.GetExternalObjectMap().Count == 0 &&
                        mi.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere))
                        mi.SaveAndReimport();
                }
            }
            EditorUtility.SetDirty(reg);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WARGAME] Реестр моделей: {reg.entries.Count} шт.");
        }
    }
}
