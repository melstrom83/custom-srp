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

    public FinalBlendMode finalBlendMode = new FinalBlendMode
    {
        src = BlendMode.One,
        dst = BlendMode.Zero
    };
}
