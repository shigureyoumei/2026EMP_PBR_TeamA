Shader "PBR/Surrounding Water"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Front ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; };
            V Vert(A i) { V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);return o; }
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.world-_WorldSpaceCameraPos);
                float elevation=saturate(d.y*1.7+.12);
                half3 color=lerp(half3(.009,.10,.16),half3(.02,.42,.53),elevation);
                float sun=pow(saturate(dot(d,normalize(float3(-.3,.85,.25)))),12);
                float waves=sin(d.x*65+sin(d.z*40+_Time.y*.1)*2)+sin(d.z*79+sin(d.x*37)*2);
                float surface=pow(saturate(1-abs(waves)*1.3),5)*pow(saturate(d.y),2);
                float shafts=pow(saturate(sin(atan2(d.x,d.z)*25+d.y*3)),18)*saturate(d.y+.1)*.035;
                color+=sun*half3(.18,.26,.22)+surface*half3(.08,.2,.17)+shafts;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
