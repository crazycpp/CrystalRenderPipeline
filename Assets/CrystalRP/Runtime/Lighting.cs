using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    // 最多支持的方向光数量
    private const int MaxDirectionalLightCount = 4;

    private const string _bufferName = "Lighting";

    private CommandBuffer _buffer = new CommandBuffer
    {
        name = _bufferName
    };

    private int _directionalLightCountID = Shader.PropertyToID("_DirectionalLightCount");
    private int _directionalLightColorsID = Shader.PropertyToID("_DirectionalLightColors");
    private int _directionalLightDirectionsID = Shader.PropertyToID("_DirectionalLightDirections");

    private static Vector4[] _directionalLightColors = new Vector4[MaxDirectionalLightCount];
    private static Vector4[] _directionalLightDirections = new Vector4[MaxDirectionalLightCount];

    private CullingResults _cullingResults;

    private Shadow _Shadow = new Shadow();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        _cullingResults = cullingResults;
        _buffer.BeginSample(_bufferName);
        // 阴影初始化
        _Shadow.Setup(context, cullingResults, shadowSettings);
        // 设置灯光
        SetupLights();
        // 渲染阴影
        _Shadow.Render();
        _buffer.EndSample(_bufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    // 将场景主光源的颜色和方向传递到GPU
    void SetupDirectionLight(int index, ref VisibleLight visibleLight)
    {
        _directionalLightColors[index] = visibleLight.finalColor; //光源最终颜色是通过finalcolor取
        _directionalLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // 光照方向是通过localtoWorldMatrix取得，第 2 列为方向向量。
        _Shadow.ReserveDirectionalShadows(visibleLight.light, index);
    }

    // 向GPU发送多光源数据
    void SetupLights()
    {
        // 取得所有的可见光
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

        int lightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            var visableLight = visibleLights[i];
            if (visableLight.lightType == LightType.Directional)
            {
                SetupDirectionLight(lightCount++, ref visableLight);
            }

            if (lightCount >= MaxDirectionalLightCount)
            {
                break;
            }
        }
        
        _buffer.SetGlobalInt(_directionalLightCountID, lightCount);
        _buffer.SetGlobalVectorArray(_directionalLightColorsID, _directionalLightColors);
        _buffer.SetGlobalVectorArray(_directionalLightDirectionsID, _directionalLightDirections);
    }
}