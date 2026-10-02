using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBR.Editor
{
    public static class RingReefBuilder
    {
        public const string ScenePath = "Assets/PBR/Scenes/FishAttention360.unity";
        const string Root = "Assets/PBR/Generated360";
        [MenuItem("PBR/Build 360 degree reef")]
        public static void BuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<ExperimentConfig>(Root + "/PilotConfig.asset");
            if (!config)
            {
                config = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ExperimentConfig>("Assets/PBR/GeneratedArcade/PilotConfig.asset"));
                AssetDatabase.CreateAsset(config, Root + "/PilotConfig.asset");
            }
            config.ringWorld = config.persistentSchool = true;
            EditorUtility.SetDirty(config);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f,.78f,.84f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.015f,.28f,.43f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .028f;
            var sun = new GameObject("Underwater sunlight").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 1.3f; sun.color = new Color(.78f,.96f,1); sun.transform.rotation = Quaternion.Euler(55,-28,0);
            var root = new GameObject("Continuous 360 degree habitat").transform;
            var habitat = root.gameObject.AddComponent<RingHabitat>();
            var waterMaterial = new Material(Shader.Find("PBR/Surrounding Water")); SaveMaterial(waterMaterial, Root+"/Water.mat");
            var water = GameObject.CreatePrimitive(PrimitiveType.Sphere); water.name="Continuous distant water and surface";
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());water.transform.SetParent(root,false);water.transform.localScale=Vector3.one*175;
            water.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Water.mat");
            var contactMaterial=new Material(Shader.Find("PBR/Reef Contact Shade"));SaveMaterial(contactMaterial,Root+"/Contact.mat");
            contactMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Contact.mat");
            var sand = new Material(Shader.Find("PBR/Three Dimensional Seabed"));
            SaveMaterial(sand, Root + "/Seabed.mat");
            const int side = 65; var vertices = new Vector3[side*side]; var triangles = new int[(side-1)*(side-1)*6];
            for(int z=0;z<side;z++) for(int x=0;x<side;x++)
            {
                float wx=(x-32)*2.5f, wz=(z-32)*2.5f;
                vertices[z*side+x]=new Vector3(wx,-.25f+.12f*Mathf.Sin(wx*.3f)*Mathf.Cos(wz*.22f),wz);
            }
            int t=0;
            for(int z=0;z<side-1;z++) for(int x=0;x<side-1;x++)
            { int a=z*side+x; triangles[t++]=a;triangles[t++]=a+side;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=a+side;triangles[t++]=a+side+1; }
            var mesh=new Mesh{name="Undulating sand floor"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string meshPath=Root+"/Seabed.asset";
            var previousMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(previousMesh) { EditorUtility.CopySerialized(mesh,previousMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=previousMesh; } else AssetDatabase.CreateAsset(mesh,meshPath);
            var floor=new GameObject("3D sand seabed");floor.transform.SetParent(root,false);floor.layer=8;
            floor.AddComponent<MeshFilter>().sharedMesh=mesh;floor.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Seabed.mat");floor.AddComponent<MeshCollider>().sharedMesh=mesh;
            var stone = new Material(Shader.Find("PBR/Reef Stone")); SaveMaterial(stone,Root+"/Rock.mat");
            stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Rock.mat");
            DiverseReefFactory.Build(root,habitat,config);
            var rig=new GameObject("Experiment 360");var input=rig.AddComponent<ParticipantInput>();input.Origin=habitat.Eye;
            var experiment=rig.AddComponent<FishExperiment>();experiment.config=config;experiment.input=input;
            var hud=rig.AddComponent<ExperimentHud>();hud.experiment=experiment;hud.fishLegend="A ORANGE / CREAM   B BLUE / GOLD   IGNORE VIOLET   WHITE DOT TRANSFERABLE";
            var projection=rig.AddComponent<FourWallProjection>();var cameras=new Camera[4];
            var clear=new GameObject("Preview background clear").AddComponent<Camera>();clear.depth=-10;clear.cullingMask=0;clear.clearFlags=CameraClearFlags.SolidColor;clear.backgroundColor=new Color(.005f,.025f,.04f);
            for(int i=0;i<4;i++)
            {
                var cam=new GameObject(new[]{"Front +Z","Right +X","Back -Z","Left -X"}[i]).AddComponent<Camera>();
                cam.transform.position=input.Origin;cam.transform.rotation=Quaternion.Euler(0,i*90,0);cam.nearClipPlane=.1f;cam.farClipPlane=100;
                cam.backgroundColor=RenderSettings.fogColor;cam.clearFlags=CameraClearFlags.SolidColor;cam.depth=i;
                cam.rect=new Rect(i%2*.5f,i<2?.5f:0,.5f,.5f);cam.aspect=16f/9;cam.fieldOfView=58.7155f;
                if(i==0) { cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>(); } cameras[i]=cam;
            }
            projection.walls=cameras;input.Cameras=cameras;
            ProjectionRoomSetup.Attach();
            EditorSceneManager.SaveScene(scene,ScenePath);
            var buildScenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();buildScenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=buildScenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        static void SaveMaterial(Material material,string path)
        {
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing) { EditorUtility.CopySerialized(material,existing);UnityEngine.Object.DestroyImmediate(material); } else AssetDatabase.CreateAsset(material,path);
        }
        public static void BuildBatch()
        {
            try
            {
                BuildScene();
                var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds360Diverse/PBR360.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("360 build failed");
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        }
    }
}
