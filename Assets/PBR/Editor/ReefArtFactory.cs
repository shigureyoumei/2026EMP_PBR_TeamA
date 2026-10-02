using System;
using UnityEditor;
using UnityEngine;

namespace PBR.Editor
{
    public static class ReefArtFactory
    {
        const string Root = "Assets/PBR/GeneratedArcade";
        static Material pink, purple, orange, lime, turquoise, pearl;
        public static void Occluder(Transform sector)
        {
            var stone=Mat("LivingReefStone",new Color(.15f,.35f,.43f));
            stone.shader=Shader.Find("PBR/Reef Stone");
            var root=new GameObject("Rounded physical reef").transform; root.SetParent(sector,false);
            root.localPosition=new Vector3(0,0,16);
            Vector3[] positions={new Vector3(0,2.05f,0),new Vector3(-1.5f,1.35f,.15f),new Vector3(1.4f,1.5f,.15f),new Vector3(-2,0.65f,-.4f),new Vector3(2,.65f,-.3f)};
            Vector3[] scales={new Vector3(3.6f,4.35f,3),new Vector3(2.9f,2.9f,2.8f),new Vector3(2.7f,3.1f,2.9f),new Vector3(2.7f,1.5f,2.8f),new Vector3(2.5f,1.6f,2.6f)};
            for(int i=0;i<positions.Length;i++)
            {
                var rock=Sphere("Coral encrusted rock",root,positions[i],scales[i],stone);
                rock.layer=8; rock.AddComponent<MeshCollider>().sharedMesh=rock.GetComponent<MeshFilter>().sharedMesh;
            }
            EditorUtility.SetDirty(stone);
        }
        public static void Build(Transform sector, Camera camera)
        {
            pink=Mat("CoralPink",new Color(1,.14f,.43f));
            purple=Mat("CoralPurple",new Color(.49f,.16f,.92f));
            orange=Mat("CoralOrange",new Color(1,.48f,.07f));
            lime=Mat("KelpLime",new Color(.42f,.8f,.07f));
            turquoise=Mat("CoralTurquoise",new Color(.04f,.73f,.62f));
            pearl=Mat("Pearl",new Color(.93f,.96f,1));
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PBR/Art/ReefBackdrop.png");
            if (!texture) throw new InvalidOperationException("Reef backdrop image is missing");
            var backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad); backdrop.name="Illustrated distant reef";
            UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            backdrop.transform.SetParent(camera.transform,false); backdrop.transform.localPosition=new Vector3(0,0,70);
            float h=2*70*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
            backdrop.transform.localScale=new Vector3(h*16/9,h,1);
            var bg=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Backdrop.mat");
            if (!bg) { bg=new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(bg,Root+"/Backdrop.mat"); }
            bg.mainTexture=texture; bg.color=Color.white; EditorUtility.SetDirty(bg);
            backdrop.GetComponent<Renderer>().sharedMaterial=bg;
            // Decorative coral has no collider; only the opaque rock island is an experimental occluder.
            var island=new GameObject("Living coral island").transform; island.SetParent(sector,false);
            island.localPosition=new Vector3(0,0,16);
            for (int i=0;i<9;i++)
            {
                float x=(i-4)*.46f;
                Branch(island,new Vector3(x,3.85f-Mathf.Abs(x)*.6f,-.5f),.6f+(i%3)*.18f,i%2==0?pink:orange,31+i);
            }
            for (int i=0;i<12;i++)
            {
                float angle=i*2.399963f;
                var center=new Vector3(Mathf.Cos(angle)*2.25f,.7f+(i%3)*.38f,Mathf.Sin(angle)*1.4f);
                Plate(island,center,.8f+(i%3)*.18f,i%2==0?purple:turquoise,i);
            }
            for (int i=0;i<8;i++) Tube(island,new Vector3(-2.4f+i*.26f,.4f,-1.2f),.5f+i%3*.3f,.16f,purple);
            // More detailed foreground edges, keeping the central target lanes readable.
            foreach (int side in new[] {-1,1})
            {
                var garden=new GameObject(side<0?"Left coral garden":"Right coral garden").transform;
                garden.SetParent(sector,false); garden.localPosition=new Vector3(side*10.2f,-.1f,15);
                for (int i=0;i<13;i++)
                {
                    var p=new Vector3((i%4-1.5f)*.8f,0,(i/4-1)*.8f);
                    if (i%3==0) Branch(garden,p,1.4f+i%4*.23f,i%2==0?pink:orange,71+i);
                    else if(i%3==1) Plate(garden,p+Vector3.up*.3f,1.05f,i%2==0?purple:turquoise,40+i);
                    else Tube(garden,p,1+i%3*.25f,.22f,purple);
                }
                for (int i=0;i<8;i++) Kelp(garden,new Vector3(side*(1.5f+i*.2f),0,i*.3f-1),2+i%3*.55f,i);
                Star(garden,new Vector3(-side*1.5f,.14f,-1.8f),.6f,orange);
                var bubble=Mat("Bubble",new Color(.45f,.94f,1));
                for(int i=0;i<9;i++) Sphere("Bubble",garden,new Vector3(side*2+.16f*Mathf.Sin(i),.4f+i*.48f,1),Vector3.one*(.04f+(i%3)*.025f),bubble);
            }
        }
        public static Material Mat(string name, Color color)
        {
            string path=Root+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.color=color; material.SetFloat("_Smoothness",.35f);
            material.SetFloat("_Cull",0);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*.1f);
            EditorUtility.SetDirty(material); return material;
        }
        public static void RingGarden(Transform parent, int seed, float scale)
        {
            lime = Mat("RingKelp", new Color(.24f,.72f,.09f));
            pearl = Mat("RingTips", new Color(.9f,.96f,1));
            var interior = Mat("RingSpongeInterior", new Color(.13f,.025f,.27f));
            var colors = new[] { Mat("RingPink",new Color(1,.12f,.4f)), Mat("RingGold",new Color(1,.53f,.07f)), Mat("RingPurple",new Color(.5f,.12f,.9f)), Mat("RingTeal",new Color(.03f,.8f,.64f)) };
            var rng = new System.Random(seed);
            for (int i = 0; i < 7; i++)
            {
                Vector3 p = new Vector3(((float)rng.NextDouble()-.5f)*2.5f, .1f, ((float)rng.NextDouble()-.5f)*2.5f)*scale;
                if (i % 2 == 0) Branch(parent,p,(.65f+(float)rng.NextDouble())*scale,colors[i%4],seed+i);
                else Tube(parent,p,(.5f+(float)rng.NextDouble())*scale,.2f*scale,colors[i%4],interior);
            }
            for (int i=0;i<3;i++) Kelp(parent,new Vector3((i-1)*.65f,0,1.1f)*scale,(1.3f+i*.4f)*scale,seed+i);
        }
        static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name=name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
        static void Limb(Transform parent,Vector3 a,Vector3 b,float radius,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name="Coral branch";
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=(a+b)*.5f;
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
            go.transform.localScale=new Vector3(radius,(b-a).magnitude*.5f+radius*.25f,radius);
            go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        static void Branch(Transform parent,Vector3 origin,float height,Material material,int seed)
        {
            var rng=new System.Random(seed);
            Vector3 top=origin+new Vector3(.2f,height,0); Limb(parent,origin,top,.13f,material);
            for(int i=0;i<5;i++)
            {
                Vector3 a=Vector3.Lerp(origin,top,.28f+i*.12f);
                Vector3 b=a+new Vector3((i%2==0?-1:1)*height*(.25f+(float)rng.NextDouble()*.22f),height*.27f,(float)rng.NextDouble()*.4f-.2f);
                Limb(parent,a,b,.095f,material);
                Vector3 tip=b+Vector3.up*height*.2f; Limb(parent,b,tip,.06f,material);
                Sphere("Coral growing tip",parent,tip,Vector3.one*.095f,pearl);
            }
        }
        static void Plate(Transform parent,Vector3 center,float radius,Material material,int id)
        {
            const int count=40; var vertices=new Vector3[count+1]; var triangles=new int[count*3];
            vertices[0]=Vector3.zero;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count; float r=radius*(1+.075f*Mathf.Sin(a*7+id));
                vertices[i+1]=new Vector3(Mathf.Cos(a)*r,.1f*Mathf.Sin(a*5+id),Mathf.Sin(a)*r*.7f);
                int j=i*3; triangles[j]=0; triangles[j+1]=(i+1)%count+1; triangles[j+2]=i+1;
            }
            var mesh=new Mesh {name="Wavy plate coral"}; mesh.vertices=vertices; mesh.triangles=triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path=Root+"/Plate_"+id+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing) { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
            else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject("Plate coral"); go.transform.SetParent(parent,false); go.transform.localPosition=center;
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void Tube(Transform parent,Vector3 pos,float height,float radius,Material material,Material interior=null)
        {
            var go=Sphere("Tube sponge",parent,pos+Vector3.up*height*.5f,new Vector3(radius*2,height,radius*2),material);
            var opening=Sphere("Sponge opening",parent,pos+Vector3.up*height,new Vector3(radius*1.35f,.035f,radius*1.35f),interior ? interior : Mat("SpongeInterior",new Color(.13f,.025f,.27f)));
        }
        static void Kelp(Transform parent,Vector3 start,float height,int seed)
        {
            for(int i=0;i<7;i++)
            {
                float y=height*i/7; Vector3 p=start+new Vector3(Mathf.Sin(i*.65f+seed)*.24f,y,0);
                var leaf=Sphere("Kelp leaf",parent,p,new Vector3(.25f,height/5,.065f),lime);
                leaf.transform.localRotation=Quaternion.Euler(0,seed*31,Mathf.Sin(i+seed)*25);
            }
        }
        static void Star(Transform parent,Vector3 center,float radius,Material material)
        {
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5; Vector3 end=center+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius;
                Limb(parent,center,end,.15f,material);
            }
            Sphere("Starfish center",parent,center,new Vector3(.3f,.15f,.3f),material);
        }
    }
}
