//using System;
using UnityEngine;

namespace Graphics
{
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        private static int baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color baseColor = Color.white;
        
        static MaterialPropertyBlock block;

        private void OnValidate()
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
            block.SetColor(baseColorId, baseColor);
            GetComponent<Renderer>().SetPropertyBlock(block);
            
        }

        void Awake()
        {
            OnValidate();
        }
    }
    
    
}