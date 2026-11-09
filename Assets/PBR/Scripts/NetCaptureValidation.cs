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
    public sealed class NetCaptureValidation : MonoBehaviour
    {
        FishExperiment session;
        RingHabitat habitat;
        FourWallProjection projection;
        NetCaptureSystem nets;
        Ray aim;
        string folder;
        int checks;
        FishTarget[] staged;
        Camera view;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-net-smoke")>=0)new GameObject("Net capture validation").AddComponent<NetCaptureValidation>();}
        void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
        bool Run(Action action)
        {
            try{action();return true;}
            catch(Exception e){File.AppendAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e+"\n");Debug.LogException(e);Application.Quit(1);return false;}
        }
        void Stage()
        {
            staged=session.Fish.ToArray();Vector3 hidden=Vector3.zero;bool found=false;
            foreach(var cover in habitat.covers.Reverse())
            {
                if(cover.hidingCandidates==null)continue;
                foreach(var local in cover.hidingCandidates)
                {
                    var p=cover.transform.TransformPoint(local);var direction=(p-session.input.Origin).normalized;
                    if(!habitat.Free(p) || !habitat.FullyOccluded(p) || Physics.Raycast(session.input.Origin,direction,11,1<<8,QueryTriggerInteraction.Ignore))continue;
                    hidden=p;found=true;break;
                }
                if(found)break;
            }
            Check(found,"Could not find a controlled real-cover test location");
            aim=new Ray(session.input.Origin,(hidden-session.input.Origin).normalized);
            Vector3 right=Vector3.Cross(Vector3.up,aim.direction).normalized,up=Vector3.Cross(aim.direction,right).normalized,center=aim.GetPoint(10);
            for(int i=0;i<staged.Length;i++)staged[i].transform.position=session.input.Origin-aim.direction*21+right*(i%5*.12f);
            staged[0].transform.position=center+right*.5f+up*.25f;
            staged[1].transform.position=center-right*.5f+up*.25f;
            staged[2].transform.position=center+up*.7f;
            staged[3].transform.position=center+right*4;
            staged[4].transform.position=hidden;
            Physics.SyncTransforms();projection.previewMode=0;projection.Apply();
            view=projection.walls.First(c=>{var v=c.WorldToViewportPoint(center);return v.z>0 && v.x>.02f && v.x<.98f && v.y>.02f && v.y<.98f;});
        }
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../ValidationNet"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"results.txt"),"");
            yield return null;
            session=FindAnyObjectByType<FishExperiment>();habitat=FindAnyObjectByType<RingHabitat>();projection=FindAnyObjectByType<FourWallProjection>();
            if(!Run(()=>
            {
                Check(session.Fish.Count==50 && session.config.netCaptureEnabled && session.config.netMaterial,"Net configuration or initial fifty-fish population");
                session.enabled=false;session.input.DesktopInput=false;session.pairId="automated_net_validation_not_participant";
                foreach(var fish in session.Fish){fish.Roaming.SetState(FishMotionState.Stationary);fish.Roaming.Tick(1.05f);fish.Roaming.Tick(1.05f);Check(fish.Roaming.State!=FishMotionState.Stationary,"Hover timer must advance with real elapsed time");}
                session.config.maxHoverSeconds=.1f;session.Fish[0].Roaming.SetState(FishMotionState.Stationary);session.Fish[0].Roaming.Tick(.06f);session.Fish[0].Roaming.Tick(.06f);
                Check(session.Fish[0].Roaming.State!=FishMotionState.Stationary,"A configured shorter hover limit was ignored");session.config.maxHoverSeconds=2;
                var rest=new Dictionary<int,float>();foreach(var fish in session.Fish){rest[fish.Id]=0;fish.Roaming.SetState(FishMotionState.Hiding);}
                int arrivals=0;
                for(int step=0;step<7200;step++)foreach(var fish in session.Fish)
                {
                    bool before=fish.Roaming.HiddenAtRest;fish.Roaming.Tick(1f/60);
                    bool stopped=fish.Roaming.HiddenAtRest || fish.Roaming.State==FishMotionState.Stationary && fish.Roaming.Speed<.01f;
                    rest[fish.Id]=stopped?rest[fish.Id]+1f/60:0;
                    Check(rest[fish.Id]<=2.04f,"Hover exceeded two seconds: "+fish.Id+" "+rest[fish.Id]);
                    if(!before && fish.Roaming.HiddenAtRest)arrivals++;
                }
                Check(arrivals>=10,"Too few physical hiding arrivals to exercise rest duration");
                // Verify exact pointer mapping in all six desktop layouts and each visible wall.
                for(int mode=0;mode<=5;mode++)
                {
                    projection.previewMode=mode;projection.Apply();
                    foreach(var camera in projection.walls)if(camera.enabled)
                    {
                        var pixel=camera.pixelRect.center;Check(session.input.TryPointerRay(pixel,out var pointer),"Pointer does not map to visible wall");
                        var projected=camera.WorldToScreenPoint(pointer.GetPoint(10));Check(Vector2.Distance(pixel,new Vector2(projected.x,projected.y))<.1f,"Pointer/ray projection mismatch");
                    }
                }
                Check(!session.input.TryPointerRay(new Vector2(-20,-20),out _),"Clicks outside a wall must not launch a net");
                session.input.SubmitOrientation(0,15,5);session.input.SubmitButton(0);var original=session.input.Aim(0);session.input.SubmitOrientation(0,90,-5);
                Check(session.input.ConsumeShot(0,out var frozen) && Vector3.Angle(original.direction,frozen.direction)<.001f,"Click direction must freeze at button press");session.input.Source[0]="desktop";
                session.Begin();Stage();session.input.SubmitOrientation(0,Mathf.Atan2(aim.direction.x,aim.direction.z)*Mathf.Rad2Deg,Mathf.Asin(aim.direction.y)*Mathf.Rad2Deg);session.input.Source[0]="desktop";
                session.Shoot(0,session.Elapsed);nets=session.Nets;Check(nets && nets.ActiveNetCount==1,"A click must launch one white net");nets.enabled=false;
                nets.Tick(session.config.netFlightSeconds*.6f);Check(nets.PendingFishCount==0 && session.Fish.Count==50,"Capture happened before net arrived");
            }))yield break;
            yield return Capture("01_launch.png");
            if(!Run(()=>
            {
                nets.Tick(session.config.netFlightSeconds*.4f+.08f);
                Check(nets.PendingFishCount==3 && staged.Take(3).All(f=>f.IsBeingCaptured),"Area net did not catch A/B/distractor together");
                Check(!staged[3].IsBeingCaptured && !staged[4].IsBeingCaptured,"Outside/rock-occluded fish was captured");
                Check(session.Fish.Count==50 && session.Captured==0,"Count changed before fish disappeared");
                Check(staged.Take(3).All(f=>!f.GetComponent<Collider>().enabled),"Captured fish collider remained active");
                Check(staged.Take(3).Any(f=>Quaternion.Angle(f.transform.Find("Animated fish").localRotation,Quaternion.identity)>1),"No struggle motion");
            }))yield break;
            yield return Capture("02_struggle.png");
            if(!Run(()=>
            {
                nets.Launch(aim,1);nets.Tick(session.config.netFlightSeconds+.02f);Check(nets.PendingFishCount==3,"Second net captured a reserved fish again");
                nets.Tick(session.config.netStruggleSeconds+.5f);
                Check(session.Fish.Count==47 && session.Captured==3,"Three disappearing fish must change count from 50 to 47");
                Check(staged.Take(3).All(f=>!f.gameObject.activeSelf),"Caught fish remained active after struggle");
                Check(staged[3].gameObject.activeSelf && staged[4].gameObject.activeSelf,"Uncaught fish disappeared");
                session.End("area_capture_test");session.Begin();Check(session.Fish.Count==47,"Block restart replenished caught fish");session.End("restart_test");
            }))yield break;
            yield return Capture("03_remaining.png");
            if(!Run(()=>
            {
                // Explicit role filtering remains available for the later research protocol.
                session.ResetSchool();session.config.netRespectsRoles=true;session.condition=ExperimentCondition.FixedRole;session.Begin();Stage();
                nets.Launch(aim,0);nets.Tick(session.config.netFlightSeconds+.01f);Check(nets.PendingFishCount==1 && staged[0].IsBeingCaptured && !staged[1].IsBeingCaptured && !staged[2].IsBeingCaptured,"Optional role filter failed");
                session.End("cancel_during_struggle");Check(staged.All(f=>!f.IsBeingCaptured) && staged.Take(3).All(f=>f.GetComponent<Collider>().enabled),"Stopping a block did not restore pending fish");
                Check(session.Fish.Count==50 && nets.ActiveNetCount==0,"Cancelled net changed population or left visual effects");
                session.config.netRespectsRoles=false;session.ResetSchool();session.Begin();
                var target=session.Fish.First(f=>session.LineVisible(f));target.Roaming.SetState(FishMotionState.Stationary);
                var d=(target.transform.position-session.input.Origin).normalized;session.input.SubmitOrientation(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,Mathf.Asin(d.y)*Mathf.Rad2Deg);session.input.Source[0]="desktop";session.input.SubmitButton(0);
                session.enabled=true;nets.enabled=true;
            }))yield break;
            yield return new WaitForSecondsRealtime(2.1f);
            if(!Run(()=>
            {
                Check(session.Captured>=1 && session.Fish.Count==50-session.Captured,"Actual input/update loop did not remove captured fish");
                Check(nets.PendingFishCount==0,"Normal capture did not finish");
                session.End("normal_net_loop_complete");
                string log=session.LastLogFolder;var rows=File.ReadAllLines(Path.Combine(log,"samples.csv"));
                Check(rows.Length>1 && rows[0].EndsWith("capture_in_progress"),"Capture phase missing from sample log");
                Check(rows.Skip(1).Any(r=>r.EndsWith(",1")),"No struggle-phase sample recorded");
                File.AppendAllText(Path.Combine(folder,"results.txt"),"Normal-loop log: "+log+"\n");
                // An empty school stays empty until the explicit reset action.
                session.Begin();foreach(var fish in session.Fish.ToArray()){fish.IsBeingCaptured=true;session.CompleteNetCapture(fish,0);}
                Check(session.Fish.Count==0,"Empty-school setup failed");session.End("empty_school");session.Begin();Check(session.Fish.Count==0,"An empty school respawned automatically");session.End("empty_school_restart");
                session.ResetSchool();Check(session.Fish.Count==50,"Explicit reset did not restore fifty fish");
                File.AppendAllText(Path.Combine(folder,"results.txt"),"PASS: "+checks+" checks; two-second hovering and hiding; pointer mapping in six layouts; frozen click ray; net launch/impact; area capture of all species; outside/real-cover rejection; struggle and 50-to-47 removal; no duplicate catches; optional role filter; cancellation; real input loop and capture-phase logging; no automatic replenishment; explicit reset.\n");
            }))yield break;
            Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            int index=Array.IndexOf(projection.walls,view);projection.room.Wall(index,out _,out _,out _,out float width);
            int w=1600,h=Mathf.RoundToInt(1600*(projection.room.ProjectionTop-projection.room.projectionBottom)/width);
            var target=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);target.Create();
            bool enabled=projection.enabled;var oldRect=view.rect;projection.enabled=false;view.rect=new Rect(0,0,1,1);projection.room.ApplyCamera(index,view);
            for(int i=0;i<2;i++){RenderPipeline.SubmitRenderRequest(view,new UniversalRenderPipeline.SingleCameraRequest{destination=target});yield return null;}
            var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());RenderTexture.active=old;Destroy(image);target.Release();Destroy(target);view.rect=oldRect;projection.enabled=enabled;
        }
    }
}
