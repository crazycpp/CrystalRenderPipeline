using System;
using  UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.Rendering;



public class Shadow
{
    private const string _BufferName = "Shadows";
    private static int _DirectionalShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAltas");


    private CommandBuffer _Buffer = new CommandBuffer() {name = _BufferName};
    private ScriptableRenderContext _Context;
    private CullingResults _CullingResults;
    private ShadowSettings _ShadowSettings;

    // 支持阴影的最大方向光数量
    private const int MaxShadowDirectionalLightCount = 4;

    private int _ShadowDirectionalLightCount;
    struct ShadowDirectionalLight
    {
        public int VisableLightIndex;
    }
    
    // 保存可投影方向光的索引
    ShadowDirectionalLight[] _ShadowDirectionalLights = new ShadowDirectionalLight[MaxShadowDirectionalLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        _Context = context;
        _CullingResults = cullingResults;
        _ShadowSettings = shadowSettings;

        _ShadowDirectionalLightCount = 0;
    }

    public void Render()
    {
        if (_ShadowDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }

    public void Cleanup()
    {
        _Buffer.ReleaseTemporaryRT(_DirectionalShadowAtlasID);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int) _ShadowSettings.DirectionalSettings.AtlasSize;
        _Buffer.GetTemporaryRT(_DirectionalShadowAtlasID, atlasSize, atlasSize,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        
        // 把渲染数据存储的到_DirectionalShadowAtlas中
        _Buffer.SetRenderTarget(_DirectionalShadowAtlasID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        // 清除深度缓冲区
        _Buffer.ClearRenderTarget(true, false, Color.clear);
        
        _Buffer.BeginSample(_BufferName);
        ExecuteBuffer();
        // 重新分割阴影图快的大小和数量
        int split = _ShadowDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;
        for (int i = 0; i < _ShadowDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        _Buffer.EndSample(_BufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = _ShadowDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_CullingResults, light.VisableLightIndex);

        _CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.VisableLightIndex, 0, 1, Vector3.zero,tileSize, 0,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

        shadowSettings.splitData = splitData;
        SetTileViewPort(index, split, tileSize);
        _Buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        _Context.DrawShadows(ref shadowSettings);

    }

    void SetTileViewPort(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        // 设置渲染视口，拆分成多个块
        _Buffer.SetViewport(new Rect(offset.x * tileSize, offset.y*tileSize, tileSize, tileSize));
    }

    void ExecuteBuffer()
    {
        _Context.ExecuteCommandBuffer(_Buffer);
        _Buffer.Clear();
    }
    
    
    
    // 存储可见光的阴影数据
    public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_ShadowDirectionalLightCount < MaxShadowDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0.0f && _CullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _ShadowDirectionalLights[_ShadowDirectionalLightCount++] = new ShadowDirectionalLight(){VisableLightIndex =  visibleLightIndex};
        }
    }
    
}
