Shader "PBR/Natural Reef Surface"
{
    Properties
    {
        _BaseColor("Tint",Color)=(1,1,1,1)
        _RockMap("Scanned albedo",2D)="white"{}
        _RockNormal("Scanned normal",2D)="bump"{}
        _RoughMap("Scanned roughness",2D)="white"{}
        _Tiling("World texture scale",Float)=.65
        _NormalStrength("Normal strength",Float)=.65
        _Style("0 rock, 1 sand, 2 coral, 3 vegetation",Float)=0
        _Sway("Current displacement",Float)=0
        _Smoothness("Smoothness",Range(0,1))=.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NaturalReefLighting.hlsl"
            TEXTURE2D(_RockMap);SAMPLER(sampler_RockMap);
            TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
            TEXTURE2D(_RoughMap);SAMPLER(sampler_RoughMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Tiling,_NormalStrength,_Style,_Sway,_Smoothness;
            CBUFFER_END
            struct A {float4 pos:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 color:COLOR;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V Vert(A v)
            {
                V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
                float3 p=TransformObjectToWorld(v.pos.xyz);
                p.x+=sin(_Time.y*.72+p.z*.7+p.x*.3)*_Sway*v.color.a*v.color.a;
                p.z+=cos(_Time.y*.55+p.x*.8)*_Sway*.45*v.color.a;
                o.world=p;o.pos=TransformWorldToHClip(p);o.normal=TransformObjectToWorldNormal(v.normal);o.uv=v.uv;o.color=v.color;o.fog=ComputeFogFactor(o.pos.z);return o;
            }
            half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n=normalize(i.normal)*IS_FRONT_VFACE(face,1,-1);
                half3 color=_BaseColor.rgb;half rough=1-_Smoothness;
                if(_Style<1.5)
                {
                    float3 w=pow(abs(n),4);w/=max(.0001,w.x+w.y+w.z);float3 p=i.world*_Tiling;
                    half3 a=SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_RockMap,sampler_RockMap,p.xy).rgb*w.z;
                    half3 nx=UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal,sampler_RockNormal,p.zy));
                    half3 ny=UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal,sampler_RockNormal,p.xz));
                    half3 nz=UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal,sampler_RockNormal,p.xy));
                    half3 detail=half3(0,nx.y,nx.x)*w.x+half3(ny.x,0,ny.y)*w.y+half3(nz.x,nz.y,0)*w.z;
                    n=normalize(n+detail*_NormalStrength);
                    rough=SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.zy).r*w.x+SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.xz).r*w.y+SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,p.xy).r*w.z;
                    color*=a;
                    if(_Style<.5)
                    {
                        float algae=saturate(n.y*.7+.25)*(.5+.5*sin(i.world.x*1.2+sin(i.world.z*.8)));
                        color=lerp(color,color*half3(.5,.67,.38),algae*.46);
                    }
                }
                else
                {
                    color*=lerp(.72,1.22,i.color.rgb);
                    if(_Style>2.5)
                    {
                        float rib=pow(saturate(cos(i.uv.x*6.28318)),14);
                        color*=lerp(.76,1.18,saturate(i.uv.y));color+=rib*color*.13;
                    }
                    else
                    {
                        float pore=sin(i.world.x*86)*sin(i.world.y*79)*sin(i.world.z*83);
                        color*=.91+.09*pore;
                    }
                }
                half3 lit=ReefLight(i.world,n,color,1-saturate(rough),_Style>2.5?.6:0);
                return half4(MixFog(lit,i.fog),1);
            }
            ENDHLSL
        }
        // Rigid rocks/sand/corals cast shadows; plant sway remains small relative to leaf size.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
