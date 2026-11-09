Shader "PBR/White Fishing Net"
{
    Properties{_BaseColor("Rope color",Color)=(1,1,1,.95) _Opacity("Opacity",Range(0,1))=1}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Opacity;
            CBUFFER_END
            struct A{float4 pos:POSITION;};
            struct V{float4 pos:SV_POSITION;};
            V Vert(A i){V o;o.pos=TransformObjectToHClip(i.pos.xyz);return o;}
            half4 Frag(V i):SV_Target{return half4(_BaseColor.rgb,_BaseColor.a*_Opacity);}
            ENDHLSL
        }
    }
}
