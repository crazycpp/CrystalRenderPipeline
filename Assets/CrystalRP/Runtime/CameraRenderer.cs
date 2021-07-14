using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext _Context;
    private Camera _camera;

    private const string _bufferName = "Render Camera";
    private CommandBuffer _buffer = new CommandBuffer {name = _bufferName};

    // 保存当前摄像机的剔除结果
    private CullingResults _cullingResults;

    private ShaderTagId _unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    public void Render (ScriptableRenderContext Context, Camera TheCamera) {
        _Context = Context;
        _camera = TheCamera;
        
        PrepareBuffer();

        // 次操作可能会给Scene视图中增加一几何体，所以我需要在Cull之前调用这个方法
        PrepareForSceneWindow();
        
        if (!Cull())
        {
            return;
        }
        
        Setup();
        DrawVisibleGeometry();
        DrawUnsuportShaders();
        DrawGizmos();
        Submit();
    }

    void Setup()
    {
        _Context.SetupCameraProperties(_camera);
        CameraClearFlags flags = _camera.clearFlags;
        _buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? _camera.backgroundColor.linear:Color.clear);
        _buffer.BeginSample(_sampleName);
        ExecuteBuffer();
    }

    bool Cull()
    {
        ScriptableCullingParameters p;
        if (_camera.TryGetCullingParameters(out p))
        {
            _cullingResults = _Context.Cull(ref p);
            return true;
        }

        return false;
    }

    void DrawVisibleGeometry()
    {
        // 设置绘制顺序和指定渲染相机
        var sortingSettings = new SortingSettings(_camera) {criteria = SortingCriteria.CommonOpaque};
        // 设置渲染的shader pass和渲染排序
        var drawSettings = new DrawingSettings(_unlitShaderTagId, sortingSettings);
        // 先绘制不透明队列
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        _Context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
        // 绘制天空
        _Context.DrawSkybox(_camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        
        _Context.DrawRenderers(_cullingResults, ref drawSettings, ref filterSettings);
    }

    void Submit()
    {
        _buffer.EndSample(_sampleName);
        ExecuteBuffer();
        _Context.Submit();
    }

    void ExecuteBuffer()
    {
        _Context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}
