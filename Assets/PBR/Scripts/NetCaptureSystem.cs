using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBR
{
    // One continuous world-space net: all four projection cameras see the same launch and mesh.
    public sealed class NetCaptureSystem : MonoBehaviour
    {
        sealed class Catch
        {
            public FishTarget fish;
            public Transform visual;
            public Vector3 position,scale;
            public Quaternion rotation;
            public Collider[] colliders;
            public bool[] colliderEnabled;
            public Renderer[] renderers;
            public float elapsed;
            public bool completed;
        }
        sealed class Shot
        {
            public Ray ray;
            public int player;
            public float age,distance,radius;
            public bool resolved;
            public Transform visual;
            public MeshRenderer renderer;
            public readonly List<Catch> catches=new List<Catch>();
        }
        FishExperiment experiment;
        readonly List<Shot> shots=new List<Shot>();
        MaterialPropertyBlock block;
        Mesh mesh;
        static readonly int Opacity=Shader.PropertyToID("_Opacity"),CaptureFade=Shader.PropertyToID("_CaptureFade");
        public int ActiveNetCount=>shots.Count;
        public int PendingFishCount
        {
            get {int count=0;foreach(var shot in shots)foreach(var caught in shot.catches)if(!caught.completed && caught.fish)count++;return count;}
        }
        public void Initialize(FishExperiment owner){experiment=owner;}
        void Awake(){block=new MaterialPropertyBlock();}
        public bool Launch(Ray ray,int player)
        {
            if(!experiment || !experiment.Running || !experiment.config.netMaterial || ray.direction.sqrMagnitude<.001f)return false;
            if(!mesh)mesh=BuildNet();
            ray.direction=ray.direction.normalized;
            float distance=Mathf.Clamp(experiment.config.netDistance,5,35);
            if(Physics.Raycast(ray,out var hit,distance,(1<<8)|(1<<9),QueryTriggerInteraction.Ignore))distance=Mathf.Max(1,hit.distance-.06f);
            var go=new GameObject("White fishing net");go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=experiment.config.netMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var shot=new Shot{ray=ray,player=player,distance=distance,radius=Mathf.Clamp(experiment.config.netRadius,.5f,5),visual=go.transform,renderer=renderer};
            shots.Add(shot);Pose(shot);
            experiment.RecordNetEvent("net_launched",null,player,"distance="+distance.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+";radius="+shot.radius.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
            return true;
        }
        public static bool InFootprint(Ray ray,float distance,float radius,Vector3 point)
        {
            Vector3 delta=point-ray.origin;float along=Vector3.Dot(delta,ray.direction);
            if(along<=.1f || along>100)return false;
            // Project the fish center onto the deployed net plane: the visible net defines the area.
            Vector3 projected=delta*(distance/along)-ray.direction*distance;
            return projected.sqrMagnitude<=radius*radius;
        }
        void Resolve(Shot shot)
        {
            shot.resolved=true;int blocked=0,rejected=0;
            foreach(var fish in experiment.Fish)
            {
                if(!fish || fish.IsBeingCaptured || !InFootprint(shot.ray,shot.distance,shot.radius,fish.transform.position))continue;
                if(!experiment.NetLineVisible(shot.ray.origin,fish)){blocked++;continue;}
                if(!experiment.CanNetCapture(fish,shot.player)){rejected++;continue;}
                var visual=fish.transform.Find("Animated fish");
                if(!visual)visual=fish.transform;
                var caught=new Catch{fish=fish,visual=visual,position=visual.localPosition,rotation=visual.localRotation,scale=visual.localScale,
                    colliders=fish.GetComponentsInChildren<Collider>(),renderers=visual.GetComponentsInChildren<Renderer>()};
                caught.colliderEnabled=new bool[caught.colliders.Length];
                for(int i=0;i<caught.colliders.Length;i++){caught.colliderEnabled[i]=caught.colliders[i].enabled;caught.colliders[i].enabled=false;}
                fish.IsBeingCaptured=true;shot.catches.Add(caught);experiment.RecordNetEvent("net_ensnared",fish,shot.player);
            }
            experiment.RecordNetEvent("net_impact",null,shot.player,"ensnared="+shot.catches.Count+";blocked="+blocked+";rejected="+rejected);
        }
        void Update(){Tick(Time.unscaledDeltaTime);}
        public void Tick(float dt)
        {
            if(!experiment)return;
            if(!experiment.Running){if(shots.Count>0)CancelAll();return;}
            dt=Mathf.Max(0,dt);
            float flight=Mathf.Clamp(experiment.config.netFlightSeconds,.1f,1),duration=Mathf.Clamp(experiment.config.netStruggleSeconds,.5f,2);
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];float previous=shot.age;shot.age+=dt;Pose(shot);
                if(!shot.resolved && shot.age>=flight)Resolve(shot);
                foreach(var caught in shot.catches)
                {
                    if(caught.completed || !caught.fish)continue;
                    caught.elapsed+=Mathf.Max(0,shot.age-Mathf.Max(previous,flight));
                    float progress=Mathf.Clamp01(caught.elapsed/duration),phase=caught.elapsed*35+caught.fish.Id*.9f;
                    caught.visual.localPosition=caught.position+new Vector3(Mathf.Sin(phase)*.055f,Mathf.Sin(phase*1.4f)*.04f,0)*(1-progress*.6f);
                    caught.visual.localRotation=caught.rotation*Quaternion.Euler(Mathf.Sin(phase*.8f)*12,Mathf.Sin(phase)*24,Mathf.Sin(phase*1.2f)*28);
                    float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.73f,1,progress));
                    caught.visual.localScale=caught.scale*Mathf.Lerp(1,.83f,progress);
                    foreach(var renderer in caught.renderers)if(renderer){renderer.GetPropertyBlock(block);block.SetFloat(CaptureFade,fade);renderer.SetPropertyBlock(block);}
                    if(progress>=1){caught.completed=true;experiment.CompleteNetCapture(caught.fish,shot.player);}
                }
                if(shot.age>=flight+duration+.28f){Destroy(shot.visual.gameObject);shots.RemoveAt(i);}
            }
        }
        void Pose(Shot shot)
        {
            float flight=Mathf.Clamp(experiment.config.netFlightSeconds,.1f,1),duration=Mathf.Clamp(experiment.config.netStruggleSeconds,.5f,2);
            float t=Mathf.Clamp01(shot.age/flight),ease=1-Mathf.Pow(1-t,2);
            shot.visual.SetPositionAndRotation(shot.ray.GetPoint(Mathf.Lerp(.7f,shot.distance,ease)),Quaternion.LookRotation(shot.ray.direction));
            float scale=Mathf.Lerp(.025f,1,Mathf.SmoothStep(0,1,t));
            if(t>=1)scale*=Mathf.Lerp(1,.88f,Mathf.Clamp01((shot.age-flight)/duration));
            shot.visual.localScale=Vector3.one*(shot.radius*scale);
            float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(flight+duration-.05f,flight+duration+.28f,shot.age));
            shot.renderer.GetPropertyBlock(block);block.SetFloat(Opacity,fade);shot.renderer.SetPropertyBlock(block);
        }
        public void CancelAll()
        {
            foreach(var shot in shots)
            {
                foreach(var caught in shot.catches)
                {
                    if(caught.completed || !caught.fish)continue;
                    caught.fish.IsBeingCaptured=false;
                    if(caught.visual){caught.visual.localPosition=caught.position;caught.visual.localRotation=caught.rotation;caught.visual.localScale=caught.scale;}
                    for(int i=0;i<caught.colliders.Length;i++)if(caught.colliders[i])caught.colliders[i].enabled=caught.colliderEnabled[i];
                    foreach(var renderer in caught.renderers)if(renderer){renderer.GetPropertyBlock(block);block.SetFloat(CaptureFade,1);renderer.SetPropertyBlock(block);}
                    experiment.RecordNetEvent("net_capture_cancelled",caught.fish,shot.player);
                }
                if(shot.visual)Destroy(shot.visual.gameObject);
            }
            shots.Clear();
        }
        void OnDestroy(){CancelAll();if(mesh)Destroy(mesh);}
        static Mesh BuildNet()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3 P(float x,float y)=>new Vector3(x,y,.18f*(1-x*x-y*y));
            void Segment(Vector3 a,Vector3 b,float width)
            {
                Vector3 side=Vector3.Cross((b-a).normalized,Vector3.forward).normalized*width;
                int start=vertices.Count;vertices.AddRange(new[]{a-side,a+side,b+side,b-side});triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            for(int axis=0;axis<2;axis++)for(int line=-5;line<=5;line++)
            {
                float offset=line/6f,extent=Mathf.Sqrt(1-offset*offset);
                for(int k=0;k<16;k++)
                {
                    float a=Mathf.Lerp(-extent,extent,k/16f),b=Mathf.Lerp(-extent,extent,(k+1)/16f);
                    Segment(axis==0?P(offset,a):P(a,offset),axis==0?P(offset,b):P(b,offset),.0055f);
                }
            }
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI/48,b=(i+1)*Mathf.PI/48;
                Segment(P(Mathf.Cos(a),Mathf.Sin(a)),P(Mathf.Cos(b),Mathf.Sin(b)),.012f);
            }
            var result=new Mesh{name="White woven circular fishing net"};result.SetVertices(vertices);result.SetTriangles(triangles,0);result.RecalculateBounds();return result;
        }
    }
}
