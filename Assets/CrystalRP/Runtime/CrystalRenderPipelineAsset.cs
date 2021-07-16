using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName = "Rendering/CreateCrystalRenderpipeline")]
public class CrystalRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool _useDynamicBatching = true, _UseGPUInstancing = true, _useSRPBatcher = true; 
    protected override RenderPipeline CreatePipeline()
    {
        return new CrystalRenderPipeline(_useDynamicBatching, _UseGPUInstancing, _useSRPBatcher);
    }
}
