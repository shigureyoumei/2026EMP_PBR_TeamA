#ifndef PBR_NATURAL_REEF_LIGHTING
#define PBR_NATURAL_REEF_LIGHTING
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float ReefCaustic(float3 world)
{
    float2 p=world.xz*.72+world.y*.09;
    p+=float2(sin(p.y*.9+_Time.y*.19),cos(p.x*.8-_Time.y*.16))*.65;
    float a=sin(p.x*3.1+p.y*.7+_Time.y*.26),b=sin(p.y*3.4-p.x*.6-_Time.y*.22);
    return pow(saturate(1-abs(a+b)*1.8),5)*.24;
}
half3 ReefLight(float3 world,half3 normal,half3 albedo,half smoothness,half translucency)
{
    InputData input=(InputData)0;
    input.positionWS=world;input.normalWS=normalize(normal);input.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(world));
    input.shadowCoord=TransformWorldToShadowCoord(world);
    // Water scatters skylight into backlit surfaces even without a baked probe volume.
    input.bakedGI=SampleSH(input.normalWS)+half3(.24,.34,.35);input.shadowMask=half4(1,1,1,1);
    input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(TransformWorldToHClip(world));
    SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.alpha=1;surface.occlusion=1;surface.smoothness=smoothness;surface.normalTS=half3(0,0,1);
    Light light=GetMainLight(input.shadowCoord);
    surface.emission=albedo*(ReefCaustic(world)*saturate(normal.y*.65+.35)*light.shadowAttenuation
        +translucency*pow(saturate(dot(-normal,light.direction)),2)*.28);
    return UniversalFragmentPBR(input,surface).rgb;
}
#endif
