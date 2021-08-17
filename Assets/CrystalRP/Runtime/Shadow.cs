using System;
using  UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.Rendering;



public class Shadow
{
    private const string _BufferName = "Shadows";
    private static int _DirectionalShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static int _DirectionalShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
    private static int _CascadeCountID = Shader.PropertyToID("_CascadeCount");
    private static int _CascadeCullingSpheredsID = Shader.PropertyToID("_CascadeCullingSpheres");
    private static int _ShadowDistanceId = Shader.PropertyToID("_ShadowDistance");

    private static Matrix4x4[] _DirectionalShadowMatrices = new Matrix4x4[MaxShadowDirectionalLightCount*MaxCascades];
    private static Vector4[] _CascadeCullingSpheres = new Vector4[MaxCascades];

    private CommandBuffer _Buffer = new CommandBuffer() {name = _BufferName};
    private ScriptableRenderContext _Context;
    private CullingResults _CullingResults;
    private ShadowSettings _ShadowSettings;

    // 支持阴影的最大方向光数量
    private const int MaxShadowDirectionalLightCount = 4;
    private const int MaxCascades = 4;

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
        int tiles = _ShadowDirectionalLightCount * _ShadowSettings.DirectionalSettings.CascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < _ShadowDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        
        // 将级联和保卫求数据发送到GPU 
        _Buffer.SetGlobalFloat(_ShadowDistanceId, _ShadowSettings.MaxDistance);
        _Buffer.SetGlobalInt(_CascadeCountID, _ShadowSettings.DirectionalSettings.CascadeCount);
        _Buffer.SetGlobalVectorArray(
            _CascadeCullingSpheredsID, _CascadeCullingSpheres
        );
        _Buffer.SetGlobalMatrixArray(_DirectionalShadowMatricesID, _DirectionalShadowMatrices);
        _Buffer.EndSample(_BufferName);
        ExecuteBuffer(); 
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowDirectionalLight light = _ShadowDirectionalLights[index];
        var shadowSettings =
            new ShadowDrawingSettings(_CullingResults, light.VisableLightIndex);

        int cascadeCount = _ShadowSettings.DirectionalSettings.CascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _ShadowSettings.DirectionalSettings.CascadeRatios;
        for (int i = 0; i < cascadeCount; i++)
        {
            _CullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.VisableLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );

            shadowSettings.splitData = splitData;

            if (index == 0)
            {
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                _CascadeCullingSpheres[i] = cullingSphere;
            }
            
            SetTileViewport(index, split, tileSize);
            // 重新调整图块索引，在光源的index上加上casdcade的索引
            int tileIndex = tileOffset + i;
            _DirectionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize), split
            );
            _Buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();
            _Context.DrawShadows(ref shadowSettings);
        }
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

            // 返回阴影强度和阴影图块的索引，这里要乘以级联数量
            return new Vector2(light.shadowStrength, _ShadowSettings.DirectionalSettings.CascadeCount*_ShadowDirectionalLightCount++);
        }
        
        return Vector2.zero;
    }
    
}
