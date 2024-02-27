//using System;
using UnityEngine;

namespace Graphics
{
    public class PerObjectMaterialParameters : MonoBehaviour
    {
        static readonly int baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int cutoffId = Shader.PropertyToID("_Cutoff");
        static readonly int metallicId = Shader.PropertyToID("_Metallic");
        static readonly int smoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int emissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color baseColor = Color.white;
        [SerializeField, Range(0.0f, 1.0f)] private float cutoff = 0.5f;
        [SerializeField, Range(0.0f, 1.0f)] private float metallic = 0.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float smoothness = 0.5f;
        [SerializeField, ColorUsage(false, true)] private Color emissionColor = Color.black;
        
        private Material material;

        private void OnValidate()
        {
            if (material == null)
            {
                material = GetComponent<Renderer>().sharedMaterial;
            }

            baseColor = new Color(Random.value, Random.value, Random.value);
            material.SetColor(baseColorId, baseColor);

            //cutoff = Mathf.Round(Random.Range(0.0f, 1.0f) * 100.0f) / 100.0f;
            material.SetFloat(cutoffId, cutoff);

            //metallic = Mathf.Round(Random.Range(0.0f, 1.0f) * 100.0f) / 100.0f;
            material.SetFloat(metallicId, metallic);

            //smoothness = Mathf.Round(Random.Range(0.0f, 1.0f) * 100.0f) / 100.0f;
            material.SetFloat(smoothnessId, smoothness);          
            
        }

        void Awake()
        {
            OnValidate();
        }
    }
    
    
}