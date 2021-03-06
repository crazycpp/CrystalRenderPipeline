#ifndef CRP_LIGHT_INCLUDED
#define CRP_LIGHT_INCLUDED


// 计算入射光照
float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)*light.attenuation)*light.color;
}

// 入社光照乘以BRDF中的漫反射，得到最终颜色
float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light)*DirectBRDF(surface, brdf, light);
}
 
float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi)
{
    // 得到表面的阴影数据
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    //return gi.shadowMask.shadows.rgb;
    float3 finalColor = gi.diffuse * brdf.diffuse;
    for (int i=0; i<GetDirectionalLightCount(); i++)
    {
        finalColor += GetLighting(surfaceWS, brdf, GetDirectionLight(i, surfaceWS, shadowData));
    }

    return finalColor;
}

#endif