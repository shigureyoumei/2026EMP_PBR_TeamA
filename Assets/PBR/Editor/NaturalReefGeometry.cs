using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBR.Editor
{
    // Deterministic geometry; no runtime mesh rebuilding or random decoration changes.
    public sealed class NaturalReefGeometry
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector3> normals=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public int Vertex(Vector3 p,Vector3 n,Vector2 tex,Color color)
        {int index=vertices.Count;vertices.Add(p);normals.Add(n.normalized);uv.Add(tex);colors.Add(color);return index;}
        public void Tri(int a,int b,int c,Vector3 outward)
        {
            if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),outward)<0){int t=b;b=c;c=t;}
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        public Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        public static float Noise(Vector3 p,int seed)=>Mathf.PerlinNoise(p.x+seed*.73f,p.y+p.z*.71f+seed*.19f);
        public void Ellipsoid(Vector3 center,Vector3 scale,int seed,bool irregular,Color color,int rows=24,int columns=40)
        {
            int start=vertices.Count;
            for(int j=0;j<=rows;j++)for(int i=0;i<=columns;i++)
            {
                float a=(float)i/columns*Mathf.PI*2,b=(float)j/rows*Mathf.PI;
                var n=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                float r=irregular? .83f+.22f*Noise(n*2.7f,seed)+.11f*Noise(n*7,seed+13)+.035f*Noise(n*23,seed+4):1;
                var p=Vector3.Scale(n,scale)*r+center;
                Vertex(p,new Vector3(n.x/scale.x,n.y/scale.y,n.z/scale.z),new Vector2((float)i/columns,(float)j/rows),color);
            }
            for(int j=0;j<rows;j++)for(int i=0;i<columns;i++)
            {
                int a=start+j*(columns+1)+i,b=a+columns+1;var outward=vertices[a]+vertices[b+1]-center*2;
                Tri(a,b,b+1,outward);Tri(a,b+1,a+1,outward);
            }
        }
        public void Tube(Vector3 a,Vector3 b,float ra,float rb,Color color,int sides=7)
        {
            var axis=(b-a).normalized;var right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.forward:Vector3.up).normalized;var up=Vector3.Cross(axis,right);int start=vertices.Count;
            for(int j=0;j<2;j++)for(int i=0;i<=sides;i++)
            {
                float angle=i*Mathf.PI*2/sides;var n=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                Vertex((j==0?a:b)+n*(j==0?ra:rb),n,new Vector2((float)i/sides,j),color);
            }
            for(int i=0;i<sides;i++){int c=start+i,d=c+sides+1;Tri(c,d,d+1,vertices[c]-a);Tri(c,d+1,c+1,vertices[c]-a);}
        }
        public static Mesh Rock(int seed)
        {var g=new NaturalReefGeometry();g.Ellipsoid(Vector3.zero,Vector3.one,seed,true,Color.white,32,48);var mesh=g.Finish("Eroded limestone "+seed);mesh.RecalculateNormals();return mesh;}
        public static Mesh Tunnel(bool closed,int seed)
        {
            var g=new NaturalReefGeometry();const int arc=72,depth=12;
            float ix=closed?2.4f:1.8f,iy=closed?3.3f:3.4f,ox=closed?3.4f:3.7f;
            Vector3 P(int a,int z,bool inner)
            {
                float angle=a*Mathf.PI/arc,d=-1.6f+3.2f*z/depth;
                var basic=new Vector3(Mathf.Cos(angle)*(inner?ix:ox),Mathf.Sin(angle)*(inner?iy:4.3f),d);
                float noise=(Noise(basic*1.4f,seed)-.5f)*(inner?.65f:1.05f);
                return basic+new Vector3(Mathf.Cos(angle)*noise,Mathf.Sin(angle)*noise,0);
            }
            for(int inner=0;inner<2;inner++)
            {
                int start=g.vertices.Count;
                for(int z=0;z<=depth;z++)for(int a=0;a<=arc;a++)
                {
                    var p=P(a,z,inner==1);var n=new Vector3(p.x,p.y,0).normalized*(inner==0?1:-1);
                    g.Vertex(p,n,new Vector2((float)a/arc,(float)z/depth),Color.white);
                }
                for(int z=0;z<depth;z++)for(int a=0;a<arc;a++)
                {int v=start+z*(arc+1)+a,w=v+arc+1;var n=new Vector3(g.vertices[v].x,g.vertices[v].y,0)*(inner==0?1:-1);g.Tri(v,w,w+1,n);g.Tri(v,w+1,v+1,n);}
            }
            foreach(int z in new[]{0,depth})for(int a=0;a<arc;a++)
            {
                Vector3 normal=z==0?Vector3.back:Vector3.forward;
                var points=new[]{P(a,z,false),P(a+1,z,false),P(a+1,z,true),P(a,z,true)};
                int start=g.vertices.Count;foreach(var p in points)g.Vertex(p,normal,new Vector2(p.x,p.y),Color.white);
                g.Tri(start,start+1,start+2,normal);g.Tri(start,start+2,start+3,normal);
                if(closed && z==depth)
                {int c=g.Vertex(new Vector3(0,0,1.6f),normal,Vector2.zero,Color.white);g.Tri(c,start+2,start+3,normal);}
            }
            var mesh=g.Finish(closed?"Weathered cave shell":"Weathered open arch");mesh.RecalculateNormals();return mesh;
        }
        public static Mesh Plants(int seed,bool cover)
        {
            var g=new NaturalReefGeometry();var rng=new System.Random(seed);int leaves=cover?56:26;
            for(int leaf=0;leaf<leaves;leaf++)
            {
                float theta=(float)rng.NextDouble()*Mathf.PI*2;
                var side=new Vector3(Mathf.Cos(theta),0,Mathf.Sin(theta));var forward=Vector3.Cross(Vector3.up,side);
                float h=(cover?3.2f:.55f)+(float)rng.NextDouble()*(cover?1.6f:1.35f);
                float width=(cover?.11f:.035f)+(float)rng.NextDouble()*(cover?.16f:.065f);
                var root=new Vector3(((float)rng.NextDouble()-.5f)*(cover?4.6f:1.4f),0,((float)rng.NextDouble()-.5f)*(cover?1.25f:1.4f));
                int start=g.vertices.Count;const int segments=14;
                float phase=(float)rng.NextDouble()*6;
                for(int j=0;j<=segments;j++)for(int across=0;across<3;across++)
                {
                    float t=(float)j/segments,x=across-1;
                    var center=root+Vector3.up*(h*t)+forward*(Mathf.Sin(t*2.4f+phase)-Mathf.Sin(phase))*t*.32f+side*Mathf.Sin(t*5+phase)*t*.16f;
                    var p=center+side*x*width*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.65f)+forward*(1-x*x)*width*.14f;
                    var n=(forward+Vector3.up*.12f+side*x*.3f).normalized;
                    g.Vertex(p,n,new Vector2(across*.5f,t),new Color(.65f+leaf%5*.07f,.8f,.65f,t));
                }
                for(int j=0;j<segments;j++)for(int a=0;a<2;a++){int v=start+j*3+a;g.Tri(v,v+3,v+4,forward);g.Tri(v,v+4,v+1,forward);}
            }
            return g.Finish(cover?"Layered ribbon kelp":"Curved seagrass meadow");
        }
        public static Mesh Coral(int seed,bool fan)
        {
            var g=new NaturalReefGeometry();var rng=new System.Random(seed);
            void Branch(Vector3 start,Vector3 direction,float length,float radius,int level)
            {
                var end=start+direction.normalized*length;g.Tube(start,end,radius,radius*.66f,new Color(.75f+level*.03f,.82f,.7f,0));
                if(level==0){g.Ellipsoid(end,Vector3.one*radius*1.1f,seed,false,Color.white,5,7);return;}
                for(int j=0;j<2;j++)
                {
                    var spread=new Vector3(((float)rng.NextDouble()-.5f)*1.4f,.15f+(float)rng.NextDouble()*.6f,fan?((float)rng.NextDouble()-.5f)*.14f:((float)rng.NextDouble()-.5f)*1.4f);
                    Branch(end,direction*.4f+spread,length*.71f,radius*.7f,level-1);
                }
            }
            int trunks=fan?5:7;
            for(int i=0;i<trunks;i++)
            {
                var direction=fan?new Vector3((i-2)*.7f,1.1f,0):new Vector3(Mathf.Sin(i*2.4f)*.6f,1,Mathf.Cos(i*2.4f)*.6f);
                Branch(Vector3.zero,direction,fan?.85f:.55f,fan?.06f:.065f,fan?6:4);
            }
            return g.Finish(fan?"Branching gorgonian":"Branching staghorn coral");
        }
        public static Mesh Fish(int species)
        {
            var g=new NaturalReefGeometry();const int rings=36,sides=32;
            float[] width={.22f,.13f,.12f,.16f,.20f,.13f,.19f,.16f};
            float[] height={.30f,.36f,.22f,.18f,.35f,.17f,.24f,.28f};
            for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            {
                float u=(float)j/rings,a=(float)i/sides*Mathf.PI*2;
                float profile=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.72f)*Mathf.Lerp(.35f,1,Mathf.SmoothStep(0,.38f,u));
                float w=width[species]*profile,h=height[species]*profile;
                Vector3 p=new Vector3(Mathf.Cos(a)*w,Mathf.Sin(a)*h,(u-.5f)*1.12f);
                var n=new Vector3(Mathf.Cos(a)/width[species],Mathf.Sin(a)/height[species],(u-.5f)*3).normalized;
                g.Vertex(p,n,new Vector2(u,(float)i/sides),new Color(.65f+.35f*(Mathf.Sin(a)*.5f+.5f),.8f,0,0));
            }
            for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;var n=g.vertices[a];n.z=0;g.Tri(a,b,b+1,n);g.Tri(a,b+1,a+1,n);}
            // Curved dorsal/anal fins and a forked caudal fin. The whole mesh bends in the shader.
            void Fin(Vector3[] points)
            {
                int center=g.Vertex(points[0],Vector3.right,Vector2.zero,new Color(.9f,.7f,1,0));
                for(int k=1;k<points.Length-1;k++)
                {
                    int a=g.Vertex(points[k],Vector3.right,new Vector2(k*.13f,.8f),new Color(.9f,.7f,1,0));
                    int b=g.Vertex(points[k+1],Vector3.right,new Vector2((k+1)*.13f,1),new Color(.9f,.7f,1,0));g.Tri(center,a,b,Vector3.right);
                }
            }
            Fin(new[]{new Vector3(0,0,-.49f),new Vector3(0,.23f,-.79f),new Vector3(0,.10f,-.72f),new Vector3(0,0,-.63f),new Vector3(0,-.10f,-.72f),new Vector3(0,-.23f,-.79f)});
            float dorsal=height[species]*(species==1 || species==4?1.65f:1.3f);
            Fin(new[]{new Vector3(0,height[species]*.65f,-.26f),new Vector3(0,dorsal,-.22f),new Vector3(0,dorsal*.95f,.02f),new Vector3(0,height[species]*.7f,.27f)});
            Fin(new[]{new Vector3(0,-height[species]*.6f,-.28f),new Vector3(0,-dorsal*.9f,-.24f),new Vector3(0,-height[species]*.8f,.23f)});
            foreach(int sign in new[]{-1,1})
            {
                Fin(new[]{new Vector3(sign*width[species]*.7f,-.02f,.15f),new Vector3(sign*(width[species]+.12f),-.12f,-.13f),new Vector3(sign*width[species]*.8f,-.08f,-.22f)});
                g.Ellipsoid(new Vector3(sign*width[species]*.67f,.065f,.36f),new Vector3(.017f,.025f,.025f),0,false,new Color(1,0,0,0),8,12);
            }
            return g.Finish(new[]{"Clownfish","Butterflyfish","Blue tang","Wrasse","Angelfish","Fusilier","Damselfish","Bannerfish"}[species]);
        }
    }
}
