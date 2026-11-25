using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PBR
{
    public sealed class FourWallProjection : MonoBehaviour
    {
        public Camera[] walls;
        public ProjectionRoom room;
        // 0 = desktop grid, 1..4 = individual wall, 5 = ordered horizontal strip.
        public int previewMode;
        public bool independentDisplays;
        void Start()
        {
            if (LargeSpaceProjection.TryStart(this))
            {
                enabled = false;
                return;
            }
            independentDisplays |= Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-four-displays") >= 0;
            if (independentDisplays && Display.displays.Length < 4)
            { Debug.LogWarning("Four displays unavailable; using desktop grid."); independentDisplays = false; }
            if (independentDisplays) for (int i = 1; i < 4; i++) Display.displays[i].Activate();
            Apply();
        }
        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && !independentDisplays)
            {
                if (kb.digit0Key.wasPressedThisFrame) previewMode = 0;
                if (kb.digit1Key.wasPressedThisFrame) previewMode = 1;
                if (kb.digit2Key.wasPressedThisFrame) previewMode = 2;
                if (kb.digit3Key.wasPressedThisFrame) previewMode = 3;
                if (kb.digit4Key.wasPressedThisFrame) previewMode = 4;
                if (kb.digit5Key.wasPressedThisFrame) previewMode = 5;
            }
            Apply();
        }
        public void Apply()
        {
            for (int i = 0; i < walls.Length; i++)
            {
                var cam = walls[i]; cam.enabled = independentDisplays || previewMode == 0 || previewMode == 5 || previewMode == i + 1;
                cam.targetDisplay = independentDisplays ? i : 0;
                if (independentDisplays || previewMode >= 1 && previewMode <= 4) cam.rect = new Rect(0, 0, 1, 1);
                else if (previewMode == 0) cam.rect = new Rect(i % 2 * .5f, i < 2 ? .5f : 0, .5f, .5f);
                else
                {
                    float h = Mathf.Min(1, (float)Screen.width / Screen.height / 4 / (16f / 9));
                    cam.rect = new Rect(i * .25f, (1 - h) * .5f, .25f, h);
                }
                if(room && !independentDisplays)
                {
                    room.Wall(i,out _,out _,out _,out float wallWidth);
                    float wallAspect=wallWidth/(room.ProjectionTop-room.projectionBottom);
                    if(previewMode==5)
                    {
                        float perimeter=2*(room.roomWidth+room.roomDepth);
                        float start=i==0?0:i==1?room.roomWidth:i==2?room.roomWidth+room.roomDepth:2*room.roomWidth+room.roomDepth;
                        float height=Mathf.Min(1,(float)Screen.width/Mathf.Max(1,Screen.height)*(room.ProjectionTop-room.projectionBottom)/perimeter);
                        cam.rect=new Rect(start/perimeter,(1-height)*.5f,wallWidth/perimeter,height);
                    }
                    else
                    {
                        var rect=cam.rect;float cellAspect=Screen.width*rect.width/Mathf.Max(1,Screen.height*rect.height);
                        if(cellAspect>wallAspect){float width=rect.width*wallAspect/cellAspect;rect.x+=(rect.width-width)*.5f;rect.width=width;}
                        else{float height=rect.height*cellAspect/wallAspect;rect.y+=(rect.height-height)*.5f;rect.height=height;}
                        cam.rect=rect;
                    }
                }
                float aspect = independentDisplays ? (float)Display.displays[i].renderingWidth / Mathf.Max(1, Display.displays[i].renderingHeight)
                    : Screen.width * cam.rect.width / Mathf.Max(1, Screen.height * cam.rect.height);
                cam.aspect = aspect;
                // Measured wall geometry determines the frustum; legacy scenes use 90 horizontal degrees.
                if(room) room.ApplyCamera(i,cam);
                else {cam.ResetProjectionMatrix();cam.fieldOfView = 2 * Mathf.Atan(1 / aspect) * Mathf.Rad2Deg;}
            }
        }
        void OnGUI()
        {
            if (independentDisplays) return;
            GUI.Label(new Rect(20, Screen.height - 58, Screen.width - 40, 25), "360 REEF   0: Four walls   1: Front   2: Right   3: Back   4: Left   5: Seam strip (Front > Right > Back > Left > Front)");
        }
    }
}
