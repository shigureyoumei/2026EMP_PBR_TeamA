using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PBR
{
    public sealed class RingReefValidation : MonoBehaviour
    {
        int assertions, hides, seams, states;
        string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-ring-smoke") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-ring-preview") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-room-smoke") >= 0)
                new GameObject("360 validation").AddComponent<RingReefValidation>();
        }
        void Check(bool ok,string reason) { assertions++;if(!ok) throw new Exception(reason); }
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation360"));Directory.CreateDirectory(folder);
            yield return null;
            var session=FindAnyObjectByType<FishExperiment>();var habitat=FindAnyObjectByType<RingHabitat>();var projection=FindAnyObjectByType<FourWallProjection>();
            bool roomSmoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-room-smoke")>=0;
            if(roomSmoke)
            {
                try
                {
                    Check(projection.room && projection.room.roomWidth==25 && projection.room.roomDepth==15 && Mathf.Abs(projection.room.roomHeight-7.7f)<.001f,"Published room preset missing");
                    Check(session.Fish.Count==session.config.fishCount,"Fixed school missing");
                    projection.Apply();
                    for(int i=0;i<4;i++)
                    {
                        projection.room.Wall(i,out var center,out var right,out _,out float width);
                        var edge=projection.room.ToVirtual(center+right*width*.5f);
                        var a=projection.walls[i].WorldToViewportPoint(edge);var b=projection.walls[(i+1)%4].WorldToViewportPoint(edge);
                        Check(Mathf.Abs(a.x-1)<.001f && Mathf.Abs(b.x)<.001f && Mathf.Abs(a.y-b.y)<.001f,"Physical room seam mismatch");
                    }
                    File.WriteAllText(Path.Combine(folder,"results.txt"),"PASS: published room preset, configured fish population and four physical wall seams.\n");
                }
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e);Application.Quit(1);yield break;}
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-ring-smoke")>=0)
            {
                try
                {
                    Check(session.config.ringWorld && session.Fish.Count==session.config.fishCount,"Ring population");
                    var originals=session.Fish.ToArray();
                    Check(habitat.rocks.Length==16,"Distributed cover missing");
                    if(habitat.covers!=null && habitat.covers.Length>0)
                    {
                        Check(habitat.covers.Select(c=>c.kind).Distinct().Count()==5,"Missing cover diversity");
                        foreach(ReefCoverKind kind in Enum.GetValues(typeof(ReefCoverKind)))
                        {
                            bool reachable=false;
                            foreach(var cover in habitat.covers.Where(c=>c.kind==kind)) foreach(var local in cover.hidingCandidates)
                            {
                                var position=cover.transform.TransformPoint(local);
                                if(habitat.Free(position) && habitat.FullyOccluded(position) && habitat.Plan(session.Fish[0].transform.position,position,new List<Vector3>())) reachable=true;
                            }
                            Check(reachable,"No reachable fully hidden position for "+kind);
                        }
                        foreach(var arch in habitat.covers.Where(c=>c.kind==ReefCoverKind.Arch))
                            Check(habitat.Clear(arch.transform.TransformPoint(new Vector3(0,1.6f,-3)),arch.transform.TransformPoint(new Vector3(0,1.6f,3))),"Arch opening is blocked by an invisible collider");
                    }
                    foreach(int mode in new[]{0,1,5})
                    {
                        projection.previewMode=mode;projection.Apply();
                        for(int i=0;i<4;i++)
                        {
                            Vector3 boundary=habitat.Eye+RingHabitat.Polar((i*90+45)*Mathf.Deg2Rad,20,0);
                            if(projection.room){projection.room.Wall(i,out var center,out var right,out _,out float width);boundary=projection.room.ToVirtual(center+right*width*.5f);}
                            var a=projection.walls[i].WorldToViewportPoint(boundary);var b=projection.walls[(i+1)%4].WorldToViewportPoint(boundary);
                            Check(Mathf.Abs(a.x-1)<.001f && Mathf.Abs(b.x)<.001f && Mathf.Abs(a.y-b.y)<.001f,"Projection seam mismatch including wrap seam");
                        }
                    }
                    projection.previewMode=0;projection.Apply();
                    var path=new List<Vector3>();
                    for(int i=0;i<48;i++)
                        Check(habitat.Plan(RingHabitat.Polar(i*Mathf.PI/24,10,4),RingHabitat.Polar((i+1)*Mathf.PI/24,10,4),path),"Broken ring route");
                    session.Begin();Check(session.Running,"Block start failed");
                    foreach(var f in session.Fish) f.Roaming.SetState(FishMotionState.Hiding);
                    var seenHiding=new HashSet<int>();
                    var coverKinds=new HashSet<string>();
                    float minRadius=100,maxRadius=0,minY=100,maxY=0;
                    const float dt=1f/60;
                    for(int step=0;step<18000;step++) foreach(var f in session.Fish)
                    {
                        var old=f.transform.position;var rot=f.transform.rotation;
                        float oldYaw=Mathf.Atan2(old.x,old.z)*Mathf.Rad2Deg;
                        f.Roaming.Tick(dt);
                        var p=f.transform.position;
                        Check(Vector3.Distance(old,p)<=session.config.burstSpeed*dt+.001f,"Position teleport");
                        Check(Quaternion.Angle(rot,f.transform.rotation)<=RingFishMotion.TurnDegreesPerSecond*dt+.1f,"Instant turn");
                        Check(habitat.Free(p),"Fish intersects reef or leaves water volume");
                        Check(f.gameObject.activeInHierarchy,"Fish vanished");
                        if((p-old).sqrMagnitude>.00001f) Check(Vector3.Dot((p-old).normalized,f.transform.forward)>.995f,"Sideways swimming");
                        float yaw=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;
                        int oldWall=(Mathf.FloorToInt((oldYaw+45)/90)%4+4)%4;
                        int newWall=(Mathf.FloorToInt((yaw+45)/90)%4+4)%4;
                        if(oldWall!=newWall) seams++;
                        states|=1<<(int)f.Roaming.State;
                        if(f.Roaming.HiddenAtRest) { Check(habitat.FullyOccluded(p),"False hiding");seenHiding.Add(f.Id);coverKinds.Add(f.Roaming.CoverKind);hides++; }
                        minRadius=Mathf.Min(minRadius,RingHabitat.Radius(p));maxRadius=Mathf.Max(maxRadius,RingHabitat.Radius(p));minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);
                    }
                    Check(states==15,"Missing behavior state");Check(seenHiding.Count>=5,"Too few fish reached physical hiding places: "+seenHiding.Count);
                    Check(seams>20,"Insufficient wall boundary crossings");
                    Check(minRadius<12 && maxRadius>20 && maxY-minY>3,"Insufficient 3D depth travel");
                    if(habitat.covers!=null && habitat.covers.Length>0) Check(coverKinds.Count==5,"Not all cover types were used: "+string.Join(",",coverKinds));
                    session.End("ring_validation");session.Begin();Check(session.Fish.SequenceEqual(originals),"Population replaced on restart");session.End("ring_validation_done");
                    File.WriteAllText(Path.Combine(folder,"results.txt"),"PASS: "+assertions+" assertions; 300 simulated seconds; "+seams+" wall crossings; "+seenHiding.Count+" fish physically hid.\nCover types used: "+string.Join(",",coverKinds)+"\nRadius range: "+minRadius+" to "+maxRadius+"; height range: "+minY+" to "+maxY+".\nFour camera seams (including 360 wrap), collision clearance, smooth turns, forward propulsion, all four states, stable population verified.\n");
                }
                catch(Exception e) { File.WriteAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e);Debug.LogException(e);Application.Quit(1);yield break; }
            }
            yield return new WaitForSecondsRealtime(2);
            if(roomSmoke || Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-ring-smoke")>=0)
            {
                session.input.DesktopInput=false;session.config.durationSeconds=10;session.Begin();
                yield return new WaitForSecondsRealtime(11);
                try
                {
                    Check(!session.Running && session.Fish.Count==session.config.fishCount,"Normal timed block changed population");
                    File.AppendAllText(Path.Combine(folder,"results.txt"),"PASS: normal rendered player loop and timed block completion. Log: "+session.LastLogFolder+"\n");
                }
                catch(Exception e) {File.AppendAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e);Application.Quit(1);yield break;}
            }
            session.enabled=false;
            // Preserve physical screen proportions and pad each image to a common preview tile.
            var images=new List<Texture2D>();
            foreach(var cam in projection.walls)
            {
                int renderWidth=1280,renderHeight=720;
                if(projection.room)
                {
                    int index=Array.IndexOf(projection.walls,cam);projection.room.Wall(index,out _,out _,out _,out float width);
                    float aspect=width/(projection.room.ProjectionTop-projection.room.projectionBottom);
                    if(aspect>1280f/720)renderHeight=Mathf.RoundToInt(1280/aspect);else renderWidth=Mathf.RoundToInt(720*aspect);
                }
                var target=new RenderTexture(renderWidth,renderHeight,24,RenderTextureFormat.ARGB32);target.Create();
                projection.enabled=false;cam.enabled=true;cam.rect=new Rect(0,0,1,1);cam.aspect=16f/9;cam.fieldOfView=58.7155f;
                for(int i=0;i<3;i++) { RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=target});yield return null; }
                var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.SetPixels(new Color[1280*720]);image.ReadPixels(new Rect(0,0,renderWidth,renderHeight),(1280-renderWidth)/2,(720-renderHeight)/2);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,cam.name.Split(' ')[0]+".png"),image.EncodeToPNG());images.Add(image);
                RenderTexture.active=previous;target.Release();Destroy(target);
            }
            var grid=new Texture2D(2560,1440,TextureFormat.RGB24,false);
            for(int i=0;i<4;i++) grid.SetPixels((i%2)*1280,i<2?720:0,1280,720,images[i].GetPixels());grid.Apply();
            File.WriteAllBytes(Path.Combine(folder,"FourWalls.png"),grid.EncodeToPNG());
            Destroy(grid);foreach(var image in images) Destroy(image);
            Application.Quit(0);
        }
    }
}
