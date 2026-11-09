using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBR.Editor
{
    public static class NetCaptureSetup
    {
        public static void Configure(ExperimentConfig config)
        {
            const string root="Assets/PBR/GeneratedNet";Directory.CreateDirectory(root);AssetDatabase.Refresh();
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+"/WhiteNet.mat");
            if(!material){material=new Material(Shader.Find("PBR/White Fishing Net"));AssetDatabase.CreateAsset(material,root+"/WhiteNet.mat");}
            material.SetColor("_BaseColor",new Color(1,1,1,.96f));material.SetFloat("_Opacity",1);EditorUtility.SetDirty(material);
            config.netMaterial=material;config.netCaptureEnabled=true;config.maxHoverSeconds=2;
            config.hidePauseSeconds=Mathf.Min(config.hidePauseSeconds,2);EditorUtility.SetDirty(config);
        }
        public static void BuildBatch()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(RingReefBuilder.ScenePath);
                Configure(UnityEngine.Object.FindAnyObjectByType<FishExperiment>().config);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                var report=BuildPipeline.BuildPlayer(new[]{RingReefBuilder.ScenePath},"BuildsNet/PBRNet.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Net player build failed");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
