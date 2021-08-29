#ifndef CRP_LIT_PASS_INCLUDED
#define CRP_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"


// 顶点着色器的输入参数
struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS:SV_POSITION;
    float3 positionWS:VAR_POSITION;
    float3 normalWS:VAR_NORMAL;
    float2 baseUV :VAR_BASE_UV;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings litPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input)
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    // 计算偏移后的uv坐标
    //float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    //output.baseUV = input.baseUV * baseST.xy + baseST.zw;

    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 litPassFragment(Varyings input):SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    //float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    //float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    //float4 color = baseMap*baseColor;
    float4 color = GetBase(input.baseUV);
    
#if defined(_CLIPPING)
    // 透明度低于阈值的片元进行舍弃
    //clip(color.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_Cutoff));
    clip(color.a - GetCutoff());
#endif

    Surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.color = color.rgb;
    surface.alpha = color.a;
    //surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    //surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    surface.metallic = GetMetallic();
    surface.smoothness = GetSmoothness();
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.depth = -TransformWorldToView(input.positionWS).z;
    // 计算出一个阴影抖动值
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface, true);
#else
    BRDF brdf = GetBRDF(surface);
#endif

    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);
    float3 finalcolor = GetLighting(surface, brdf, gi);
    finalcolor += GetEmission(input.baseUV);

    return float4(finalcolor, surface.alpha);
}

#endif