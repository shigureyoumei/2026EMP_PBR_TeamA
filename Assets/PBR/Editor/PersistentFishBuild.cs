using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBR.Editor
{
    public static class PersistentFishBuild
    {
        public static void Run()
        {
            try
            {
                const string scene = "Assets/PBR/Scenes/FishAttentionArcade.unity";
                EditorSceneManager.OpenScene(scene);
                PbrArcadeBuilder.Validate();
                var session = UnityEngine.Object.FindAnyObjectByType<FishExperiment>();
                if (!session.config.persistentSchool || session.config.fishCount != 10) throw new Exception("Persistent school configuration missing");
                var result = BuildPipeline.BuildPlayer(new[] { scene }, "BuildsPersistent/PBRReef.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
                if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
