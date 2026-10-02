using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PBR.Editor
{
    public static class DiverseReefFactory
    {
        const string Root="Assets/PBR/Generated360/Habitat";
        sealed class Geometry
        {
            public readonly List<Vector3> points=new List<Vector3>();
            readonly List<Vector3> normals=new List<Vector3>();
            readonly List<int> outside=new List<int>(),inside=new List<int>();
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool inner=false,bool doubleSided=false,Vector3? smoothNormal=null)
            {
                int i=points.Count;points.AddRange(new[]{a,b,c,d});var indices=inner?inside:outside;
                Vector3 normal=smoothNormal??Vector3.Cross(b-a,c-a).normalized;normals.AddRange(new[]{normal,normal,normal,normal});
                indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
                if(doubleSided) {int j=points.Count;points.AddRange(new[]{d,c,b,a});normals.AddRange(new[]{-normal,-normal,-normal,-normal});indices.AddRange(new[]{j,j+1,j+2,j,j+2,j+3});}
            }
            public Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.subMeshCount=2;
                mesh.SetTriangles(outside,0);mesh.SetTriangles(inside,1);mesh.SetNormals(normals);mesh.RecalculateBounds();return mesh;
            }
        }
        static Material Material(string name,Color color,bool stone=false)
        {
            string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) {mat=new Material(Shader.Find(stone?"PBR/Reef Stone":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);
            // Meshes already have separate front/back faces and normals. Cull backfaces to avoid coplanar lighting artifacts.
            if(!stone) {mat.SetFloat("_Cull",2);mat.SetFloat("_Smoothness",.35f);mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.08f);}
            EditorUtility.SetDirty(mat);return mat;
        }
        static Mesh Save(Mesh mesh,string name)
        {
            string path=Root+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) {EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static GameObject Object(Transform parent,string name,Mesh mesh,Material a,Material b,bool physical)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=mesh.subMeshCount==1?new[]{a}:new[]{a,b};
            if(physical) {go.layer=8;go.AddComponent<MeshCollider>().sharedMesh=mesh;}return go;
        }
        static Vector3 Arc(float angle,float rx,float ry,float z)=>new Vector3(Mathf.Cos(angle)*rx,Mathf.Sin(angle)*ry,z);
        static Mesh Tunnel(bool closed,int variant)
        {
            var g=new Geometry();const int n=24;
            float ix=closed?2.4f:1.8f,iy=closed?3.3f:3.4f,ox=closed?3.4f:3.7f,oy=4.3f;
            float front=-1.6f,back=1.6f;
            for(int j=0;j<n;j++)
            {
                float a=j*Mathf.PI/n,b=(j+1)*Mathf.PI/n;
                // Irregular outer silhouette, with a smooth passable interior and real open entrance.
                float ra=1+.045f*Mathf.Sin(j*2.1f+variant),rb=1+.045f*Mathf.Sin((j+1)*2.1f+variant);
                var af=Arc(a,ox*ra,oy*ra,front);var bf=Arc(b,ox*rb,oy*rb,front);var ab=Arc(a,ox*ra,oy*ra,back);var bb=Arc(b,ox*rb,oy*rb,back);
                var iaf=Arc(a,ix,iy,front);var ibf=Arc(b,ix,iy,front);var iab=Arc(a,ix,iy,back);var ibb=Arc(b,ix,iy,back);
                g.Quad(af,bf,bb,ab,false,true);g.Quad(iaf,iab,ibb,ibf,true,true);
                g.Quad(af,iaf,ibf,bf,false,true);g.Quad(ab,bb,ibb,iab,false,true);
                if(closed) g.Quad(new Vector3(0,0,back),iab,ibb,new Vector3(0,0,back),true,true);
            }
            return Save(g.Mesh(closed?"Cave shell":"Open natural arch"),(closed?"Cave":"Arch")+variant);
        }
        static Mesh Kelp(int variant,bool broad)
        {
            var g=new Geometry();var rng=new System.Random(700+variant);
            int blades=broad?22:12;
            for(int leaf=0;leaf<blades;leaf++)
            {
                int column=leaf%(blades/2),row=leaf/(blades/2);
                float x=(column-(blades/2-1)*.5f)*(broad?.4f:.28f);
                float height=(broad?3.7f:1.5f)+(float)rng.NextDouble()*(broad?1.1f:1.4f);
                float phase=(float)rng.NextDouble()*6.28f;
                for(int segment=0;segment<9;segment++)
                {
                    float t0=segment/9f,t1=(segment+1)/9f;
                    Vector3 c0=new Vector3(x+Mathf.Sin(t0*4+phase)*t0*.32f,t0*height,row*.36f+Mathf.Sin(t0*5+phase)*.16f);
                    Vector3 c1=new Vector3(x+Mathf.Sin(t1*4+phase)*t1*.32f,t1*height,row*.36f+Mathf.Sin(t1*5+phase)*.16f);
                    float w0=(broad?.36f:.13f)*(.08f+.92f*Mathf.Sin(t0*Mathf.PI));
                    float w1=(broad?.36f:.13f)*(.08f+.92f*Mathf.Sin(t1*Mathf.PI));
                    g.Quad(c0-Vector3.right*w0,c1-Vector3.right*w1,c1+Vector3.right*w1,c0+Vector3.right*w0,leaf%3==0,true,Vector3.back);
                }
            }
            return Save(g.Mesh("Layered kelp blades"),(broad?"KelpCover":"Grass")+variant);
        }
        static Mesh Fan(int variant)
        {
            var g=new Geometry();const int count=48;var center=new Vector3(0,.35f,0);
            for(int i=0;i<count;i++)
            {
                float a=.12f+i*(Mathf.PI-.24f)/count,b=.12f+(i+1)*(Mathf.PI-.24f)/count;
                Vector3 Edge(float angle) {float r=1+.065f*Mathf.Sin(angle*19+variant);return center+new Vector3(Mathf.Cos(angle)*2.35f*r,Mathf.Sin(angle)*3.5f*r,.15f*Mathf.Sin(angle*3));}
                g.Quad(center,Edge(a),Edge(b),center,false,true,Vector3.back);
                if(i%3==0)
                {
                    var tip=Edge(a);Vector3 line=tip-center;Vector3 side=Vector3.Cross(line,Vector3.forward).normalized*.022f;
                    var start=center+Vector3.back*.18f;var end=tip+Vector3.back*.18f;
                    g.Quad(start-side,end-side,end+side,start+side,true,true,Vector3.back);
                    for(int j=1;j<4;j++)
                    {
                        Vector3 branch=Vector3.Lerp(start,end,j*.21f),finish=Vector3.Lerp(start,end,j*.21f+.16f)+Vector3.right*(i%2==0?1:-1)*.25f;
                        g.Quad(branch-side,finish-side,finish+side,branch+side,true,true,Vector3.back);
                    }
                }
            }
            return Save(g.Mesh("Scalloped fan coral"),"Fan"+variant);
        }
        static Mesh Ridge(int variant)
        {
            var g=new Geometry();var rng=new System.Random(830+variant);var heights=new float[13];
            for(int i=0;i<heights.Length;i++) heights[i]=3+(float)rng.NextDouble()*4;
            for(int x=0;x<12;x++)
            {
                float left=x-6,top=heights[x],next=heights[x+1];
                g.Quad(new Vector3(left,0,0),new Vector3(left,top,.6f),new Vector3(left+1,next,.7f),new Vector3(left+1,0,0),false,true);
                g.Quad(new Vector3(left,top,.6f),new Vector3(left,0,4),new Vector3(left+1,0,4),new Vector3(left+1,next,.7f),false,true);
            }
            return Save(g.Mesh("Layered distant escarpment"),"Ridge"+variant);
        }
        public static void Build(Transform root,RingHabitat habitat,ExperimentConfig config)
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var limestone=Material("WarmLimestone",new Color(.4f,.48f,.35f),true);
            var slate=Material("BlueSlate",new Color(.16f,.31f,.4f),true);
            var caveInterior=Material("DarkCaveLining",new Color(.035f,.065f,.085f));
            var jade=Material("JadeKelp",new Color(.08f,.47f,.12f));var goldGreen=Material("OliveKelp",new Color(.38f,.64f,.085f));
            var purple=Material("PurpleFan",new Color(.61f,.11f,.68f));var coral=Material("CoralFan",new Color(1,.28f,.12f));
            var pinkVeins=Material("PinkVeins",new Color(.96f,.38f,.68f));var goldVeins=Material("GoldVeins",new Color(1,.58f,.24f));
            var covers=new List<ReefCover>();var transforms=new List<Transform>();
            ReefCoverKind[] kinds={ReefCoverKind.Arch,ReefCoverKind.Kelp,ReefCoverKind.Rock,ReefCoverKind.Cave,ReefCoverKind.FanCoral,ReefCoverKind.Kelp,ReefCoverKind.Rock,ReefCoverKind.Arch,ReefCoverKind.Kelp,ReefCoverKind.Cave,ReefCoverKind.Rock,ReefCoverKind.FanCoral,ReefCoverKind.Kelp,ReefCoverKind.Arch,ReefCoverKind.Rock,ReefCoverKind.Cave};
            for(int i=0;i<kinds.Length;i++)
            {
                var kind=kinds[i];float angle=(i+.35f)*Mathf.PI*2/16;
                float radius=i%2==0?13.3f:18.2f;
                Mesh mesh;Material a=limestone,b=slate;
                if(kind==ReefCoverKind.Cave || kind==ReefCoverKind.Arch) {mesh=Tunnel(kind==ReefCoverKind.Cave,i);a=i%2==0?limestone:slate;b=caveInterior;}
                else if(kind==ReefCoverKind.Kelp) {mesh=Kelp(i,true);a=jade;b=goldGreen;}
                else if(kind==ReefCoverKind.FanCoral) {mesh=Fan(i);a=i%2==0?coral:purple;b=i%2==0?goldVeins:pinkVeins;}
                else mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/PBR/Generated360/RockShape"+i+".asset");
                var go=Object(root,kind+" habitat "+i,mesh,a,b,true);go.transform.position=RingHabitat.Polar(angle,radius,-.2f);go.transform.rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                var site=go.AddComponent<ReefCover>();site.kind=kind;var candidates=new List<Vector3>();
                if(kind==ReefCoverKind.Rock)
                {
                    go.transform.localScale=new Vector3(2.9f,3.2f,2.8f);go.transform.position+=Vector3.up*1.6f;
                    for(int j=0;j<5;j++) candidates.Add(new Vector3((j-2)*.12f,.06f,1.05f+j*.08f));
                }
                else
                {
                    for(int j=0;j<5;j++) for(int k=0;k<3;k++)
                    {
                        float x=kind==ReefCoverKind.Arch?(j%2==0?-2.5f:2.5f):((j-2)*.38f);
                        float y=kind==ReefCoverKind.FanCoral?2.1f:1.7f+k*.16f;
                        float z=kind==ReefCoverKind.Cave || kind==ReefCoverKind.Arch?3.0f+k*.3f:1.65f+k*.25f;
                        candidates.Add(new Vector3(x,y,z));
                    }
                }
                site.hidingCandidates=candidates.ToArray();covers.Add(site);transforms.Add(go.transform);
            }
            habitat.covers=covers.ToArray();habitat.rocks=transforms.ToArray();
            var detail=root.gameObject.AddComponent<ReefEnvironmentDetail>();detail.config=config;
            detail.mediumDetails=new GameObject("Medium background detail");detail.mediumDetails.transform.SetParent(root,false);
            detail.richDetails=new GameObject("Rich background detail");detail.richDetails.transform.SetParent(root,false);
            var grassMeshes=new[]{Kelp(0,false),Kelp(1,false),Kelp(2,false)};
            var fanMeshes=new[]{Fan(0),Fan(1)};
            for(int i=0;i<40;i++)
            {
                float angle=(i+.3f)*Mathf.PI*2/40;float radius=i%3==0?8.3f:26+i%4*2;
                var parent=i%2==0?detail.mediumDetails.transform:detail.richDetails.transform;
                var grass=Object(parent,"Seagrass bed "+i,grassMeshes[i%3],jade,goldGreen,false);
                grass.transform.position=RingHabitat.Polar(angle,radius,-.2f);grass.transform.rotation=Quaternion.Euler(0,i*67,0);grass.transform.localScale=Vector3.one*(i%3==0?.45f:1.5f);
                if(i%4==0)
                {
                    var garden=new GameObject("Colorful coral garden "+i).transform;garden.SetParent(parent,false);garden.position=RingHabitat.Polar(angle+.09f,radius+1,-.2f);
                    ReefArtFactory.RingGarden(garden,900+i,i%3==0?.55f:1.1f);
                }
            }
            for(int i=0;i<10;i++)
            {
                float angle=(i+.4f)*Mathf.PI/5;
                var ridge=Object(root,"Distant canyon wall "+i,Ridge(i),i%2==0?limestone:slate,slate,false);
                ridge.transform.position=RingHabitat.Polar(angle,38+i%3*4,-.3f);ridge.transform.rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
                var fan=Object(detail.richDetails.transform,"Distant coral fan "+i,fanMeshes[i%2],i%2==0?purple:coral,i%2==0?pinkVeins:goldVeins,false);
                fan.transform.position=RingHabitat.Polar(angle+.15f,29,-.2f);fan.transform.rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg+25,0);fan.transform.localScale=Vector3.one*1.5f;
            }
            // Low-relief shells and branching coral add local visual search clutter without false targets.
            var shellMat=Material("IvoryShell",new Color(.89f,.79f,.55f));
            for(int i=0;i<20;i++)
            {
                float angle=i*2.399963f;var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Seabed shell "+i;
                UnityEngine.Object.DestroyImmediate(shell.GetComponent<Collider>());shell.transform.SetParent(detail.richDetails.transform,false);
                shell.transform.position=RingHabitat.Polar(angle,7+i%5*3,-.05f);shell.transform.localScale=new Vector3(.5f,.18f,.75f);shell.transform.rotation=Quaternion.Euler(0,i*71,0);shell.GetComponent<Renderer>().sharedMaterial=shellMat;
            }
        }
    }
}
