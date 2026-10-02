using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PBR
{
    // Opt-in executable smoke test; inactive during normal use.
    public sealed class PrototypeSmokeRun : MonoBehaviour
    {
        public FishExperiment experiment;
        IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pbr-smoke") < 0) yield break;
            experiment.input.DesktopInput = false;
            experiment.condition = ExperimentCondition.AdaptiveHandoff;
            experiment.pairId = "automated_smoke_not_participant";
            experiment.Begin();
            if (!experiment.Running) { Application.Quit(2); yield break; }
            bool wrongOwner = false, captured = false, distractor = false, blocked = false, handoffCaptured = false, screenshot = false;
            float[] yaw = { -15, 15 }, pitch = { 0, 0 };
            while (experiment.Elapsed < 28 && experiment.Running)
            {
                double now = experiment.Elapsed;
                int shoot = -1;
                foreach (var f in experiment.Fish)
                {
                    if (f.Id == 0 && now > 1 && !captured)
                    {
                        int p = wrongOwner ? 0 : 1; Aim(f,p,yaw,pitch); shoot = p;
                        if (wrongOwner) captured = true; else wrongOwner = true;
                        break;
                    }
                    if (f.Id == 2 && now > 6.5 && !distractor)
                    { Aim(f,0,yaw,pitch); shoot = 0; distractor = true; break; }
                    if (f.Id == 1 && !experiment.LineVisible(f) && !blocked)
                    { Aim(f,1,yaw,pitch); shoot = 1; blocked = true; break; }
                    if (f.Id == 3 && !handoffCaptured)
                    {
                        yaw[0] = 130; pitch[0] = 0; Aim(f,1,yaw,pitch);
                        if (f.HandoffPlayer == 1 && experiment.LineVisible(f)) { shoot = 1; handoffCaptured = true; }
                        break;
                    }
                    if (f.Id == 4 && handoffCaptured)
                    { yaw[1] = 130; pitch[1] = 0; Aim(f,0,yaw,pitch); break; }
                }
                for (int p=0;p<2;p++) experiment.input.SubmitOrientation(p,yaw[p],pitch[p]);
                if (shoot >= 0) experiment.input.SubmitButton(shoot);
                if (now > 6.8 && now < 7.3)
                { CaptureScene(); screenshot = true; }
                yield return null;
            }
            experiment.End("automated_smoke_complete");
            File.WriteAllText(Path.Combine(Application.dataPath, "../smoke-result.txt"), "Completed 28 second runtime smoke\nLogs: " + experiment.LastLogFolder);
            Application.Quit(0);
        }
        void Aim(FishTarget f, int p, float[] yaw, float[] pitch)
        {
            Vector3 direction = (f.transform.position - experiment.input.Origin).normalized;
            yaw[p] = Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            pitch[p] = Mathf.Asin(direction.y)*Mathf.Rad2Deg;
        }
        void CaptureScene()
        {
            var report = new System.Text.StringBuilder();
            foreach (var fish in experiment.Fish)
            {
                report.AppendLine(fish.name + " position=" + fish.transform.position);
                foreach (var renderer in fish.GetComponentsInChildren<Renderer>())
                {
                    report.AppendLine(renderer.name + " bounds=" + renderer.bounds + " scale=" + renderer.transform.lossyScale);
                    if (renderer is SkinnedMeshRenderer skin)
                    {
                        var baked = new Mesh(); skin.BakeMesh(baked, true);
                        report.AppendLine("baked=" + baked.bounds + " worldCenter=" + skin.transform.TransformPoint(baked.bounds.center));
                        Destroy(baked);
                    }
                }
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../smoke-renderers.txt"), report.ToString());
            var camera = experiment.input.Cameras[0];
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var pixels = new Texture2D(1600,900,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1600,900),0,0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../smoke-preview.png"), pixels.EncodeToPNG());
            RenderTexture.active = previous; target.Release(); Destroy(target); Destroy(pixels);
        }
    }
}
