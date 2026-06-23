using UnityEngine;
using System;

[Serializable]
public struct CameraBufferSettings
{
    public bool allowHDR;
    public bool copyColor, copyColorReflection;
    public bool copyDepth, copyDepthReflection;
    [Range(0.25f, 2.0f)]
    public float renderScale;
    public enum BicubicRescalingMode { Off, UpOnly, UpAndDown };
    public BicubicRescalingMode bicubicRescaling;

    [Serializable]
    public struct FXAA
    {
        public bool enabled;

        [Range(0.0312f, 0.0833f)]
        public float fixedThreshold;

        [Range(0.063f, 0.333f)]
        public float relativeThreshold;

        [Range(0.0f, 1.0f)]
        public float subpixelBlending;
    };
    
    public FXAA fxaa;
}