using System;
using UnityEngine;

namespace Graphics
{
    [Serializable]
    public class ShadowSettings
    {
        [Min(0.001f)] public float MaxDistance = 100.0f;
        [Range(0.001f, 1.0f)] public float FadeDistance = 0.1f;

        public enum TextureSize
        {
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192
        }

        public enum FilterMode
        {
            Default,
            PCF3x3,
            PCF5x5,
            PCF7x7,
        }

        [Serializable]
        public struct Directional
        {
            public TextureSize atlasSize;
            public FilterMode filter;
            [Range(1, 4)] public int cascadeCount;
            [Range(0.0f, 1.0f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
            [Range(0.001f, 1.0f)] public float cascadeFade;
            
            public Vector3 CascadeRatios => 
                new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);
        }

        [Serializable]
        public struct Additional
        {
            public TextureSize atlasSize;
            public FilterMode filter;
        }

        public Directional directional = new Directional()
        {
            atlasSize = TextureSize._1024,
            filter = FilterMode.Default,
            cascadeCount = 4,
            cascadeRatio1 = 0.125f,
            cascadeRatio2 = 0.25f,
            cascadeRatio3 = 0.5f,
            cascadeFade = 0.1f
            
        };

        public Additional additional = new Additional()
        {
            atlasSize = TextureSize._1024,
            filter = FilterMode.Default
        };
    }
}