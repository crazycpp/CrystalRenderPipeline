using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShadowSettings 
{
    [Min(0f)]
    public float MaxDistance = 100f;

    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    // 方向光阴影
    [System.Serializable]
    public struct Directional
    {
        public TextureSize AtlasSize;

        // 级联数量
        [Range(1,4)]
        public int CascadeCount;
        
        // 级联比例
        [Range(0, 1)] public float CascadeRatio1, CascadeRatio2, CascadeRatio3;
        
        public Vector3 CascadeRatios => new Vector3(CascadeRatio1, CascadeRatio2, CascadeRatio3);
    }

    public Directional DirectionalSettings = new Directional()
    {
        AtlasSize = TextureSize._1024,
        CascadeCount = 4,
        CascadeRatio1 = 0.1f,
        CascadeRatio2 = 0.25f,
        CascadeRatio3 = 0.5f 
    };
}
