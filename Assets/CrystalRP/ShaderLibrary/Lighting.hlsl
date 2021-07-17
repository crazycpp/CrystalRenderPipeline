#ifndef CRP_LIGHT_INCLUDED
#define CRP_LIGHT_INCLUDED


// 计算入射光照
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)*light.color);
}

float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light);
}

float3 GetLighting(Surface surface)
{
    float3 finalColor = 0.0;
    for (int i=0; i<GetDirectionalLightCount(); i++)
    {
        finalColor += GetLighting(surface, GetDirectionLight(i));
    }

    return finalColor;
}

#endif