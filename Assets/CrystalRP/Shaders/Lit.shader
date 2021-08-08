Shader "CrystalRenderPipeline/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white"{}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        // alpha test threshold
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0

        // for alpha blend
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", FLoat) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Float) = 0
        [Enum(Off,0, On, 1)] _ZWrite("Z Write", Float) = 1

        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "LitMode" = "CrystalLit"
            }
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            HLSLPROGRAM
            #pragma vertex litPassVertex
            #pragma fragment litPassFragment

            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile_instancing

            #include "LitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "CrystalShaderGUI"
}