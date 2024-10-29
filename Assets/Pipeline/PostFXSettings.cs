using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [Serializable]
    public struct BloomSettings
    {
        [Range(0.0f, 16.0f)]
        public int maxIterations;

        [Min(1.0f)]
        public int downscaleLimit;

        public bool bicubicUpsampling;

        [Min(0.0f)]
        public float threshold;

        [Range(0.0f, 1.0f)]
        public float thresholdKnee;

        [Min(0.0f)]
        public float intensity;

        public bool fadeFireflies;

        public enum Mode { Additive, Scattering}
        public Mode mode;

        [Range(0.05f, 0.95f)]
        public float scatter;
    }

    [SerializeField]
    BloomSettings bloom = default;
    public BloomSettings Bloom => bloom;

    [Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode { None, ACES, Neutral, Reinhard }
        public Mode mode;
    }

    [SerializeField]
    ToneMappingSettings toneMapping = default;
    public ToneMappingSettings ToneMapping => toneMapping;

    [Serializable]
    public struct ColorAdjustmentSettings 
    {
        public float postExposure;
        [Range(-100f, 100f)]
        public float contrast;
        [ColorUsage(false, true)]
        public Color colorFilter;
        [Range(-180f, 180f)]
        public float hueShift;
        [Range(-100f, 100f)]
        public float saturation;
    }

    [SerializeField]
    ColorAdjustmentSettings colorAdjustments = new ColorAdjustmentSettings
    {
        colorFilter = Color.white
    };
    public ColorAdjustmentSettings ColorAdjustments => colorAdjustments;

    [SerializeField]
    Shader shader = default;

    [NonSerialized]
    Material material;
    public Material Material
    {
        get
        {
            if(material == null && shader != null)
            {
                material = new Material (shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }
}
