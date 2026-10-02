using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBR.Editor
{
    public static class ProjectionRoomSetup
    {
        public static ProjectionRoom Attach()
        {
            var projection=UnityEngine.Object.FindAnyObjectByType<FourWallProjection>();
            if(!projection) throw new Exception("Open the 360 reef scene first");
            var room=UnityEngine.Object.FindAnyObjectByType<ProjectionRoom>();
            if(!room) room=new GameObject("Projection Room").AddComponent<ProjectionRoom>();
            room.projection=projection;projection.room=room;
            room.virtualEye=projection.walls[0].transform.position;room.Sanitize();room.Apply();
            EditorUtility.SetDirty(room);EditorUtility.SetDirty(projection);return room;
        }
        [MenuItem("PBR/Select projection room settings")]
        public static void SelectRoom()
        {
            var room=UnityEngine.Object.FindAnyObjectByType<ProjectionRoom>();
            if(!room) {EditorUtility.DisplayDialog("Projection room","请先打开 FishAttention360 场景。","OK");return;}
            Selection.activeGameObject=room.gameObject;EditorGUIUtility.PingObject(room.gameObject);
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(room.virtualEye,Quaternion.Euler(35,-25,0),Mathf.Max(room.roomWidth,room.roomDepth)*.8f);
        }
        static void Validate(ProjectionRoom room)
        {
            int count=0;void Check(bool value,string reason){count++;if(!value)throw new Exception(reason);}
            string snapshot=JsonUtility.ToJson(room);
            try
            {
                foreach(var size in new[]{new Vector3(25,15,7.7f),new Vector3(6,6,3),new Vector3(8,5,3.5f)})
                foreach(var offset in new[]{Vector2.zero,new Vector2(.6f,-.4f)})
                {
                    room.roomWidth=size.x;room.roomDepth=size.y;room.roomHeight=size.z;room.testerOffset=offset;room.projectionBottom=.2f;room.projectionTopMargin=.3f;room.Sanitize();room.Apply();
                    for(int i=0;i<4;i++)
                    {
                        room.Wall(i,out var center,out var right,out _,out float width);
                        foreach(var uv in new[]{Vector2.zero,Vector2.one,new Vector2(0,1),new Vector2(1,0),new Vector2(.5f,.5f)})
                        {
                            Vector3 p=center+right*(uv.x-.5f)*width;p.y=Mathf.Lerp(room.projectionBottom,room.ProjectionTop,uv.y);
                            Vector3 view=room.projection.walls[i].WorldToViewportPoint(room.ToVirtual(p));
                            Check(Mathf.Abs(view.x-uv.x)<.001f && Mathf.Abs(view.y-uv.y)<.001f,"Off-axis corner mapping");
                        }
                        var ray=new Ray(room.PhysicalEye,(center-room.PhysicalEye).normalized);
                        Check(room.TryWallHit(ray,out int wall,out var hit,out var mapped) && wall==i && Vector3.Distance(hit,center)<.001f && Vector2.Distance(mapped,new Vector2(.5f,.5f))<.001f,"Physical wall intersection");
                    }
                    Check(!room.TryWallHit(new Ray(room.PhysicalEye,Vector3.up),out _,out _,out _),"Upward ray must not hit a wall");
                }
            }
            finally {JsonUtility.FromJsonOverwrite(snapshot,room);room.Sanitize();room.Apply();}
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/room-results.txt","PASS: "+count+" checks. Square, rectangular, Large Space dimensions; centered and offset observers; all wall corners, center rays, upward miss.\n");
        }
        public static void InstallBatch()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(RingReefBuilder.ScenePath);
                var room=Attach();Validate(room);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                var report=BuildPipeline.BuildPlayer(new[]{RingReefBuilder.ScenePath},"BuildsRoom/PBRRoom.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Room player build failed");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
