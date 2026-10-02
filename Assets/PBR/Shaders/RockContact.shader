Shader "PBR/Reef Contact Shade"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION;float2 uv:TEXCOORD0; };
            struct V { float4 p:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1; };
            V Vert(A i) {V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target {float r=length((i.uv-.5)*2);return half4(MixFog(half3(.01,.07,.055),i.fog),pow(saturate(1-r),1.5)*.6);}
            ENDHLSL
        }
    }
}
