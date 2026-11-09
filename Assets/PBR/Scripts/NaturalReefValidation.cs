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
    // Explicit local player validation. Inactive during ordinary editor and participant sessions.
    public sealed class NaturalReefValidation : MonoBehaviour
    {
        int checks,states;
        string folder;
        readonly HashSet<int> hiddenFish=new HashSet<int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-natural-smoke")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-natural-preview")>=0)
                new GameObject("Natural reef validation").AddComponent<NaturalReefValidation>();
        }
        void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../ValidationNatural"));Directory.CreateDirectory(folder);
            yield return null;
            var session=FindAnyObjectByType<FishExperiment>();var habitat=FindAnyObjectByType<RingHabitat>();var projection=FindAnyObjectByType<FourWallProjection>();
            bool smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-natural-smoke")>=0;
            if(smoke)
            {
                try
                {
                    Check(session.config.naturalReef && session.config.fishCount==50 && session.Fish.Count==50,"Expected exactly fifty persistent fish");
                    Check(session.config.fishPrefabs.Length==24,"Expected eight species with three responsibility variants");
                    Check(session.Fish.Select(f=>f.GetComponentInChildren<ReefFishSpecies>().species).Distinct().Count()==8,"Missing fish diversity");
                    Check(session.Fish.Count(f=>f.Owner==0)==20 && session.Fish.Count(f=>f.Owner==1)==20 && session.Fish.Count(f=>f.Owner<0)==10,"Responsibility balance");
                    Check(habitat.covers.Length==16,"Original cover sites changed");
                    foreach(var renderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                        foreach(var mat in renderer.sharedMaterials)Check(mat && mat.shader.isSupported,"Missing/unsupported material: "+renderer.name);
                    foreach(var cover in habitat.covers)Check(cover.GetComponent<MeshCollider>().sharedMesh==cover.GetComponent<MeshFilter>().sharedMesh,"Hidden collider mismatch");
                    var originals=session.Fish.ToArray();
                    session.Begin();Check(session.Running,"Block did not begin");
                    session.enabled=false;
                    foreach(var fish in session.Fish)fish.Roaming.SetState(FishMotionState.Hiding);
                    // 120 simulated seconds exercises actual swept motion and physical hiding for all 50 fish.
                    for(int step=0;step<7200;step++)
                    {
                        foreach(var fish in session.Fish)
                        {
                            var old=fish.transform.position;var rotation=fish.transform.rotation;fish.Roaming.Tick(1f/60);
                            Check(fish.gameObject.activeInHierarchy,"Fish disappeared");
                            Check(Vector3.Distance(old,fish.transform.position)<=session.config.burstSpeed/60+.002f,"Fish teleported");
                            Check(Quaternion.Angle(rotation,fish.transform.rotation)<=RingFishMotion.TurnDegreesPerSecond/60+.1f,"Instant fish turn");
                            Check(habitat.Free(fish.transform.position),"Fish clips physical habitat: "+fish.Id+" "+fish.transform.position);
                            states|=1<<(int)fish.Roaming.State;
                            if(fish.Roaming.HiddenAtRest){Check(habitat.FullyOccluded(fish.transform.position),"False hiding");hiddenFish.Add(fish.Id);}
                        }
                        if(step%600==0){Debug.Log("Natural reef simulation step "+step);}
                    }
                    Check(states==15,"Missing a motion state");Check(hiddenFish.Count>=10,"Insufficient actual hiding: "+hiddenFish.Count);
                    session.End("natural_motion_validation");session.Begin();Check(session.Fish.SequenceEqual(originals),"School replaced across blocks");session.End("natural_motion_done");
                    session.enabled=true;
                    projection.Apply();
                    for(int i=0;i<4;i++)
                    {
                        projection.room.Wall(i,out var center,out var right,out _,out float width);var point=projection.room.ToVirtual(center+right*width*.5f);
                        var a=projection.walls[i].WorldToViewportPoint(point);var b=projection.walls[(i+1)%4].WorldToViewportPoint(point);
                        Check(Mathf.Abs(a.x-1)<.001f && Mathf.Abs(b.x)<.001f && Mathf.Abs(a.y-b.y)<.001f,"Wall seam mismatch");
                    }
                    File.WriteAllText(Path.Combine(folder,"results.txt"),"PASS: "+checks+" assertions; 120 simulated seconds; 50 stable fish; 8 species silhouettes; balanced roles; all 4 motion states; "+hiddenFish.Count+" fish physically hid; matching cover render/collider meshes; four-wall seams.\n");
                }
                catch(Exception e){File.WriteAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e);Debug.LogException(e);Application.Quit(1);yield break;}
                yield return new WaitForSecondsRealtime(2);
                session.input.DesktopInput=false;session.config.durationSeconds=15;session.Begin();
                int frames=Time.frameCount;double start=Time.realtimeSinceStartupAsDouble;
                yield return new WaitForSecondsRealtime(16);
                try
                {
                    Check(!session.Running && session.Fish.Count==50,"Normal timed block/population failed");
                    double fps=(Time.frameCount-frames)/(Time.realtimeSinceStartupAsDouble-start);
                    File.AppendAllText(Path.Combine(folder,"results.txt"),"PASS: normal rendered 15-second block; 50 fish retained; average FPS "+fps.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"; log "+session.LastLogFolder+"\n");
                }
                catch(Exception e){File.AppendAllText(Path.Combine(folder,"results.txt"),"FAIL: "+e);Application.Quit(1);yield break;}
            }
            yield return new WaitForSecondsRealtime(3);
            session.enabled=false;projection.enabled=false;
            var images=new List<Texture2D>();
            for(int i=0;i<4;i++)
            {
                var camera=projection.walls[i];projection.room.Wall(i,out _,out _,out _,out float width);
                float aspect=width/(projection.room.ProjectionTop-projection.room.projectionBottom);
                int w=1600,h=Mathf.RoundToInt(w/aspect);var target=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);target.Create();
                camera.rect=new Rect(0,0,1,1);camera.enabled=true;projection.room.ApplyCamera(i,camera);
                for(int warm=0;warm<3;warm++){RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});yield return null;}
                var old=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(w,900,TextureFormat.RGB24,false);image.SetPixels(new Color[w*900]);image.ReadPixels(new Rect(0,0,w,h),0,(900-h)/2);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,new[]{"Front","Right","Back","Left"}[i]+".png"),image.EncodeToPNG());images.Add(image);
                RenderTexture.active=old;target.Release();Destroy(target);
            }
            var grid=new Texture2D(3200,1800,TextureFormat.RGB24,false);
            for(int i=0;i<4;i++)grid.SetPixels(i%2*1600,i<2?900:0,1600,900,images[i].GetPixels());grid.Apply();
            File.WriteAllBytes(Path.Combine(folder,"FourWalls.png"),grid.EncodeToPNG());
            Destroy(grid);foreach(var image in images)Destroy(image);
            Application.Quit(0);
        }
    }
}
