#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// 阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CrystalShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT*MAX_CASCADE_COUNT];
    float _ShadowDistance;
    // 阴影过渡距离
    float4 _ShadowDistanceFade;
    // 级联数据
    float4 _CascadeData[MAX_CASCADE_COUNT];

    // 阴影图集尺寸
    float4 _ShadowAtlasSize;
CBUFFER_END


// 阴影数据
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    // 法线偏差
    float normalBias;
};

struct ShadowData
{
    // 阴影强度
    float strength;
    // 级联索引
    int cascadeIndex;

    // 混合级联
    float cascadeBlend;
};


float FadedShadowStrength (float distance, float scale, float fade) {
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData data;
    data.cascadeBlend = 1.0f;
    // 得到有线性过渡的阴影强度
    data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    int i;
    //如果物体表面到球心的平方距离小于球体半径的平方，就说明该物体在这层级联包围球中，得到合适的级联层级索引
    for (i=0; i<_CascadeCount; i++)
    {
        
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSequared(surfaceWS.position, sphere.xyz);
        if (i == _CascadeCount -1)
        {
            data.strength *=FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
        }

        if (distanceSqr < sphere.w)
        {
            float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            if (i== _CascadeCount -1)
            {
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;
        }
    }

    // 超出了阴影范围外，则把阴影强度改为0
    if (i==_CascadeCount)
    {
        data.strength = 0;
    }
    #if defined (_CASCADE_BLEND_DITHER)
     else if (data.cascadeBlend <surfaceWS.dither)
     {
         i += 1;
     }
    #endif

    #if !defined(_CASCADE_BLEND_SOFT)
        data.cascadeBlend = 1.0f;
    #endif
    

    
    data.cascadeIndex = i;
    return data;
}


// 采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
    // 样本权重
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    // 样本位置

    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;

    // 第一个参数中xy表示图集图素大小，第二个参数表示原始样本的位置，后梁个样本和权重和样本位置
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xyz, weights, positions);
    float shadow = 0;
    for (int i=0; i<DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        // 便利所有滤波样本，将所有的样本权重进行累加
        shadow += weights[i]*SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
    #else
    return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

// 计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data, ShadowData globalShadowData, Surface surfaceWS)
{
    #if !defined(_RECEIVE_SHADOWS)
    return 1.0f;
    #endif

    if (data.strength <= 0.0)
    {
        return 1.0;
    }

    // 计算发现偏差
    float3 normalBias = surfaceWS.normal *(data.normalBias * _CascadeData[globalShadowData.cascadeIndex].y);
    
    // 通过加上法线偏移后的表面顶点位置，得到在阴影纹理空间的新位置，然后对图集进行采样
    float4 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position + normalBias, 1.0));
    float shadow = FilterDirectionalShadow(positionSTS.xyz);
    // 如果级联混合小于1代表在级联层级过渡区域中，必须从给下一个几连中采样并在两个值之间进行插值
    if (globalShadowData.cascadeBlend < 1.0f)
    {
        normalBias = surfaceWS.normal * (data.normalBias * _CascadeData[globalShadowData.cascadeIndex+1].y);
        positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex+1], float4(surfaceWS.position + normalBias, 1.0));
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, globalShadowData.cascadeBlend);
    }
    return lerp(1.0, shadow, data.strength);
}



#endif