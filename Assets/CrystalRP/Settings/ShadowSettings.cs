using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShadowSettings 
{
    [Min(0.01f)]
    public float MaxDistance = 100f;
    
    //阴影过渡距离
    [Range(0.001f, 1f)]
    public float DistanceFade = 0.1f;

    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    // PCF滤波模式
    public enum FilterMode
    {
        PCF2x2,
        PCF3x3,
        PCF5x5,
        PCF7x7,
    }

    public enum CascadeBlendMode
    {
        Hard,
        Soft,
        Dither,
    }

    // 方向光阴影
    [System.Serializable]
    public struct Directional
    {
        public TextureSize AtlasSize;

        public FilterMode PCFFilter;
        
        // 级联数量
        [Range(1,4)]
        public int CascadeCount;
        
        // 级联比例
        [Range(0, 1)] public float CascadeRatio1, CascadeRatio2, CascadeRatio3, CascadeFade;
        
        public Vector3 CascadeRatios => new Vector3(CascadeRatio1, CascadeRatio2, CascadeRatio3);

        public CascadeBlendMode CascadeBlend;
    }

    public Directional DirectionalSettings = new Directional()
    {
        AtlasSize = TextureSize._1024,
        PCFFilter = FilterMode.PCF2x2,
        CascadeCount = 4,
        CascadeRatio1 = 0.1f,
        CascadeRatio2 = 0.25f,
        CascadeRatio3 = 0.5f,
        CascadeFade = 0.1f,
        CascadeBlend = CascadeBlendMode.Hard,
    };
}
