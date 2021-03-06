#ifndef CRP_LIGHTING_INCLUDED
#define CRP_LIGHTING_INCLUDED
// 计算光照相关的shader

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

// 方向光数据，由CPU传入
CBUFFER_START(_CrystalLight)
    //float3 _DirectionLightColor;
    //float3 _DirectionLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

// 获取方向光数量
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

// 获取方向光的阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    // 获取当前灯光的阴影强度
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    // 获取当前灯光对应的在阴影图集中的索引
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    // 获取法线偏移
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;

    return  data;
}

// 获取方向光属性
Light GetDirectionLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;

    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    return light;
}



#endif