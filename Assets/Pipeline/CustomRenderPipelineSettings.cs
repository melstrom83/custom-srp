using UnityEngine;

namespace Graphics
{
[System.Serializable]
    public class CustomRenderPipelineSettings
    {
        public CameraBufferSettings cameraBuffer = new()
        {
            allowHDR = true,
            renderScale = 1.0f,
            fxaa = new()
            {
                fixedThreshold = 0.0833f,
                relativeThreshold = 0.166f,
                subpixelBlending = 0.75f
            }
        };
        public ShadowSettings shadowSettings;
        public PostFXSettings postFXSettings;
        public enum ColorLUTResolution
        {
            _16 = 16, _32 = 32, _64 = 64,
        }
        public ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
        public Shader cameraRendererShader;
    }
}