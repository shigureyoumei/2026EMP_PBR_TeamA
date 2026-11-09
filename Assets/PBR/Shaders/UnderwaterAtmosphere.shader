Shader "PBR/Underwater Atmosphere"
{
    Properties{_BaseColor("Color",Color)=(.3,.65,.65,.1) _Particle("Particle mode",Float)=0}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Particle;
            CBUFFER_END
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;half4 color:COLOR;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.pos.xyz);o.pos=TransformWorldToHClip(o.world);o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                float edge=saturate(sin(i.uv.x*3.141593));float opacity=edge*edge*saturate(sin(i.uv.y*3.141593));
                if(_Particle>.5)opacity=pow(saturate(1-length(i.uv*2-1)),2);
                else opacity*=.65+.35*sin(i.world.y*.8+i.world.x*.4+_Time.y*.14);
                float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.pos)),_ZBufferParams);
                opacity*=saturate((depth+TransformWorldToView(i.world).z)/.6);
                return half4(_BaseColor.rgb,_BaseColor.a*opacity*i.color.a);
            }
            ENDHLSL
        }
    }
}
