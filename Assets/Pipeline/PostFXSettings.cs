using System;
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

    [Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float temperature, tint;
    }

    [SerializeField]
    WhiteBalanceSettings whiteBalance = default;
    public WhiteBalanceSettings WhiteBalance => whiteBalance;


    [Serializable]
    public struct SplitToningSettings
    {
        [ColorUsage(false)]
        public Color shadows, highlights;

        [Range(-100f, 100f)]
        public float balance;
    }

    [SerializeField]
    SplitToningSettings splitToning = new SplitToningSettings
    {
        shadows = Color.gray,
        highlights = Color.gray,
    };
    public SplitToningSettings SplitToning => splitToning;

    [Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 r, g, b;
    }
    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        r = Vector3.right,
        g = Vector3.up,
        b = Vector3.forward,
    };
    public ChannelMixerSettings ChannelMixer => channelMixer;

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
