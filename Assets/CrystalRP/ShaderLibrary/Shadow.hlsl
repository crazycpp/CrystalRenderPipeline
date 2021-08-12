#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

// 阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END


// 阴影数据
struct DirectionalShadowData
{
    float strength;
    int tileIndex;
};


// 采样阴影图集
float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

// 计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
    if (data.strength <= 0.0)
    {
        return 1.0;
    }
    
    // 通过阴影变换矩阵和表面世界坐标得到在阴影纹理空间中的位置，然后就对图集进行采样
    float4 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1.0));
    float shadow = SampleDirectionalShadowAtlas(positionSTS.xyz);
    return lerp(1.0, shadow, data.strength);
}



#endif