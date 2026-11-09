using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PBR.Editor
{
    public static class NaturalReefUpgrade
    {
        public const string Root="Assets/PBR/GeneratedNatural";
        const string Textures="Assets/PBR/Art/NaturalReef/";
        static Mesh Save(Mesh mesh,string name)
        {
            foreach(var p in mesh.vertices)
                if(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.sqrMagnitude))throw new Exception("Non-finite generated mesh: "+name);
            string path=Root+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static Material Surface(string name,Color color,int style,string scan="",float sway=0)
        {
            string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("PBR/Natural Reef Surface"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Style",style);mat.SetFloat("_Sway",sway);mat.SetFloat("_Tiling",style==1?.45f:.65f);mat.SetFloat("_NormalStrength",style==1?.45f:.8f);mat.SetFloat("_Smoothness",.23f);mat.enableInstancing=true;
            if(scan!="")
            {
                mat.SetTexture("_RockMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_diffuse.jpg"));
                mat.SetTexture("_RockNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_nor_gl.jpg"));
                mat.SetTexture("_RoughMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+scan+"_rough.jpg"));
            }
            EditorUtility.SetDirty(mat);return mat;
        }
        static void ConfigureTextures()
        {
            foreach(string path in Directory.GetFiles(Textures,"*.jpg"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                bool normal=path.Contains("nor_gl");importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.Contains("diffuse");importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
            }
        }
        static GameObject Make(Transform parent,string name,Mesh mesh,Material material,Vector3 position,Vector3 scale,float yaw=0,bool physical=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            if(physical){go.layer=8;go.AddComponent<MeshCollider>().sharedMesh=mesh;}return go;
        }
        static void Swap(GameObject go,Mesh mesh,Material material)
        {
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{material};
            var collider=go.GetComponent<MeshCollider>();if(collider){collider.sharedMesh=null;collider.sharedMesh=mesh;}
        }
        static void Fish(ExperimentConfig config)
        {
            var prefabs=new List<GameObject>();
            string[] species={"Clownfish","Butterflyfish","BlueTang","Wrasse","Angelfish","Fusilier","Damselfish","Bannerfish"};
            for(int s=0;s<8;s++)
            {
                var mesh=Save(NaturalReefGeometry.Fish(s),"FishMesh"+s);
                for(int role=0;role<3;role++)
                {
                    string name=species[s]+"_"+new[]{"A","B","Distractor"}[role];string path=Root+"/"+name+".mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!mat){mat=new Material(Shader.Find("PBR/Natural Reef Fish"));AssetDatabase.CreateAsset(mat,path);}
                    Color[] colors={new Color(.87f,.32f,.075f),new Color(.045f,.33f,.62f),new Color(.32f,.24f,.39f)};
                    Color[] accents={new Color(.95f,.81f,.53f),new Color(.78f,.83f,.58f),new Color(.56f,.5f,.62f)};
                    mat.SetColor("_BaseColor",colors[role]*Mathf.Lerp(.87f,1.1f,s/7f));mat.SetColor("_AccentColor",accents[role]);mat.SetFloat("_Pattern",s);mat.SetFloat("_Amplitude",.09f);mat.SetFloat("_Frequency",1.65f+s*.12f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
                    var go=Make(null,name,mesh,mat,Vector3.zero,Vector3.one*1.13f);
                    go.AddComponent<ReefFishSpecies>().species=species[s];
                    prefabs.Add(PrefabUtility.SaveAsPrefabAsset(go,Root+"/"+name+".prefab"));UnityEngine.Object.DestroyImmediate(go);
                }
            }
            config.fishPrefabs=prefabs.ToArray();config.fishCount=50;config.naturalReef=true;EditorUtility.SetDirty(config);
        }
        static void Atmosphere(Transform root)
        {
            Material Mat(string name,Color color,bool particles)
            {
                string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!mat){mat=new Material(Shader.Find("PBR/Underwater Atmosphere"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",color);mat.SetFloat("_Particle",particles?1:0);EditorUtility.SetDirty(mat);return mat;
            }
            var shaftMat=Mat("FilteredSunbeams",new Color(.36f,.68f,.67f,.065f),false);
            var shaft=new NaturalReefGeometry();
            int a=shaft.Vertex(new Vector3(-2,0,0),Vector3.back,new Vector2(0,0),Color.white);
            int b=shaft.Vertex(new Vector3(2,0,0),Vector3.back,new Vector2(1,0),Color.white);
            int c=shaft.Vertex(new Vector3(6,19,3),Vector3.back,new Vector2(1,1),Color.white);
            int d=shaft.Vertex(new Vector3(5,19,3),Vector3.back,new Vector2(0,1),Color.white);
            shaft.Tri(a,b,c,Vector3.back);shaft.Tri(a,c,d,Vector3.back);var mesh=Save(shaft.Finish("Soft filtered sunlight"),"Sunbeam");
            for(int i=0;i<12;i++)Make(root,"Filtered sunlight shaft "+i,mesh,shaftMat,RingHabitat.Polar(i*2.399963f,24+i%4*3,-.2f),Vector3.one,i*137);
            var go=new GameObject("Slow suspended marine particles");go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(0,4,0);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=80;main.loop=true;main.prewarm=true;main.startLifetime=80;main.startSpeed=.035f;main.startSize=new ParticleSystem.MinMaxCurve(.012f,.03f);main.maxParticles=650;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.6f,.8f,.8f,.5f);
            var emission=ps.emission;emission.rateOverTime=8;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(65,10,65);
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.015f;velocity.y=.008f;velocity.z=.01f;
            ps.useAutoRandomSeed=false;ps.randomSeed=20261101;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Mat("MarineParticle",new Color(.63f,.8f,.81f,.65f),true);renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        [MenuItem("PBR/Upgrade current reef to natural environment")]
        public static void Upgrade()
        {
            var habitat=UnityEngine.Object.FindAnyObjectByType<RingHabitat>();var session=UnityEngine.Object.FindAnyObjectByType<FishExperiment>();
            if(!habitat || !session)throw new Exception("Open FishAttention360 first.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();ConfigureTextures();
            var config=session.config;Fish(config);NetCaptureSetup.Configure(config);
            var limestone=Surface("ScannedReefLimestone",new Color(.83f,.88f,.8f),0,"coral_fort_wall_02");
            var darkStone=Surface("ScannedBlueLimestone",new Color(.52f,.65f,.64f),0,"coral_fort_wall_02");
            var sand=Surface("ScannedRippleSand",new Color(.88f,.92f,.85f),1,"coast_sand_01");
            var kelp=Surface("RibbonKelp",new Color(.18f,.30f,.08f),3,"",.075f);
            var grass=Surface("Seagrass",new Color(.19f,.34f,.095f),3,"",.11f);
            var coralMats=new[]{Surface("OchreCoral",new Color(.49f,.31f,.16f),2),Surface("RoseCoral",new Color(.51f,.22f,.2f),2),Surface("LavenderCoral",new Color(.34f,.29f,.41f),2),Surface("IvoryCoral",new Color(.67f,.58f,.37f),2)};
            var root=habitat.transform;
            // Replace the old decorative layer in-place, preserving the observer, scene and all cover sites.
            var obsolete=new List<GameObject>();foreach(Transform child in root)
                if(!child.GetComponent<ReefCover>() && child.name!="3D sand seabed" && child.name!="Continuous distant water and surface")obsolete.Add(child.gameObject);
            foreach(var go in obsolete)UnityEngine.Object.DestroyImmediate(go);
            var detail=root.GetComponent<ReefEnvironmentDetail>();if(!detail)detail=root.gameObject.AddComponent<ReefEnvironmentDetail>();detail.config=config;
            detail.mediumDetails=new GameObject("Natural reef medium detail");detail.mediumDetails.transform.SetParent(root,false);
            detail.richDetails=new GameObject("Natural reef rich detail");detail.richDetails.transform.SetParent(root,false);
            var rockMeshes=new Mesh[8];for(int i=0;i<8;i++)rockMeshes[i]=Save(NaturalReefGeometry.Rock(910+i),"Limestone"+i);
            var vegetation=new Mesh[4];var branches=new Mesh[4];
            for(int i=0;i<4;i++){vegetation[i]=Save(NaturalReefGeometry.Plants(161+i,false),"Seagrass"+i);branches[i]=Save(NaturalReefGeometry.Coral(279+i,false),"Staghorn"+i);}
            var kelpMesh=Save(NaturalReefGeometry.Plants(82,true),"DenseKelp");var gorgonian=Save(NaturalReefGeometry.Coral(188,true),"Gorgonian");
            int index=0;
            foreach(var cover in habitat.covers)
            {
                Mesh mesh;Material material=limestone;
                switch(cover.kind)
                {
                    case ReefCoverKind.Rock:mesh=rockMeshes[index%8];break;
                    case ReefCoverKind.Cave:mesh=Save(NaturalReefGeometry.Tunnel(true,index),"WeatheredCave"+index);material=darkStone;break;
                    case ReefCoverKind.Arch:mesh=Save(NaturalReefGeometry.Tunnel(false,index),"WeatheredArch"+index);break;
                    case ReefCoverKind.Kelp:mesh=kelpMesh;material=kelp;break;
                    default:
                        var geo=new NaturalReefGeometry();geo.Ellipsoid(new Vector3(0,1.65f,0),new Vector3(2.1f,1.75f,.95f),index,true,Color.white,28,40);
                        mesh=Save(geo.Finish("Massive living coral"),"LivingCoral"+index);material=coralMats[index%4];break;
                }
                Swap(cover.gameObject,mesh,material);index++;
                if(cover.kind==ReefCoverKind.FanCoral)Make(cover.transform,"Fine gorgonian branches",gorgonian,coralMats[(index+1)%4],new Vector3(0,2.5f,.2f),Vector3.one*.65f,15);
            }
            var floor=root.Find("3D sand seabed");floor.GetComponent<Renderer>().sharedMaterial=sand;
            var rng=new System.Random(20261101);float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
            // Rich detail covers every azimuth; foreground remains open around the participant.
            for(int i=0;i<160;i++)
            {
                float angle=i*2.399963f,radius=R(7.6f,38);var parent=i%3==0?detail.mediumDetails.transform:detail.richDetails.transform;
                Make(parent,"Seagrass meadow "+i,vegetation[i%4],grass,RingHabitat.Polar(angle,radius,-.23f),new Vector3(R(.65f,1.6f),R(.7f,1.5f),R(.65f,1.6f)),R(0,360));
            }
            for(int i=0;i<100;i++)
            {
                float angle=i*2.399963f+.42f,radius=R(8,39);var parent=i%3==0?detail.mediumDetails.transform:detail.richDetails.transform;
                Make(parent,"Living staghorn colony "+i,branches[i%4],coralMats[i%4],RingHabitat.Polar(angle,radius,-.15f),Vector3.one*R(.38f,1.15f),R(0,360));
                if(i%5==0)Make(parent,"Sea fan colony "+i,gorgonian,coralMats[(i+1)%4],RingHabitat.Polar(angle+.07f,radius+1,-.2f),Vector3.one*R(.6f,1.2f),angle*Mathf.Rad2Deg);
            }
            for(int i=0;i<150;i++)
            {
                float angle=i*2.399963f+.13f,radius=R(8,43),size=R(.18f,.6f);
                Make(detail.richDetails.transform,"Eroded reef rubble "+i,rockMeshes[i%8],i%3==0?darkStone:limestone,RingHabitat.Polar(angle,radius,-.19f),new Vector3(size*R(1.1f,2),size*.65f,size*R(1,1.8f)),R(0,360));
            }
            // Irregular distant outcrops replace the old flat, repeated canyon panels.
            for(int i=0;i<48;i++)
            {
                float angle=(i+.2f)*Mathf.PI*2/48,radius=R(34,47),h=R(2.3f,5.5f);
                Make(root,"Distant weathered reef "+i,rockMeshes[i%8],i%3==0?darkStone:limestone,RingHabitat.Polar(angle,radius,h*.35f-.3f),new Vector3(R(2,4),h,R(2,4)),R(0,360));
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.29f,.47f,.51f);RenderSettings.ambientEquatorColor=new Color(.12f,.25f,.29f);RenderSettings.ambientGroundColor=new Color(.075f,.12f,.12f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.035f,.235f,.29f);RenderSettings.fogDensity=.024f;
            var sun=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            sun.intensity=2.0f;sun.color=new Color(.84f,.96f,.92f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.76f;sun.shadowBias=.035f;sun.shadowNormalBias=.15f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.sun=sun;QualitySettings.shadowDistance=55;
            Atmosphere(detail.richDetails.transform);
            var oldVolume=GameObject.Find("Natural underwater grading");if(oldVolume)UnityEngine.Object.DestroyImmediate(oldVolume);
            var volume=new GameObject("Natural underwater grading").AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/UnderwaterVolume.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/UnderwaterVolume.asset");}
            foreach(var component in profile.components.ToArray()){profile.Remove(component.GetType());UnityEngine.Object.DestroyImmediate(component,true);}
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var adjust=profile.Add<ColorAdjustments>(true);adjust.postExposure.Override(.45f);adjust.contrast.Override(8);adjust.saturation.Override(3);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.12f);bloom.threshold.Override(1.2f);
            foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
            volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
            var projection=UnityEngine.Object.FindAnyObjectByType<FourWallProjection>();
            foreach(var camera in projection.walls){camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
            session.GetComponent<ExperimentHud>().fishLegend="A ORANGE / CREAM   B BLUE / GOLD   IGNORE VIOLET   WHITE DOT TRANSFERABLE";
            EditorSceneManager.MarkSceneDirty(habitat.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void BuildBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(RingReefBuilder.ScenePath);Upgrade();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                var report=BuildPipeline.BuildPlayer(new[]{RingReefBuilder.ScenePath},"BuildsNatural/PBRNatural.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Natural reef build failed");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
