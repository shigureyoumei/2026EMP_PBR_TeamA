using UnityEngine;

namespace PBR
{
    [DefaultExecutionOrder(100)]
    public sealed class FishSurfaceCoordinates : MonoBehaviour
    {
        Renderer[] renderers;
        MaterialPropertyBlock block;
        void Awake() { renderers=GetComponentsInChildren<SkinnedMeshRenderer>(); block=new MaterialPropertyBlock(); }
        void LateUpdate()
        {
            foreach(var renderer in renderers)
            {
                renderer.GetPropertyBlock(block);
                block.SetVector("_FishOrigin",transform.position);
                block.SetVector("_FishForward",transform.forward);
                block.SetVector("_FishUp",transform.up);
                block.SetVector("_FishRight",transform.right);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
