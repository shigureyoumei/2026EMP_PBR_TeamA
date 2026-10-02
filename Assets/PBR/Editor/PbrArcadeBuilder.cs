using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace PBR.Editor
{
    public static class PbrArcadeBuilder
    {
        const string Root = "Assets/PBR/GeneratedArcade";
        const string ScenePath = "Assets/PBR/Scenes/FishAttentionArcade.unity";
        static Material sand, stone, grass;
        [MenuItem("PBR/Build colorful reef scene")]
        public static void BuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root); Directory.CreateDirectory("Assets/PBR/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            sand = Material("Sand", new Color(.65f, .86f, .71f));
            stone = Material("Reef", new Color(.25f, .48f, .58f));
            grass = Material("Seagrass", new Color(.1f, .48f, .32f));
            var config = AssetDatabase.LoadAssetAtPath<ExperimentConfig>(Root + "/PilotConfig.asset");
            if (!config)
            {
                var original=AssetDatabase.LoadAssetAtPath<ExperimentConfig>("Assets/PBR/Generated/PilotConfig.asset");
                config=original?UnityEngine.Object.Instantiate(original):ScriptableObject.CreateInstance<ExperimentConfig>();
                AssetDatabase.CreateAsset(config,Root+"/PilotConfig.asset");
            }
            config.fishPrefabs = new[] { FishPrefab(1, new Color(1,.38f,.035f)), FishPrefab(2, new Color(.035f,.63f,1)), FishPrefab(3, new Color(.61f,.16f,.94f)) };
            config.persistentSchool = true;
            config.transferableMarkerMaterial = Material("TransferableMarker", Color.white);
            config.rockPrefabs = new[] { "rock_largeA", "rock_largeB", "rock_tallA" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KenneyNature/" + n + ".fbx")).ToArray();
            EditorUtility.SetDirty(config);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.66f,.8f,.88f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.02f,.42f,.65f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .006f;
            var sun = new GameObject("Soft underwater light").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 1.4f; sun.color = new Color(.86f,.97f,1); sun.transform.rotation = Quaternion.Euler(48,-32,0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.name = "Sand seabed";
            floor.transform.localScale = Vector3.one * 12; floor.GetComponent<Renderer>().sharedMaterial = sand;
            floor.layer = 8;
            // The illustrated sand supplies the distant ground; keep the physical floor for ray occlusion.
            floor.GetComponent<Renderer>().enabled=false;
            var rig = new GameObject("Experiment");
            var input = rig.AddComponent<ParticipantInput>();
            var session = rig.AddComponent<FishExperiment>(); session.config = config; session.input = input;
            var hud=rig.AddComponent<ExperimentHud>(); hud.experiment=session;
            hud.fishLegend="A  ORANGE / CREAM     B  BLUE / GOLD     IGNORE  VIOLET     WHITE DOT  TRANSFERABLE";
            rig.AddComponent<PrototypeSmokeRun>().experiment = session;
            rig.AddComponent<ReefShowcase>().experiment = session;
            rig.AddComponent<ReefArtPreview>();
            int sectors = config.panoramicPreview ? 3 : 1;
            var cameras = new Camera[sectors]; var paths = new SplineContainer[sectors * 2];
            for (int s = 0; s < sectors; s++)
            {
                float sectorYaw = sectors == 1 ? 0 : (s - 1) * 90;
                Quaternion rotation = Quaternion.Euler(0, sectorYaw, 0);
                var cam = new GameObject("Projection camera " + s).AddComponent<Camera>();
                cam.transform.position = input.Origin; cam.transform.rotation = rotation;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.045f,.2f,.27f);
                cam.nearClipPlane = .1f; cam.farClipPlane = 100;
                // Desktop view is wide. Three 90-degree square views are a logical preview, not a calibrated projection.
                cam.fieldOfView = sectors == 1 ? 52 : 90;
                cam.rect = new Rect((float)s / sectors, 0, 1f / sectors, 1);
                if (s == sectors / 2) { cam.tag = "MainCamera"; cam.gameObject.AddComponent<AudioListener>(); }
                cameras[s] = cam;
                var sector = new GameObject("Reef sector " + s).transform;
                sector.position = new Vector3(0, 0, -6); sector.rotation = rotation;
                ReefArtFactory.Occluder(sector);
                ReefArtFactory.Build(sector,cam);
                for (int lane = 0; lane < 2; lane++)
                {
                    var route = new GameObject("Fish route " + s + "-" + lane).AddComponent<SplineContainer>();
                    route.transform.SetParent(sector, false);
                    var spline = new Spline();
                    float y = lane == 0 ? 3.0f : 3.6f;
                    var points = new[] { new float3(-11,y,20), new float3(-6,y+.2f,21), new float3(0,y,22), new float3(6,y-.2f,21), new float3(11,y,20) };
                    if (lane == 1) Array.Reverse(points);
                    foreach (var p in points) spline.Add(new BezierKnot(p), TangentMode.AutoSmooth);
                    route.Spline = spline; paths[s * 2 + lane] = route;
                }
            }
            input.Cameras = cameras; session.routes = paths;
            EditorSceneManager.SaveScene(scene, ScenePath);
            var existing = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("PBR scene generated: " + ScenePath);
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; mat.SetFloat("_Smoothness", .2f); EditorUtility.SetDirty(mat); return mat;
        }
        static GameObject FishPrefab(int number, Color color)
        {
            string path = "Assets/ThirdParty/QuaterniusAnimatedFish/Fish" + number + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer.animationType != ModelImporterAnimationType.Legacy) { importer.animationType = ModelImporterAnimationType.Legacy; importer.SaveAndReimport(); }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new InvalidOperationException("Fish model missing: " + path);
            var root = new GameObject("FishVisual" + number);
            root.AddComponent<FishSurfaceCoordinates>();
            // Keep normalization off imported animated transforms; FBX curves can overwrite them.
            var normalizer = new GameObject("Visual normalization"); normalizer.transform.SetParent(root.transform, false);
            var model = UnityEngine.Object.Instantiate(source, normalizer.transform);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));
            if (!clip) throw new InvalidOperationException("No swimming animation in " + path);
            clip.SampleAnimation(model, 0);
            Bounds bounds = BoundsOf(model);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            normalizer.transform.localScale *= 1.5f / size;
            bounds = BoundsOf(model); normalizer.transform.localPosition -= bounds.center;
            var accent=number==1?new Color(1,.97f,.75f):number==2?new Color(1,.84f,.12f):new Color(.98f,.52f,.84f);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var material=Material("Fish"+number+renderer.name,color);
                material.shader=Shader.Find("PBR/Reef Fish");
                material.SetColor("_BaseColor",color); material.SetColor("_AccentColor",accent);
                material.SetColor("_FinColor",number==1?new Color(1,.62f,.02f):number==2?new Color(.08f,.25f,.93f):new Color(.58f,.52f,.72f));
                material.SetFloat("_StripeCount",number==3?6:3);
                var mesh=renderer is SkinnedMeshRenderer sk?sk.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                var local=mesh.bounds; material.SetVector("_LocalMin",local.min);
                material.SetVector("_LocalInvSize",new Vector4(1/Mathf.Max(local.size.x,.0001f),1/Mathf.Max(local.size.y,.0001f),1/Mathf.Max(local.size.z,.0001f),0));
                EditorUtility.SetDirty(material);
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>material).ToArray();
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
            }
            foreach (int side in new[] {-1,1})
            {
                AddEye(root.transform,new Vector3(side*.22f,.15f,.4f),.15f,Material("EyeWhite",Color.white));
                AddEye(root.transform,new Vector3(side*.29f,.15f,.42f),.079f,Material("EyeDark",new Color(.012f,.025f,.075f)));
                AddEye(root.transform,new Vector3(side*.323f,.17f,.434f),.022f,Material("EyeShine",Color.white));
            }
            var animation = model.GetComponent<Animation>(); if (!animation) animation = model.AddComponent<Animation>();
            animation.AddClip(clip, clip.name); animation.clip = clip; animation.playAutomatically = true; animation.wrapMode = WrapMode.Loop;
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Fish" + number + ".prefab");
            UnityEngine.Object.DestroyImmediate(root); return prefab;
        }
        static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(); bool first = true;
            foreach (var r in renderers)
            {
                Bounds world = r.bounds;
                if (r is SkinnedMeshRenderer skin)
                {
                    // Imported FBX localBounds can describe a different bind-pose scale.
                    // Normalize actual deformed geometry, not that cached culling box.
                    var baked = new Mesh(); skin.BakeMesh(baked, true);
                    var vertices = baked.vertices;
                    world = new Bounds(skin.transform.TransformPoint(vertices[0]), Vector3.zero);
                    foreach (var vertex in vertices) world.Encapsulate(skin.transform.TransformPoint(vertex));
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                if (first) { b = world; first = false; } else b.Encapsulate(world);
            }
            return b;
        }
        static void AddEye(Transform parent,Vector3 position,float size,Material material)
        {
            var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name="Eye detail";
            UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());
            eye.transform.SetParent(parent,false); eye.transform.localPosition=position; eye.transform.localScale=Vector3.one*size;
            eye.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Rock(GameObject prefab, Transform parent, Vector3 localPosition, Vector3 dimensions)
        {
            var holder = new GameObject("Occluding reef"); holder.transform.SetParent(parent, false); holder.transform.localPosition = localPosition;
            var model = UnityEngine.Object.Instantiate(prefab, holder.transform);
            // Work in an unrotated local staging transform while normalizing imported model bounds.
            Quaternion rotation = holder.transform.rotation; holder.transform.rotation = Quaternion.identity;
            Bounds bounds = BoundsOf(model);
            model.transform.localScale = new Vector3(dimensions.x/bounds.size.x, dimensions.y/bounds.size.y, dimensions.z/bounds.size.z);
            bounds = BoundsOf(model);
            model.transform.position += new Vector3(holder.transform.position.x - bounds.center.x, holder.transform.position.y - bounds.min.y, holder.transform.position.z - bounds.center.z);
            foreach (var mesh in model.GetComponentsInChildren<MeshFilter>())
            {
                mesh.gameObject.layer = 8;
                mesh.GetComponent<Renderer>().sharedMaterial = stone;
                mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
            }
            holder.transform.rotation = rotation;
        }
        [MenuItem("PBR/Validate colorful reef")]
        public static void Validate()
        {
            int assertions = 0;
            void Check(bool value, string message) { assertions++; if (!value) throw new Exception("PBR test failed: " + message); }
            foreach (ExperimentCondition c in Enum.GetValues(typeof(ExperimentCondition)))
                foreach (FishKind k in Enum.GetValues(typeof(FishKind)))
                    for (int p=0;p<2;p++)
                    {
                        int owner = ExperimentRules.Owner(k);
                        Check(ExperimentRules.CanCapture(c,k,p,-1) == (owner >= 0 && (c == ExperimentCondition.SharedRole || p == owner)), "capture permissions");
                    }
            Check(ExperimentRules.CanCapture(ExperimentCondition.AdaptiveHandoff, FishKind.TransferableA,1,1), "handoff permission");
            Check(!ExperimentRules.CanCapture(ExperimentCondition.FixedRole, FishKind.TransferableA,1,1), "no stale handoff in fixed");
            Check(!ExperimentRules.CanCapture(ExperimentCondition.AdaptiveHandoff, FishKind.PlayerA,1,1), "nontransferable target");
            Check(ExperimentRules.Covered(179,-179,5), "wrapped yaw");
            Check(!ExperimentRules.Covered(0,40,25), "outside cone");
            var session = UnityEngine.Object.FindAnyObjectByType<FishExperiment>();
            Check(session && session.routes.Length >= 2, "scene references");
            Physics.SyncTransforms();
            foreach (var route in session.routes)
            {
                var go = new GameObject("Visibility test"); var f = go.AddComponent<FishTarget>();
                f.transform.position = route.EvaluatePosition(.5f); Check(!session.LineVisible(f), "midpoint occluded by real rock");
                f.transform.position = route.EvaluatePosition(0); Check(session.LineVisible(f), "entry visible");
                f.transform.position = route.EvaluatePosition(1); Check(session.LineVisible(f), "exit visible");
                UnityEngine.Object.DestroyImmediate(go);
            }
            foreach (var prefab in session.config.fishPrefabs)
            {
                Check(prefab.GetComponentInChildren<Animation>().clip != null, "swimming clip");
                var instance = UnityEngine.Object.Instantiate(prefab);
                var animation = instance.GetComponentInChildren<Animation>();
                animation.Play(animation.clip.name);
                foreach (float t in new[] {0f,.25f,.5f,.75f})
                {
                    animation[animation.clip.name].normalizedTime = t; animation.Sample();
                    var b = BoundsOf(instance);
                    float size = Mathf.Max(b.size.x,b.size.y,b.size.z);
                    Check(size > .3f && size < 3f, "animated model retains normalized scale: " + prefab.name + " t=" + t + " size=" + b.size.ToString("G9"));
                    Check(b.center.magnitude < 1.5f, "animated model stays at target collider");
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/editmode-results.txt", "PASS: " + assertions + " assertions\nUnity " + Application.unityVersion + "\n");
            Debug.Log("PBR validation passed: " + assertions);
        }
        public static void BuildAndValidateBatch()
        {
            try
            {
                BuildScene(); Validate();
                var report = BuildPipeline.BuildPlayer(new[] {ScenePath}, "BuildsArcade/PBRReef.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
