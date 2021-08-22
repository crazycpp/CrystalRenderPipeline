using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;

public class CrystalShaderGUI : ShaderGUI
{
    enum ShadowMode
    {
        On,
        Clip,
        Dither,
        Off
    }
    
    private MaterialEditor _Editor;
    private Object[] _Materials;
    private MaterialProperty[] _Properties;

    private bool _ShowPresets;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        _Editor = materialEditor;
        _Materials = _Editor.targets;
        _Properties = properties;
        EditorGUI.BeginChangeCheck();

        _ShowPresets = EditorGUILayout.Foldout(_ShowPresets, "Presets", true);
        if (_ShowPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
        //如果材质属性有被更改，检查阴影模式的设置状态
        if (EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
        }
    }

    ShadowMode Shadows
    {
        set
        {
            if (SetProperty("_Shadows", (float)value)) {
                SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    private bool PremutiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float) value);
    }

    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float) value);
    }

    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }


    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in _Materials)
            {
                m.renderQueue = (int) value;
            }
        }
    }

    bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            _Editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremutiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;

        }
    }

    void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremutiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    // 标准透明模式
    void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremutiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusDstAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    void TransparentPreset()
    {
        if (PresetButton("Transparent"))
        {
            Clipping = false;
            PremutiplyAlpha = true;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusDstAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    /// <summary>
    /// 设置材质属性
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool SetProperty(string name, float value)
    {
        MaterialProperty property = FindProperty(name, _Properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 设置关键字
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="enable"></param>
    void SetKeyword(string keyword, bool enable)
    {
        if (enable)
        {
            foreach (Material m in _Materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in _Materials)
            {
                m.DisableKeyword(keyword);
            }
        }
    }

    /// <summary>
    /// 设置关键字和属性
    /// </summary>
    /// <param name="name"></param>
    /// <param name="keyword"></param>
    /// <param name="value"></param>
    void SetProperty(string name, string keyword, bool value)
    {
        SetProperty(name, value?1f:0f);
        SetKeyword(keyword, value);
    }

    void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadow", _Properties, false);
        if (shadows == null || shadows.hasMixedValue)
        {
            return;
        }

        bool enalbled = shadows.floatValue < (float) ShadowMode.Off;
        foreach (Material m in _Materials)
        {
            m.SetShaderPassEnabled("ShowCaster", enalbled);
        }

    }
}
