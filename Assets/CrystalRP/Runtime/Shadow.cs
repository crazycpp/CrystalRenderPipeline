using  UnityEngine;
using UnityEngine.Experimental.Rendering;
using  UnityEngine.Rendering;



public class Shadow
{
    private const string _BufferName = "Shadows";


    private CommandBuffer _Buffer = new CommandBuffer() {name = _BufferName};
    private ScriptableRenderContext _Context;
    private CullingResults _CullingResults;
    private ShadowSettings _ShadowSettings;

    // 支持阴影的最大方向光数量
    private const int MaxShadowDirectionalLightCount = 1;

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

    void RenderDirectionalShadows()
    {
        
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
