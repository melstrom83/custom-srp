using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine;

namespace Graphics
{
    public class SetupPass
    {
        static readonly ProfilingSampler sampler = new("Setup");
        static int attachmentSizeId = Shader.PropertyToID("_CameraBufferSize");

        bool useIntermediateAttachments;


        TextureHandle colorAttachment, depthAttachment;
        Vector2Int attachmentSize;
        Camera camera;
        CameraClearFlags clearFlags;

        void Render(RenderGraphContext context)
        {
            context.renderContext.SetupCameraProperties(camera);
            CommandBuffer cmd = context.cmd;
            
            CameraClearFlags flags = camera.clearFlags;

            if (useIntermediateAttachments)
            {
                cmd.SetRenderTarget(
                    colorAttachment, 
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    depthAttachment,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
            }
            cmd.ClearRenderTarget(
                flags <= CameraClearFlags.Depth,
                flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear);
            //buffer.SetGlobalTexture(colorTextureId, missingTexture); 
            //buffer.SetGlobalTexture(depthTextureId, missingTexture);
            cmd.SetGlobalVector(attachmentSizeId, new Vector4(
                1.0f / attachmentSize.x, 1.0f / attachmentSize.y, attachmentSize.x, attachmentSize.y));
            
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
        public static CameraRendererTextures Record(
            RenderGraph renderGraph, 
            bool useIntermediateAttachments,
            bool copyColor,
            bool copyDepth,
            bool useHDR, 
            Vector2Int attachmentSize,
            Camera camera)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out SetupPass pass, sampler);
            pass.useIntermediateAttachments = useIntermediateAttachments;
            pass.attachmentSize = attachmentSize;
            pass.camera = camera;
            pass.clearFlags = camera.clearFlags;

            TextureHandle colorAttachment, depthAttachment;
            TextureHandle colorCopy = default, depthCopy = default;
            
            if(useIntermediateAttachments)
            {
                if(pass.clearFlags > CameraClearFlags.Color)
                {
                    pass.clearFlags = CameraClearFlags.Color;
                }

                var desc = new TextureDesc(attachmentSize.x, attachmentSize.y)
                {
                    colorFormat = SystemInfo.GetGraphicsFormat(useHDR 
                    ? DefaultFormat.HDR 
                    : DefaultFormat.LDR),
                    name = "Color Attachment"
                };
                colorAttachment = pass.colorAttachment = 
                    builder.WriteTexture(renderGraph.CreateTexture(desc));
                if(copyColor)
                {
                    desc.name = "Color Copy";
                    colorCopy = renderGraph.CreateTexture(desc);
                }

                desc.depthBufferBits = DepthBits.Depth32;
                desc.name = "Depth Attachment";
                depthAttachment = pass.depthAttachment = 
                    builder.WriteTexture(renderGraph.CreateTexture(desc));
                if(copyDepth)
                {
                    desc.name = "Depth Copy";
                    depthCopy = renderGraph.CreateTexture(desc);
                }
            }
            else
            {
                colorAttachment = depthAttachment = 
                    pass.colorAttachment = pass.depthAttachment = 
                    builder.WriteTexture(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget));
            }
            builder.AllowPassCulling(false);
            builder.SetRenderFunc<SetupPass>((pass, context) => pass.Render(context));

            return new CameraRendererTextures(
                colorAttachment, depthAttachment, colorCopy, depthCopy);
        }
    }
}