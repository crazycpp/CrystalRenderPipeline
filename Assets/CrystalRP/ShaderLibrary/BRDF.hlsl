#ifndef CRYSTAL_BRDF_INCLUDED
#define CRYSTAL_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

// 一些不导电物质，如玻璃，塑料等非金属物体，还会有些点光从表面反射出来，平均为0.04.
#define MIN_REFLECVITY 0.04

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECVITY;
    return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }

    // 根据能量守恒定律，表面反射的光能不能超过入社的光能，因此，镜面反射的遗憾色应该等于表面颜色减去漫反射颜色
    // 但是事实情况是，金属影响镜面反射的颜色，而非金属不影响。非金属的镜面反射应该是白色的。
    // 因此，最终是在金属度在最小反射率和表面颜色之间进行插值得到BRDF的镜面反射颜色
    brdf.specular = lerp(MIN_REFLECVITY, surface.color, surface.metallic);

    // 粗糙度和光环度相反，只需要使用1减去光滑度即可
    // 使用PerceptualSmoothnessToPerceptualRoughness方法，通过感知到的光环度得到粗糙度，然后通过PerceptualRoughnessToRoughness方法将感知到的粗糙度平方，得到实际的粗糙度。
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    return brdf;
}

// 根据公式得到镜面反射强度
// s = r² / d²max(0.1, (L.H)²)n
// d = (N.H)²(r²-1)+1.0001
// r代表粗糙度，N代表表面法线，L代表光照方向，V代表视角方向，H代表归一化的L+V，它是光和视角方向的中间对角线向量。
float SpecularStrength(Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2*(r2-1.0)+1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / ( d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif
