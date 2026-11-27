using UnityEditor;
using UnityEngine;

namespace WG.EditorTools
{
    /// <summary>Единые настройки импорта FBX из Assets/_Project/Art/Models (см. MODELING_GUIDELINES.md, раздел 7).</summary>
    public class ModelImportSettings : AssetPostprocessor
    {
        const string ModelsRoot = "Assets/_Project/Art/Models";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsRoot)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;              // Convert Units
            mi.bakeAxisConversion = true;        // обязательно: убирает поворот −90° корня
            // природу сливаем в меши для GPU instancing — нужен доступ к вершинам
            mi.isReadable = assetPath.Contains("/Environment/");
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.addCollider = false;
            mi.meshCompression = assetPath.Contains("/Military/") ? ModelImporterMeshCompression.Low : ModelImporterMeshCompression.Medium;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;
            mi.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
                if (p.StartsWith(ModelsRoot) && p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    EditorApplication.delayCall -= WargameProjectSetup.RefreshModelRegistry;
                    EditorApplication.delayCall += WargameProjectSetup.RefreshModelRegistry;
                    return;
                }
        }
    }
}
