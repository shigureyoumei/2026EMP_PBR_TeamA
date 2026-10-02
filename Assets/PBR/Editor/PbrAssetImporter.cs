using UnityEditor;
using UnityEngine;

namespace PBR.Editor
{
    public sealed class PbrAssetImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/QuaterniusAnimatedFish/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            if (!assetPath.StartsWith("Assets/ThirdParty/QuaterniusAnimatedFish/")) return;
            clip.wrapMode = WrapMode.Loop;
        }
    }
}
