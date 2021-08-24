// 全局照明库文件
#ifndef CRP_GI_INCLUDED
#define CRP_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if defined(LIGHTMAP_ON)
#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHTMAP_UV;
#define TRANSFER_GI_DATA(input, output)  output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
#define GI_ATTRIBUTE_DATA
#define GI_VARYINGS_DATA
#define TRANSFER_GI_DATA(input, output)
#define GI_FRAGMENT_DATA(input) 0.0
#endif

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

// 采样光照贴图
float3 SampleLightMap(float2 lightMapUV)
{
#if defined(LIGHTMAP_ON)
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),lightMapUV, float4(1.0, 1.0, 0.0, 0.0),
    #if defined(UNITY_LIGHTMAP_FULL_HDR)
    false,
    #else
    true,
    #endif
    float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0)
    );
#else
    return 0.0;

#endif
}

// 光照探针采样
float3 SampleLightProbe(Surface surfaceWS)
{
    #if defined(LIGHTMAP_ON)
    return 0.0;
    #else
        float4 coefficients[7];
        coefficients[0] = unity_SHAr;
        coefficients[1] = unity_SHAg;
        coefficients[2] = unity_SHAb;
        coefficients[3] = unity_SHBr;
        coefficients[4] = unity_SHBg;
        coefficients[5] = unity_SHBb;
        coefficients[6] = unity_SHC;
        return max(0.0, SampleSH9(coefficients, surfaceWS.normal));
    #endif
}

struct GI
{
    // 漫反射颜色
    float3 diffuse;
};

GI GetGI(float2 lightMapUV, Surface surfaceWS)
{
    GI gi;
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
    return gi;
}

#endif
