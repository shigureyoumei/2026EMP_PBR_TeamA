Shader "PBR/Natural Reef Fish"
{
    Properties
    {
        _BaseColor("Body",Color)=(.8,.35,.08,1)
        _AccentColor("Markings",Color)=(.96,.85,.6,1)
        _Pattern("Pattern",Float)=0
        _Frequency("Tail frequency",Float)=2.4
        _Amplitude("Tail amplitude",Float)=.045
        _CaptureFade("Capture visibility",Range(0,1))=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
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
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor,_AccentColor;float _Pattern,_Frequency,_Amplitude,_CaptureFade;
            CBUFFER_END
            struct A {float4 pos:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 pos:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 color:COLOR;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V Vert(A v)
            {
                V o=(V)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);
                float3 p=v.pos.xyz;float bend=saturate((.28-p.z)/1.0);float phase=_Time.y*_Frequency*6.28318+p.z*6+TransformObjectToWorld(float3(0,0,0)).x;
                p.x+=sin(phase)*bend*bend*_Amplitude;
                o.world=TransformObjectToWorld(p);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normal);o.uv=v.uv;o.color=v.color;o.fog=ComputeFogFactor(o.pos.z);return o;
            }
            half4 Frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float dither=frac(sin(dot(floor(i.pos.xy),float2(12.9898,78.233)))*43758.5453);
                clip(_CaptureFade-dither-.00001);
                float u=i.uv.x,v=i.uv.y;
                float stripe=smoothstep(.35,.54,sin(u*(3+fmod(_Pattern,4))*6.28318+v*.6));
                if(_Pattern>3.5)stripe=smoothstep(.3,.7,sin(v*24+u*3))*.65;
                half3 albedo=lerp(_BaseColor.rgb,_AccentColor.rgb,stripe*.65);
                albedo*=lerp(.67,1.1,saturate(i.color.r));
                float scales=pow(saturate(sin(u*210+sin(v*112)*1.5)*sin(v*112)),12);
                albedo+=scales*.035;
                if(i.color.b>.8)albedo=lerp(_BaseColor.rgb,_AccentColor.rgb,.6)*(.75+.25*sin(u*110));
                if(i.color.g<.1)albedo=half3(.008,.014,.019);
                half3 n=normalize(i.normal)*IS_FRONT_VFACE(face,1,-1);
                half3 lit=ReefLight(i.world,n,albedo,i.color.g<.1?.85:.42,0);
                return half4(MixFog(lit,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
