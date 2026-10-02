Shader "PBR/Three Dimensional Seabed"
{
    Properties { _BaseColor("Sand",Color)=(.42,.72,.59,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; half fog:TEXCOORD2; };
            V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(i.normalOS); o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.world.xz;
                float ripple=sin(p.x*7+sin(p.y*1.9)*1.8)*.028;
                float2 q=p+float2(sin(p.y*.53),cos(p.x*.61))*.75;
                float c=sin(q.x*1.65+q.y*.45+_Time.y*.16)+sin(q.y*1.95-q.x*.42-_Time.y*.13);
                float caustic=pow(saturate(1-abs(c)*1.5),6)*.11;
                Light light=GetMainLight();
                half3 color=(_BaseColor.rgb+ripple+caustic)*(.65+.35*saturate(dot(normalize(i.normal),light.direction)));
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
