using System;
using  UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.Rendering;



public class Shadow
{
    private const string _BufferName = "Shadows";
    private static int _DirectionalShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int _DirectionalShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");

    private static Matrix4x4[] _DirectionalShadowMatrices = new Matrix4x4[MaxShadowDirectionalLightCount];

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
        _Buffer.SetGlobalMatrixArray(_DirectionalShadowMatricesID, _DirectionalShadowMatrices);
        _Buffer.EndSample(_BufferName);
        ExecuteBuffer(); 
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = _ShadowDirectionalLights[index];
        var shadowSettings =
            new ShadowDrawingSettings(_CullingResults, light.VisableLightIndex);
        _CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.VisableLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
        );

        shadowSettings.splitData = splitData;
        SetTileViewport(index, split, tileSize);
        _DirectionalShadowMatrices[index] = ConvertToAtlasMatrix(
            projectionMatrix * viewMatrix,
            SetTileViewport(index, split, tileSize), split
        );
        _Buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        _Context.DrawShadows(ref shadowSettings);
        

    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        // 设置渲染视口，拆分成多个块
        _Buffer.SetViewport(new Rect(offset.x * tileSize, offset.y*tileSize, tileSize, tileSize));

        return offset;
    }

    // 把阴影变换矩阵转化到阴影图块空间中去，用于把物体从实际额空间变化到图块空间
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        // 如果使用了反向的Zbuffer，注意 opengl中 0 是0深度，1是最大深度。在DirectX中0是0深度，-1是最大深度
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        //设置矩阵坐标
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        
        return m;
    }

    void ExecuteBuffer()
    {
        _Context.ExecuteCommandBuffer(_Buffer);
        _Buffer.Clear();
    }
    
    
    
    // 存储可见光的阴影数据
    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_ShadowDirectionalLightCount < MaxShadowDirectionalLightCount && light.shadows != LightShadows.None &&
            light.shadowStrength > 0.0f && _CullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            _ShadowDirectionalLights[_ShadowDirectionalLightCount] = new ShadowDirectionalLight(){VisableLightIndex =  visibleLightIndex};

            return new Vector2(light.shadowStrength, _ShadowDirectionalLightCount++);
        }
        
        return Vector2.zero;
    }
    
}
