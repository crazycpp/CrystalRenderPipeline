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
    private ShaderTagId _litShaderTagId = new ShaderTagId("CrystalLit");

    private Lighting _lighting = new Lighting();

    public void Render (ScriptableRenderContext Context, Camera TheCamera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings) {
        _Context = Context;
        _camera = TheCamera;
        
        PrepareBuffer();

        // 次操作可能会给Scene视图中增加一几何体，所以我需要在Cull之前调用这个方法
        PrepareForSceneWindow();
        
        if (!Cull(shadowSettings.MaxDistance))
        {
            return;
        }
        
        _buffer.BeginSample(_sampleName);
        ExecuteBuffer();
        _lighting.Setup(Context, _cullingResults, shadowSettings);
        _buffer.EndSample(_sampleName);
        
        Setup();
        //绘制几何体
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        
        DrawUnsuportShaders();
        
        DrawGizmos();
        
        _lighting.Cleanup();
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

    bool Cull(float maxShadowDistance)
    {
        ScriptableCullingParameters p;
        if (_camera.TryGetCullingParameters(out p))
        {
            // 将最大阴影距离和相机远裁剪面做比较，取最小值作为阴影的渲染距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            _cullingResults = _Context.Cull(ref p);
            return true;
        }

        return false;
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // 设置绘制顺序和指定渲染相机
        var sortingSettings = new SortingSettings(_camera) {criteria = SortingCriteria.CommonOpaque};
        // 设置渲染的shader pass和渲染排序
        var drawSettings = new DrawingSettings(_unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching =  useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe |
                            PerObjectData.OcclusionProbeProxyVolume | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume,
        };
        
        // 同时渲染CrystalLit表示的pass
        drawSettings.SetShaderPassName(1, _litShaderTagId);
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
