Shader "CrystalRenderPipeline/Unlit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white"{}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)

        // alpha test threshold
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)]_CLipping("Alpha Clipping", Float) = 0

        // for alpha blend
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend ("Src Blend", FLoat) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Dst Blend", Float) = 0
        [Enum(Off,0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {
        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing

            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "CrystalShaderGUI"
}