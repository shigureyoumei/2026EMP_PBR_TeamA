using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBR.Editor
{
    public static class SeabedViewSetup
    {
        public static void BuildBatch()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(RingReefBuilder.ScenePath);
                var room=UnityEngine.Object.FindAnyObjectByType<ProjectionRoom>();var session=UnityEngine.Object.FindAnyObjectByType<FishExperiment>();
                float physicalEye=room.eyeHeight;room.seabedViewHeight=.55f;room.Apply();
                var projection=room.projection;projection.Apply();int checks=0;
                void Check(bool value,string reason){checks++;if(!value)throw new Exception(reason);}
                Check(Mathf.Abs(room.virtualEye.y-.55f)<.0001f,"Virtual height");
                Check(Mathf.Abs(room.eyeHeight-physicalEye)<.0001f,"Physical eye height changed");
                Check(Vector3.Distance(session.input.Origin,room.virtualEye)<.0001f,"Input origin mismatch");
                Check(Vector3.Distance(room.habitat.Eye,room.virtualEye)<.0001f,"Hiding viewpoint mismatch");
                for(int i=0;i<4;i++)
                {
                    var camera=projection.walls[i];Check(Vector3.Distance(camera.transform.position,room.virtualEye)<.0001f,"Camera height mismatch");
                    room.Wall(i,out var center,out var right,out _,out float width);
                    foreach(var uv in new[]{Vector2.zero,Vector2.one,new Vector2(0,1),new Vector2(1,0)})
                    {
                        var p=center+right*(uv.x-.5f)*width;p.y=Mathf.Lerp(room.projectionBottom,room.ProjectionTop,uv.y);
                        // Test the frustum directly; batch-editor preview rectangles may be rounded to pixels.
                        var world=room.ToVirtual(p);var clip=camera.projectionMatrix*camera.worldToCameraMatrix*new Vector4(world.x,world.y,world.z,1);
                        var mapped=new Vector2(clip.x/clip.w*.5f+.5f,clip.y/clip.w*.5f+.5f);
                        Check(Vector2.Distance(mapped,uv)<.001f,"Wall calibration changed: wall="+i+" expected="+uv+" actual="+mapped.ToString("F8"));
                    }
                }
                EditorUtility.SetDirty(room);EditorUtility.SetDirty(session.input);EditorUtility.SetDirty(room.habitat);
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Validation");File.WriteAllText("Validation/seabed-view.txt","PASS: "+checks+" checks; virtual camera Y 0.55 m; physical eye height retained; input, hiding and four cameras synchronized; wall corners preserved.\n");
                var report=BuildPipeline.BuildPlayer(new[]{RingReefBuilder.ScenePath},"BuildsLow/PBRLow.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Low-view build failed");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
