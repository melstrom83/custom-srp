using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace Graphics
{   public class CopyAttachmentsPass
    {
        static readonly ProfilingSampler sampler = new("Copy Attachments");
        
        CameraRenderer renderer;

        void Render(RenderGraphContext context) => renderer.CopyAttachments();

        public static void Record(RenderGraph renderGraph, CameraRenderer renderer)
        {
            using var builder = renderGraph.AddRenderPass(sampler.name, out CopyAttachmentsPass pass, sampler);
            pass.renderer = renderer;

            builder.SetRenderFunc<CopyAttachmentsPass>((pass, context) => pass.Render(context));
        }
    }
}
