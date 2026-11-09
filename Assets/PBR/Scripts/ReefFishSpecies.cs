using UnityEngine;

namespace PBR
{
    public sealed class ReefFishSpecies : MonoBehaviour
    {
        public string species;
        Renderer body;
        RingFishMotion motion;
        FishTarget target;
        MaterialPropertyBlock block;
        static readonly int Frequency=Shader.PropertyToID("_Frequency"),Amplitude=Shader.PropertyToID("_Amplitude");
        void Start(){body=GetComponent<Renderer>();motion=GetComponentInParent<RingFishMotion>();target=GetComponentInParent<FishTarget>();block=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(!body || !motion)return;
            bool caught=target && target.IsBeingCaptured;
            body.GetPropertyBlock(block);block.SetFloat(Frequency,caught?8.5f:1.1f+motion.Speed*.48f);block.SetFloat(Amplitude,caught?.14f:.035f+Mathf.Min(motion.Speed,6)*.009f);body.SetPropertyBlock(block);
        }
    }
}
