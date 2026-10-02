Shader "PBR/Reef Stone"
{
    Properties { _BaseColor("Rock",Color)=(.15,.35,.43,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
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
            struct A { float4 pos:POSITION; float3 normal:NORMAL; };
            struct V { float4 pos:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; half fog:TEXCOORD2; };
            V Vert(A v) { V o; o.pos=TransformObjectToHClip(v.pos.xyz); o.world=TransformObjectToWorld(v.pos.xyz); o.normal=TransformObjectToWorldNormal(v.normal); o.fog=ComputeFogFactor(o.pos.z); return o; }
            float Hash(float3 p) { return frac(sin(dot(p,float3(12.9898,78.233,37.719)))*43758.5453); }
            float Noise(float3 p)
            {
                float3 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+float3(1,1,1)),f.x),f.y),f.z);
            }
            half4 Frag(V i):SV_Target
            {
                float n=Noise(i.world*3)*.6+Noise(i.world*9)*.3+Noise(i.world*27)*.1;
                float3 normal=normalize(i.normal);
                half moss=saturate(normal.y*.7+.3)*smoothstep(.4,.7,n);
                half3 color=lerp(_BaseColor.rgb*lerp(.6,1.7,n),half3(.36,.55,.24),moss*.75);
                color*=.6+.6*saturate(dot(normal,GetMainLight().direction));
                color+=pow(saturate(1-abs(sin(i.world.y*5+n*4))),14)*.025;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
