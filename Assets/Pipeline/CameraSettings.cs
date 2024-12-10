using System;
using UnityEngine.Rendering;

[Serializable]
public class CameraSettings
{
    [Serializable]
    public struct FinalBlendMode
    {
        public BlendMode src, dst;
    }

    public bool copyColor = true;
    public bool copyDepth = true;

    public bool overridePostFX = false;
    public PostFXSettings postFXSettings = default;

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        src = BlendMode.One,
        dst = BlendMode.Zero
    };
}
