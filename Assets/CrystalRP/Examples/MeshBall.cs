using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class MeshBall : MonoBehaviour
{
    private static int _BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int _MetallicId = Shader.PropertyToID("_Metallic");
    private static int _SmoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh _Mesh = default;

    [SerializeField]
    Material _Material = default;

    [SerializeField]
    LightProbeProxyVolume _LightProbeVolume = null;
	
    Matrix4x4[] _Matrices = new Matrix4x4[1023];
    Vector4[] _BaseColors = new Vector4[1023]; 
    float[] _Metallic = new float[1023];
    float[] _Smoothness = new float[1023];

    MaterialPropertyBlock block;

    void Awake () {
        for (int i = 0; i < _Matrices.Length; i++) {
            _Matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f,
                Quaternion.Euler(
                    Random.value * 360f, Random.value * 360f, Random.value * 360f
                ),
                Vector3.one * Random.Range(0.5f, 1.5f)
            );
            _BaseColors[i] =
                new Vector4(
                    Random.value, Random.value, Random.value,
                    Random.Range(0.5f, 1f)
                );
            _Metallic[i] = Random.value < 0.25f ? 1f : 0f;
            _Smoothness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    void Update () {
        if (block == null) {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(_BaseColorId, _BaseColors);
            block.SetFloatArray(_MetallicId, _Metallic);
            block.SetFloatArray(_SmoothnessId, _Smoothness);

            if (!_LightProbeVolume) {
                var positions = new Vector3[1023];
                for (int i = 0; i < _Matrices.Length; i++) {
                    positions[i] = _Matrices[i].GetColumn(3);
                }
                var lightProbes = new SphericalHarmonicsL2[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(
                    positions, lightProbes, null
                );
                block.CopySHCoefficientArraysFrom(lightProbes);
            }
        }
        Graphics.DrawMeshInstanced(
            _Mesh, 0, _Material, _Matrices, 1023, block,
            ShadowCastingMode.On, true, 0, null,
            _LightProbeVolume ?
                LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided,
            _LightProbeVolume
        );
    }
}