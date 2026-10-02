Shader "PBR/Reef Fish"
{
    Properties
    {
        _BaseColor("Body", Color) = (1,0.4,0.04,1)
        _AccentColor("Stripe", Color) = (1,0.92,0.6,1)
        _FinColor("Fin tips", Color) = (1,0.6,0.02,1)
        _LocalMin("Mesh minimum", Vector) = (0,0,0,0)
        _LocalInvSize("Mesh inverse size", Vector) = (1,1,1,0)
        _StripeCount("Stripe count", Float) = 4
        _FishOrigin("Origin", Vector)=(0,0,0,0)
        _FishForward("Forward", Vector)=(0,0,1,0)
        _FishUp("Up", Vector)=(0,1,0,0)
        _FishRight("Right", Vector)=(1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
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
            half4 _BaseColor, _AccentColor, _FinColor;
            float4 _LocalMin, _LocalInvSize;
            float4 _FishOrigin, _FishForward, _FishUp, _FishRight;
            float _StripeCount;
            CBUFFER_END
            struct A { float4 pos:POSITION; float3 normal:NORMAL; };
            struct V { float4 pos:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; float3 local:TEXCOORD2; half fog:TEXCOORD3; };
            V Vert(A v)
            {
                V o; o.pos=TransformObjectToHClip(v.pos.xyz); o.world=TransformObjectToWorld(v.pos.xyz);
                o.normal=TransformObjectToWorldNormal(v.normal);
                float3 relative=o.world-_FishOrigin.xyz;
                o.local=float3(dot(relative,_FishRight.xyz)/.6+.5,dot(relative,_FishForward.xyz)/1.5+.5,dot(relative,_FishUp.xyz)/1.0+.5);
                o.fog=ComputeFogFactor(o.pos.z); return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 n=normalize(i.normal), view=normalize(GetWorldSpaceViewDir(i.world));
                Light light=GetMainLight();
                half stripes=smoothstep(.45,.68,sin(i.local.y*_StripeCount*6.28318 + .35*sin(i.local.z*7)));
                half3 color=lerp(_BaseColor.rgb,_AccentColor.rgb,stripes*.88);
                half belly=smoothstep(.38,.02,i.local.z);
                color=lerp(color,lerp(_AccentColor.rgb,half3(1,1,1),.4),belly*.48);
                half fin=saturate((abs(i.local.z-.5)-.28)*6);
                color=lerp(color,_FinColor.rgb,fin*.7);
                half diffuse=saturate(dot(n,light.direction)*.45+.55);
                half rim=pow(1-saturate(dot(n,view)),2.5);
                half spec=pow(saturate(dot(n,normalize(light.direction+view))),48)*.6;
                color=color*(.55+diffuse*.5)+rim*_AccentColor.rgb*.24+spec;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
