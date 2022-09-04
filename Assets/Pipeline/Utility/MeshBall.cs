using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBall : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    private static int limit = 1023;
    private Matrix4x4[] matrices = new Matrix4x4[limit];
    private Vector4[] colors = new Vector4[limit];
    private float[] metallics = new float[limit];
    private float[] smoothnesses = new float[limit];

    private MaterialPropertyBlock block;

    private void Awake()
    {
        for (int i = 0; i < limit; ++i)
        {
            var translation = UnityEngine.Random.insideUnitSphere * 10.0f;
            var rotation = Quaternion.identity;
            var scale = Vector3.one;
            matrices[i] = Matrix4x4.TRS(translation, rotation, scale);

            var r = UnityEngine.Random.value;
            var g = UnityEngine.Random.value;
            var b = UnityEngine.Random.value;
            colors[i] = new Vector4(r, g, b, 1.0f);

            metallics[i] = UnityEngine.Random.Range(0.0f, 1.0f);
            
            smoothnesses[i] = UnityEngine.Random.Range(0.05f, 0.95f);
        }
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, colors);
            block.SetFloatArray(metallicId, metallics);
            block.SetFloatArray(smoothnessId, smoothnesses);
        }
        UnityEngine.Graphics.DrawMeshInstanced(mesh, 0, material, matrices, limit, block);
    }
}
