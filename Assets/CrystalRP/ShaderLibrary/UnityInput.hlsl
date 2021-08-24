#ifndef CRP_UNITY_INPUT_INCLUDED
#define CRP_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 UNITY_LODFade;
real4 unity_WorldTransformParams;
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

CBUFFER_END

float3 _WorldSpaceCameraPos;

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;


#endif
