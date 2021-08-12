#ifndef CRP_SURFACE_INCLUDED
#define CRP_SURFACE_INCLUDED


// 存储表面相关信息
struct Surface
{
    float3 position;
    float3 normal;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDirection;
};

#endif