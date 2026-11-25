Shader "PBR/Large Space Screen"
{
    Properties
    {
        _FacePositiveX ("Reef +X", 2D) = "black" {}
        _FaceNegativeX ("Reef -X", 2D) = "black" {}
        _FacePositiveY ("Reef +Y", 2D) = "black" {}
        _FaceNegativeY ("Reef -Y", 2D) = "black" {}
        _FacePositiveZ ("Reef +Z", 2D) = "black" {}
        _FaceNegativeZ ("Reef -Z", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FacePositiveX); SAMPLER(sampler_FacePositiveX);
            TEXTURE2D(_FaceNegativeX); SAMPLER(sampler_FaceNegativeX);
            TEXTURE2D(_FacePositiveY); SAMPLER(sampler_FacePositiveY);
            TEXTURE2D(_FaceNegativeY); SAMPLER(sampler_FaceNegativeY);
            TEXTURE2D(_FacePositiveZ); SAMPLER(sampler_FacePositiveZ);
            TEXTURE2D(_FaceNegativeZ); SAMPLER(sampler_FaceNegativeZ);

            CBUFFER_START(UnityPerMaterial)
                float3 _PhysicalEye;
                float _CaptureTangent;
                float4 _AimA;
                float4 _AimB;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vertex(float3 position : POSITION)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(position);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float2 FaceUV(float2 coordinates, float depth)
            {
                return coordinates / (depth * 2 * _CaptureTangent) + .5;
            }

            half3 Reef(float3 direction)
            {
                float3 d = direction;
                float3 a = abs(d);
                half3 color = 0;
                // The face axes match the capture cameras. Each face overscans the cube edges by one degree.
                if (a.x >= max(a.y, a.z))
                {
                    if (d.x >= 0) color = SAMPLE_TEXTURE2D_LOD(_FacePositiveX, sampler_FacePositiveX, FaceUV(float2(-d.z, d.y), a.x), 0).rgb;
                    else color = SAMPLE_TEXTURE2D_LOD(_FaceNegativeX, sampler_FaceNegativeX, FaceUV(float2(d.z, d.y), a.x), 0).rgb;
                }
                else if (a.y >= a.z)
                {
                    if (d.y >= 0) color = SAMPLE_TEXTURE2D_LOD(_FacePositiveY, sampler_FacePositiveY, FaceUV(float2(d.x, -d.z), a.y), 0).rgb;
                    else color = SAMPLE_TEXTURE2D_LOD(_FaceNegativeY, sampler_FaceNegativeY, FaceUV(float2(d.x, d.z), a.y), 0).rgb;
                }
                else if (d.z >= 0) color = SAMPLE_TEXTURE2D_LOD(_FacePositiveZ, sampler_FacePositiveZ, FaceUV(float2(d.x, d.y), a.z), 0).rgb;
                else color = SAMPLE_TEXTURE2D_LOD(_FaceNegativeZ, sampler_FaceNegativeZ, FaceUV(float2(-d.x, d.y), a.z), 0).rgb;
                return color;
            }

            half Reticle(float3 direction, float4 aim)
            {
                if (aim.w == 0 || dot(direction, aim.xyz) < .9997) return 0;
                float3 right = normalize(cross(float3(0, 1, 0), aim.xyz) + float3(.00001, 0, 0));
                float2 offset = abs(float2(dot(direction, right), dot(direction, cross(aim.xyz, right))));
                return (max(offset.x, offset.y) < .015 && min(offset.x, offset.y) < .0015) ? 1 : 0;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.positionWS - _PhysicalEye);
                half3 color = Reef(direction);
                color = lerp(color, half3(1, .45, .12), Reticle(direction, _AimA));
                color = lerp(color, half3(.16, .75, 1), Reticle(direction, _AimB));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
