using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

enum Pass
{
    Copy,
    BloomHorizontal,
    BloomVertical,
    BloomCombine,
    BloomPrefilter,
    BloomPrefilterFireflies,
    BloomScatter,
    ColorGradingNone,
    ColorGradingACES,
    ColorGradingNeutral,
    ColorGradingReinhard,
    Final
}

public partial class PostFXStack
{
    const string bufferName = "PostFX";
    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName,
    };

    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;

    bool useHDR;

    CameraSettings.FinalBlendMode finalBlendMode;

    static Rect fullViewRect = new Rect(0.0f, 1.0f, 1.0f, 1.0f); 

    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");
    int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    int colorFilterId = Shader.PropertyToID("_ColorFilter");
    int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");
    int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
    int splitToningHightlightsId = Shader.PropertyToID("_SplitToningHighlights");
    int channelMixerRId = Shader.PropertyToID("_ChannelMixerR");
    int channelMixerGId = Shader.PropertyToID("_ChannelMixerG");
    int channelMixerBId = Shader.PropertyToID("_ChannelMixerB");
    int smhShadowsId = Shader.PropertyToID("_SMHShadows");
    int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
    int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
    int smhRangeId = Shader.PropertyToID("_SMHRange");
    int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    int colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");

    int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;
    int colorLUTResolution;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for(var i = 0; i < maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    partial void ApplySceneViewState();

#if UNITY_EDITOR
    partial void ApplySceneViewState()
    {
        if(camera.cameraType == CameraType.SceneView &&
            !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
        {
            settings = null;
        }
    }
#endif

    void ConfigureColorAdjustments()
    {
        var colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
            Mathf.Pow(2.0f, colorAdjustments.postExposure),
            colorAdjustments.contrast * 0.01f + 1.0f,
            colorAdjustments.hueShift * (1.0f / 360.0f),
            colorAdjustments.saturation * 0.01f + 1.0f));
        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        var whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
            whiteBalance.temperature, whiteBalance.tint));
    }

    void ConfigureSplitToning()
    {
        var splitToning = settings.SplitToning;
        var splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalVector(splitToningShadowsId, splitColor);
        buffer.SetGlobalVector(splitToningHightlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer()
    {
        var channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRId, channelMixer.r);
        buffer.SetGlobalVector(channelMixerGId, channelMixer.g);
        buffer.SetGlobalVector(channelMixerBId, channelMixer.b);
    }

    void ConfigureShadowsMidtonesHightlights()
    {
        var smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalVector(smhRangeId, new Vector4(
            smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
    }

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,
        bool useHDR, int colorLUTResolution, CameraSettings.FinalBlendMode finalBlendMode)
    {
        this.finalBlendMode = finalBlendMode;
        this.context = context;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;

        ApplySceneViewState();
    }

    public bool IsActive => settings != null;

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,
            MeshTopology.Triangles, 3);
    }

    void DrawFinal(RenderTargetIdentifier from)
    {
        buffer.SetGlobalFloat(finalSrcBlendId, (float)finalBlendMode.src);
        buffer.SetGlobalFloat(finalDstBlendId, (float)finalBlendMode.dst);
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
            finalBlendMode.dst == BlendMode.Zero && camera.rect == fullViewRect ?
            RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
            RenderBufferStoreAction.Store);
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material,
            (int)Pass.Final, MeshTopology.Triangles, 3);
    }

    public void Render(int sourceId)
    {
        if(DoBloom(sourceId))
        {
            DoColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoColorGradingAndToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool DoBloom(int sourceId)
    {
        var bloom = settings.Bloom;
        var width = camera.pixelWidth / 2;
        var height = camera.pixelHeight / 2;
        var format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2.0f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        var fromId = bloomPrefilterId;
        var toId = bloomPyramidId + 1;

        if (bloom.maxIterations == 0
            || bloom.intensity <= 0.0f
            || width < bloom.downscaleLimit * 2
            || height < bloom.downscaleLimit * 2)
        {
            return false;
        }

        buffer.BeginSample("Bloom");

        int i;
        for(i = 0; i < bloom.maxIterations; ++i)
        {
            if(width < bloom.downscaleLimit || height < bloom.downscaleLimit)
            {
                break;
            }

            var midId = toId - 1;
            buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        

        buffer.SetGlobalFloat(bloomBicubicUpsamplingId,
            bloom.bicubicUpsampling ? 1.0f : 0.0f);

        float finalIntensity;
        Pass combinePass;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = Pass.BloomCombine;
            buffer.SetGlobalFloat(bloomIntensityId, 1.0f);
            finalIntensity = bloom.intensity;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
            finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
        }

        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId -= 5;

            for (i -= 1; i > 0; --i)
            {
                buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId);
                buffer.ReleaseTemporaryRT(toId + 1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
        buffer.SetGlobalTexture(fxSource2Id, sourceId);
        buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0,
            FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, combinePass);
        buffer.ReleaseTemporaryRT(fromId);
        

        buffer.EndSample("Bloom");

        return true;
    }

    void DoColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHightlights();

        var lutHeight = colorLUTResolution;
        var lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight,
            0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));

        var mode = settings.ToneMapping.mode;
        var pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(colorGradingLUTInLogCId,
            useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
        DrawFinal(sourceId);
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }
}
