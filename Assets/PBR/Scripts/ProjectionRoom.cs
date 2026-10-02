using UnityEngine;

namespace PBR
{
    [ExecuteAlways]
    public sealed class ProjectionRoom : MonoBehaviour
    {
        [Min(1)] public float roomWidth = 25;
        [Min(1)] public float roomDepth = 15;
        [Min(1)] public float roomHeight = 7.7f;
        [Min(.1f)] public float eyeHeight = 1.6f;
        public Vector2 testerOffset;
        [Min(0)] public float projectionBottom = 0;
        [Min(0)] public float projectionTopMargin = 0;
        public bool showRoomGuides = true;
        [HideInInspector] public Vector3 virtualEye = new Vector3(0,3,0);
        [HideInInspector] public FourWallProjection projection;
        public Vector3 PhysicalEye => new Vector3(testerOffset.x,eyeHeight,testerOffset.y);
        public Vector3 ToVirtual(Vector3 physicalPoint) => physicalPoint-PhysicalEye+virtualEye;
        public float ProjectionTop => roomHeight-projectionTopMargin;
        public void Sanitize()
        {
            roomWidth=Mathf.Max(1,roomWidth);roomDepth=Mathf.Max(1,roomDepth);roomHeight=Mathf.Max(1,roomHeight);
            eyeHeight=Mathf.Clamp(eyeHeight,.1f,roomHeight-.1f);
            testerOffset.x=Mathf.Clamp(testerOffset.x,-roomWidth*.5f+.2f,roomWidth*.5f-.2f);
            testerOffset.y=Mathf.Clamp(testerOffset.y,-roomDepth*.5f+.2f,roomDepth*.5f-.2f);
            projectionBottom=Mathf.Clamp(projectionBottom,0,roomHeight-.2f);
            projectionTopMargin=Mathf.Clamp(projectionTopMargin,0,roomHeight-projectionBottom-.2f);
        }
        void OnValidate() { Sanitize(); }
        void OnEnable() { Sanitize();Apply(); }
        void Update() { Apply(); }
        public void Apply()
        {
            if(!projection || projection.walls==null) return;
            for(int i=0;i<Mathf.Min(4,projection.walls.Length);i++) if(projection.walls[i]) ApplyCamera(i,projection.walls[i]);
        }
        public void Wall(int index,out Vector3 center,out Vector3 right,out Vector3 outward,out float width)
        {
            var rotation=Quaternion.Euler(0,index*90,0);right=rotation*Vector3.right;outward=rotation*Vector3.forward;
            width=index%2==0?roomWidth:roomDepth;
            float distance=index%2==0?roomDepth*.5f:roomWidth*.5f;
            center=outward*distance+Vector3.up*((projectionBottom+ProjectionTop)*.5f);
        }
        public void ApplyCamera(int index,Camera camera)
        {
            Sanitize();Wall(index,out var center,out var right,out var outward,out float width);
            Vector3 relative=center-PhysicalEye;
            float distance=Vector3.Dot(relative,outward), near=camera.nearClipPlane;
            float horizontal=Vector3.Dot(relative,right);
            camera.transform.SetPositionAndRotation(virtualEye,Quaternion.LookRotation(outward,Vector3.up));
            camera.projectionMatrix=Matrix4x4.Frustum((horizontal-width*.5f)*near/distance,(horizontal+width*.5f)*near/distance,
                (projectionBottom-eyeHeight)*near/distance,(ProjectionTop-eyeHeight)*near/distance,near,camera.farClipPlane);
        }
        // Inputs/outputs are in physical room coordinates: origin is the center of the floor, meters.
        public bool TryWallHit(Ray ray,out int wall,out Vector3 point,out Vector2 uv)
        {
            wall=-1;point=Vector3.zero;uv=Vector2.zero;float best=float.PositiveInfinity;
            for(int i=0;i<4;i++)
            {
                Wall(i,out var center,out var right,out var normal,out float width);
                float denominator=Vector3.Dot(ray.direction,normal);
                if(Mathf.Abs(denominator)<.00001f) continue;
                float t=Vector3.Dot(center-ray.origin,normal)/denominator;
                if(t<0 || t>=best) continue;
                var hit=ray.GetPoint(t);float u=Vector3.Dot(hit-center,right)/width+.5f;
                float v=(hit.y-projectionBottom)/(ProjectionTop-projectionBottom);
                if(u<-.0001f || u>1.0001f || v<-.0001f || v>1.0001f) continue;
                best=t;wall=i;point=hit;uv=new Vector2(Mathf.Clamp01(u),Mathf.Clamp01(v));
            }
            return wall>=0;
        }
        void OnDrawGizmos()
        {
            if(!showRoomGuides) return;
            var colors=new[]{Color.cyan,Color.green,new Color(1,.65f,.1f),Color.magenta};
            for(int i=0;i<4;i++)
            {
                Wall(i,out var center,out var right,out _,out float width);
                Vector3 a=center-right*width*.5f,b=center+right*width*.5f;
                Vector3 floorA=a;floorA.y=0;Vector3 floorB=b;floorB.y=0;
                Vector3 roofA=a;roofA.y=roomHeight;Vector3 roofB=b;roofB.y=roomHeight;
                Gizmos.color=new Color(.5f,.6f,.65f,.5f);
                Gizmos.DrawLine(ToVirtual(floorA),ToVirtual(floorB));Gizmos.DrawLine(ToVirtual(roofA),ToVirtual(roofB));Gizmos.DrawLine(ToVirtual(floorA),ToVirtual(roofA));
                a.y=b.y=projectionBottom;Vector3 c=b,d=a;c.y=d.y=ProjectionTop;
                Gizmos.color=colors[i];Gizmos.DrawLine(ToVirtual(a),ToVirtual(b));Gizmos.DrawLine(ToVirtual(b),ToVirtual(c));Gizmos.DrawLine(ToVirtual(c),ToVirtual(d));Gizmos.DrawLine(ToVirtual(d),ToVirtual(a));
            }
            Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(virtualEye,.12f);
            Vector3 feet=PhysicalEye;feet.y=0;Gizmos.DrawLine(ToVirtual(feet),virtualEye);
            Gizmos.DrawLine(virtualEye,virtualEye+Vector3.forward*.6f);
        }
    }
}
