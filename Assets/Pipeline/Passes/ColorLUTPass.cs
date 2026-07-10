using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

//using static PostFXSettings;
using static PostFXStack;

namespace Graphics
{
    public class ColorLUTPass
    {
        static readonly ProfilingSampler sampler = new("Color LUT");

        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);

        static readonly int 
            colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
            colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
            colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
            colorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
            colorFilterId = Shader.PropertyToID("_ColorFilter"),
            whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
            splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
            splitToningHightlightsId = Shader.PropertyToID("_SplitToningHighlights"),
            channelMixerRId = Shader.PropertyToID("_ChannelMixerR"),
            channelMixerGId = Shader.PropertyToID("_ChannelMixerG"),
            channelMixerBId = Shader.PropertyToID("_ChannelMixerB"),
            smhShadowsId = Shader.PropertyToID("_SMHShadows"),
            smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
            smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
            smhRangeId = Shader.PropertyToID("_SMHRange");

        PostFXStack stack;
        int colorLUTResolution;
        TextureHandle colorLUT;

        void ConfigureColorAdjustments(CommandBuffer buffer, PostFXSettings settings)
        {
            var colorAdjustments = settings.ColorAdjustments;
            buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
                Mathf.Pow(2.0f, colorAdjustments.postExposure),
                colorAdjustments.contrast * 0.01f + 1.0f,
                colorAdjustments.hueShift * (1.0f / 360.0f),
                colorAdjustments.saturation * 0.01f + 1.0f));
            buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
        }

        void ConfigureWhiteBalance(CommandBuffer buffer, PostFXSettings settings)
        {
            var whiteBalance = settings.WhiteBalance;
            buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
                whiteBalance.temperature, whiteBalance.tint));
        }

        void ConfigureSplitToning(CommandBuffer buffer, PostFXSettings settings)
        {
            var splitToning = settings.SplitToning;
            var splitColor = splitToning.shadows;
            splitColor.a = splitToning.balance * 0.01f;
            buffer.SetGlobalVector(splitToningShadowsId, splitColor);
            buffer.SetGlobalVector(splitToningHightlightsId, splitToning.highlights);
        }

        void ConfigureChannelMixer(CommandBuffer buffer, PostFXSettings settings)
        {
            var channelMixer = settings.ChannelMixer;
            buffer.SetGlobalVector(channelMixerRId, channelMixer.r);
            buffer.SetGlobalVector(channelMixerGId, channelMixer.g);
            buffer.SetGlobalVector(channelMixerBId, channelMixer.b);
        }

        void ConfigureShadowsMidtonesHightlights(CommandBuffer buffer, PostFXSettings settings)
        {
            var smh = settings.ShadowsMidtonesHighlights;
            buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
            buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
            buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
            buffer.SetGlobalVector(smhRangeId, new Vector4(
                smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highlightsEnd));
        }

        void Render(RenderGraphContext context)
        {
            var buffer = context.cmd;
            var settings = stack.Settings;

            ConfigureColorAdjustments(buffer, settings);
            ConfigureWhiteBalance(buffer, settings);
            ConfigureSplitToning(buffer, settings);
            ConfigureChannelMixer(buffer, settings);
            ConfigureShadowsMidtonesHightlights(buffer, settings);

            var lutHeight = colorLUTResolution;
            var lutWidth = lutHeight * lutHeight;
            buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
                lutHeight, 
                0.5f / lutWidth, 0.5f / lutHeight, 
                lutHeight / (lutHeight - 1.0f)));

            var mode = settings.ToneMapping.mode;
            var pass = Pass.ColorGradingNone + (int)mode;
            buffer.SetGlobalFloat(colorGradingLUTInLogCId,
                stack.BufferSettings.allowHDR 
                && pass != Pass.ColorGradingNone ? 1f : 0f);
            stack.Draw(buffer, colorLUT, pass);
            buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
                1.0f / lutWidth, 
                1.0f / lutHeight,
                lutHeight - 1));
            buffer.SetGlobalTexture(colorGradingLUTId, colorLUT);
        }

        public static TextureHandle Record(
            RenderGraph renderGraph,
            PostFXStack stack,
            int colorLUTResolution)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out ColorLUTPass pass, sampler);
            pass.stack = stack;
            pass.colorLUTResolution = colorLUTResolution;

            var desc = new TextureDesc(colorLUTResolution * colorLUTResolution, colorLUTResolution)
            {
                colorFormat = colorFormat,
                name = "Color LUT"
            };

            pass.colorLUT = builder.WriteTexture(renderGraph.CreateTexture(desc));
            builder.SetRenderFunc<ColorLUTPass>(static (pass, context) => pass.Render(context));

            return pass.colorLUT;
        }
    }
}