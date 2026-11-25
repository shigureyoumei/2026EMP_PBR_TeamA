using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PBR.Editor
{
    public static class LargeSpaceBuild
    {
        const string PlayerPath = "BuildsLargeSpace/PBRLargeSpace.exe";

        [MenuItem("PBR/Build Large Space player")]
        public static void BuildPlayer() => BuildPlayerAt(PlayerPath);

        static void BuildPlayerAt(string playerPath)
        {
            var report = BuildPipeline.BuildPlayer(new[] { RingReefBuilder.ScenePath }, playerPath,
                BuildTarget.StandaloneWindows64, BuildOptions.StrictMode);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Large Space build ended with {report.summary.result}.");
        }

        public static void BuildBatch()
        {
            try
            {
                BuildPlayerAt(Environment.GetEnvironmentVariable("PBR_LARGE_SPACE_BUILD_PATH") ?? PlayerPath);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
