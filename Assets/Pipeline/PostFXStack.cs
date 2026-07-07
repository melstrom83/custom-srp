using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
    ApplyColorGrading,
    ApplyColorGradingWithLuma,
    FinalRescale,
    FXAA,
    FXAAWithLuma
}

public partial class PostFXStack
{
    CommandBuffer buffer;

    const int maxBloomPyramidLevels = 16;

    static Rect fullViewRect = new Rect(0.0f, 1.0f, 1.0f, 1.0f); 


    int fxSourceId = Shader.PropertyToID("_PostFXSource");
    int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
    int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
    int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    int bloomResultId = Shader.PropertyToID("_BloomResult");

    int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
    int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
    int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
    int colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");
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

    int copyBicubicId = Shader.PropertyToID("_CopyBicubic");
    int colorGradingResultId = Shader.PropertyToID("_ColorGradingResult");
    int finalResultId = Shader.PropertyToID("_FinalResult");
    int finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
    int finalDstBlendId = Shader.PropertyToID("_FinalDstBlend");

    int fxaaConfigId = Shader.PropertyToID("_FXAAConfig");

    Camera camera;
    PostFXSettings settings;
    int bloomPyramidId;
    bool useHDR;
    bool keepAlpha;
    int colorLUTResolution;
    Vector2Int bufferSize;
    CameraBufferSettings.BicubicRescalingMode bicubicRescaling;
    CameraSettings.FinalBlendMode finalBlendMode;
    CameraBufferSettings.FXAA fxaa;
    public bool IsActive => settings != null;

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

    public void Setup(Camera camera, Vector2Int bufferSize, 
    PostFXSettings settings, bool useHDR, int colorLUTResolution, 
    CameraSettings.FinalBlendMode finalBlendMode, bool keepAlpha,
    CameraBufferSettings.BicubicRescalingMode bicubicRescaling, 
    CameraBufferSettings.FXAA fxaa)
    {
        this.finalBlendMode = finalBlendMode;
        this.keepAlpha = keepAlpha;
        this.bicubicRescaling = bicubicRescaling;
        this.fxaa = fxaa;
        this.bufferSize = bufferSize;
        this.camera = camera;
        this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;

        ApplySceneViewState();
    }

    public void Render(RenderGraphContext context, TextureHandle sourceId)
    {
        buffer = context.cmd;

        if(DoBloom(sourceId))
        {
            DoFinal(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoFinal(sourceId);
        }
        context.renderContext.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool DoBloom(RenderTargetIdentifier sourceId)
    {
        var bloom = settings.Bloom;
        var width = bloom.ignoreRenderScale ? camera.pixelWidth / 2 : bufferSize.x / 2;
        var height = bloom.ignoreRenderScale ? camera.pixelHeight / 2 : bufferSize.y / 2;
        var format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

        if (bloom.maxIterations == 0 || bloom.intensity <= 0.0f
            || width < bloom.downscaleLimit * 2 || height < bloom.downscaleLimit * 2)
        {
            return false;
        }

        buffer.BeginSample("Bloom");
        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
        threshold.y = threshold.x * bloom.thresholdKnee;
        threshold.z = 2.0f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        buffer.GetTemporaryRT(bloomPrefilterId, bufferSize.x, bufferSize.y, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, 
            bloom.fadeFireflies ? Pass.BloomPrefilterFireflies : Pass.BloomPrefilter);
        
        width /= 2;
        height /= 2;
        var fromId = bloomPrefilterId;
        var toId = bloomPyramidId + 1;

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
        buffer.GetTemporaryRT(bloomResultId, 
            camera.pixelWidth, camera.pixelHeight, 0,
            FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, combinePass);
        buffer.ReleaseTemporaryRT(fromId);
        

        buffer.EndSample("Bloom");

        return true;
    }

    void DoFinal(RenderTargetIdentifier sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHightlights();

        var lutHeight = colorLUTResolution;
        var lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0,
            FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1.0f)));

        var mode = settings.ToneMapping.mode;
        var pass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(colorGradingLUTInLogCId,
            useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
        Draw(sourceId, colorGradingLUTId, pass);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            1f / lutWidth, 1f / lutHeight, lutHeight - 1f));

        buffer.SetGlobalFloat(finalSrcBlendId, 1.0f);
        buffer.SetGlobalFloat(finalDstBlendId, 0.0f);
        if(fxaa.enabled)
        {
            buffer.SetGlobalVector(fxaaConfigId, 
                new Vector4(fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending));
            buffer.GetTemporaryRT(colorGradingResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default);
            Draw(sourceId, colorGradingResultId, Pass.ApplyColorGrading);
        }

        if(bufferSize.x == camera.pixelWidth)
        {
            if(fxaa.enabled)
            {
                DrawFinal(colorGradingResultId, Pass.FXAA);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
            }
        }
        else
        {
            buffer.GetTemporaryRT(finalResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default);

            if(fxaa.enabled)
            {
                Draw(colorGradingResultId, finalResultId, Pass.FXAA);
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }
        
            var bicubicSampling = 
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly && bufferSize.x < camera.pixelWidth;
            buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1.0f : 0.0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
        }
       
        buffer.ReleaseTemporaryRT(colorGradingLUTId);
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, 
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,
            MeshTopology.Triangles, 3);
    }

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
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
            (int)pass, MeshTopology.Triangles, 3);
    }
}
