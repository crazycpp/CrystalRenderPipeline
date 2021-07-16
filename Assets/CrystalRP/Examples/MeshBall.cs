using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBall : MonoBehaviour
{
    private static int baseColorID = Shader.PropertyToID("_BaseColor");

    [SerializeField] 
    private Mesh _mesh = default;

    [SerializeField] private Material _material = default;

    private Matrix4x4[] _matrices = new Matrix4x4[1023];
    private Vector4[] _baseColors = new Vector4[1023];

    private MaterialPropertyBlock _block;
    
    // Start is called before the first frame update
    void Awake()
    {
        for (int i = 0; i < _matrices.Length; i++)
        {
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f,
                Quaternion.Euler(Random.value * 360, Random.value * 360, Random.value * 360),
                    Vector3.one * Random.Range(0.5f, 1.5f));
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1.0f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
            _block.SetVectorArray(baseColorID, _baseColors);
        }
        
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block);
        
    }
}
