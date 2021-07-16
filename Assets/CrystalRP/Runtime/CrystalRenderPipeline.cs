using UnityEngine;
using UnityEngine.Rendering;

public class CrystalRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();


    private bool _useDynamicBatching, _useGPUInctancing;
    public CrystalRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        _useDynamicBatching = useDynamicBatching;
        _useGPUInctancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
    }
    
    protected override void Render(ScriptableRenderContext Content, Camera[] Cameras)
    {
        foreach (Camera camera in Cameras) {
            renderer.Render(Content, camera, _useDynamicBatching, _useGPUInctancing);
        }
    }
}
