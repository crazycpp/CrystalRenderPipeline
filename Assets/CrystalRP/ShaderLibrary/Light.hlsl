#ifndef CRP_LIGHTING_INCLUDED
#define CRP_LIGHTING_INCLUDED
// 计算光照相关的shader

struct Light
{
    float3 color;
    float3 direction;
};

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

// 方向光数据，由CPU传入
CBUFFER_START(_CrystalLight)
    //float3 _DirectionLightColor;
    //float3 _DirectionLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

// 获取方向光数量
int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

// 获取方向光属性
Light GetDirectionLight(int index)
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    return light;
}

#endif