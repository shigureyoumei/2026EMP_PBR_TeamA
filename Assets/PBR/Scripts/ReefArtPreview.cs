using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PBR
{
    public sealed class ReefArtPreview : MonoBehaviour
    {
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pbr-art-preview")<0) yield break;
            yield return new WaitForSecondsRealtime(2);
            var camera=Camera.main;
            var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32); target.Create();
            for(int i=0;i<3;i++)
            {
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                yield return null;
            }
            var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../art-preview.png"),image.EncodeToPNG());
            RenderTexture.active=previous; target.Release(); Destroy(target); Destroy(image);
            Application.Quit(0);
        }
    }
}
