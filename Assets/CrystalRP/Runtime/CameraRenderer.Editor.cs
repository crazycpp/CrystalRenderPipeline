using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    partial void DrawUnsuportShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();

#if UNITY_EDITOR
    // 使用错误材质的物体，专用一个才是来渲染
    private static Material _errorMaterial;

    private static ShaderTagId[] _legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

    partial void DrawUnsuportShaders()
    {
        if (_errorMaterial == null)
        {
            _errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(_legacyShaderTagIds[0], new SortingSettings(_camera))
        {
            overrideMaterial = _errorMaterial
        };
        // 由于第一个元素用于构建drawingsetting了，因此这里循环从1开始
        for (int i = 1; i < _legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, _legacyShaderTagIds[i]);
        }

        var filteringSetting = FilteringSettings.defaultValue;

        // 绘制不支持的ShaderTag类型的物体
        _Context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSetting);
    }
#endif

#if UNITY_EDITOR
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            _Context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _Context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// 在Game视图绘制的几何体也绘制到Scene视图中
    /// </summary>
    partial void PrepareForSceneWindow()
    {
        if (_camera.cameraType == CameraType.SceneView)
        {
            // 如果切换到Scene视图，调用此方法完绘制
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }
#endif

#if UNITY_EDITOR
    private string _sampleName { get; set; }
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        _buffer.name = _sampleName = _camera.name;
        Profiler.EndSample();
    }
#else
    const string _sampleName = _bufferName;
#endif
}