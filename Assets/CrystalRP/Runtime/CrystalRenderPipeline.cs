using UnityEngine;
using UnityEngine.Rendering;

public class CrystalRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    
    protected override void Render(ScriptableRenderContext Content, Camera[] Cameras)
    {
        foreach (Camera camera in Cameras) {
            renderer.Render(Content, camera);
        }
    }
}
