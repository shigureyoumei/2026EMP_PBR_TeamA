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
                float elevation=saturate(d.y*1.25+.18);
                half3 color=lerp(half3(.018,.105,.14),half3(.085,.39,.43),elevation);
                float alignment=saturate(dot(d,normalize(float3(.32,.82,.46))));
                float sun=pow(alignment,36);
                float2 surfaceUV=d.xz/max(.18,d.y);
                float distortion=sin(surfaceUV.x*12+sin(surfaceUV.y*7+_Time.y*.12))*sin(surfaceUV.y*14-_Time.y*.15);
                float surface=pow(saturate(d.y),4)*(.025+.018*distortion);
                float shaft=pow(saturate(.5+.5*sin(d.x*47+d.z*31+d.y*7)),24)*pow(alignment,5)*saturate(d.y)*.045;
                color+=sun*half3(.28,.37,.31)+surface*half3(.38,.6,.58)+shaft;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
