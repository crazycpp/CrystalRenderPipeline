#ifndef CRP_CORE_INCLUDED
#define CRP_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/common.hlsl"
#include "UnityInput.hlsl"
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
/*

// 把定点从对象空间变换到世界空间
float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(unity_ObjectToWorld, float4(positionOS, 1)).xyz;
}


float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(unity_MatrixVP, float4(positionWS, 1));
}
*/

#endif