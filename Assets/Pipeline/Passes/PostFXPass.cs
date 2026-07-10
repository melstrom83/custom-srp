using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine;

using static PostFXSettings;
using static PostFXStack;

namespace Graphics
{
    public class PostFXPass
    {
        static readonly ProfilingSampler 
            groupSampler = new("Post FX"),
            finalSampler = new("Final Post FX");

        static readonly int 
            copyBicubicId = Shader.PropertyToID("_CopyBicubic"),
            fxaaConfigId = Shader.PropertyToID("_FXAAConfig");

        static readonly GraphicsFormat colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        
        enum ScaleMode { None, Linear, Bicubic }
        ScaleMode scaleMode;

        PostFXStack stack;
        bool keepAlpha;
        TextureHandle colorSource, colorGradingResult, scaledResult;

        void Render(RenderGraphContext context)
        {
            CommandBuffer buffer = context.cmd;
            buffer.SetGlobalFloat(finalSrcBlendId, 1.0f);
            buffer.SetGlobalFloat(finalDstBlendId, 0.0f);

            RenderTargetIdentifier finalSource;
            Pass finalPass;
            if(stack.BufferSettings.fxaa.enabled)
            {
                finalSource = colorGradingResult;
                finalPass = keepAlpha ? Pass.FXAA : Pass.FXAAWithLuma;
                stack.Draw(buffer,  colorSource, finalSource, keepAlpha
                    ? Pass.ApplyColorGrading
                    : Pass.ApplyColorGradingWithLuma);
            }
            else
            {
                finalSource = colorSource;
                finalPass = Pass.ApplyColorGrading;
            }

            if(scaleMode == ScaleMode.None)
            {
                stack.DrawFinal(buffer, finalSource, finalPass);
            }
            else
            {
                stack.Draw(buffer, finalSource, scaledResult, finalPass);
                buffer.SetGlobalFloat(copyBicubicId, scaleMode == ScaleMode.Bicubic ? 1.0f : 0.0f);
                stack.DrawFinal(buffer, scaledResult, Pass.FinalRescale);
            }
;
            context.renderContext.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public static void Record(
            RenderGraph renderGraph,
            PostFXStack stack,
            int colorLUTResolution,
            bool keepAlpha,
            in CameraRendererTextures textures)
        {
            using var _ = new RenderGraphProfilingScope(renderGraph, groupSampler);

            TextureHandle colorSource = BloomPass.Record(
                renderGraph, stack, textures);
            TextureHandle colorLUT = ColorLUTPass.Record(
                renderGraph, stack, colorLUTResolution);
            
            using var builder = renderGraph.AddRenderPass(finalSampler.name, out PostFXPass pass, finalSampler);
            pass.stack = stack;
            pass.keepAlpha = keepAlpha;
            pass.colorSource = builder.ReadTexture(colorSource);
            builder.ReadTexture(colorLUT);

            if(stack.BufferSize.x == stack.Camera.pixelWidth)
            {
                pass.scaleMode = ScaleMode.None;
            }
            else
            {
                pass.scaleMode = 
                    stack.BufferSettings.bicubicRescaling == 
                    CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
                    stack.BufferSettings.bicubicRescaling == 
                    CameraBufferSettings.BicubicRescalingMode.UpOnly &&
                    stack.BufferSize.x < stack.Camera.pixelWidth
                    ? ScaleMode.Bicubic : ScaleMode.Linear;
            }

            if(stack.BufferSettings.fxaa.enabled)
            {
                var desc = new TextureDesc(stack.BufferSize.x, stack.BufferSize.y)
                {
                    colorFormat = colorFormat,
                    name = "Color Grading Result"
                };
                pass.colorGradingResult = builder.CreateTransientTexture(desc);
            }

            if(pass.scaleMode != ScaleMode.None)
            {
                var desc = new TextureDesc(stack.BufferSize.x, stack.BufferSize.y)
                {
                    colorFormat = colorFormat,
                    name = "Scaled Result"
                };
                pass.scaledResult = builder.CreateTransientTexture(desc);
            }
            
            builder.SetRenderFunc<PostFXPass>(static (pass, context) => pass.Render(context));
        }
    }
}