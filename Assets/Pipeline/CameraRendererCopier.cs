using UnityEngine;
using UnityEngine.Rendering;

namespace Graphics
{
    public readonly struct CameraRendererCopier
    {
        static readonly int 
            sourceTextureId = Shader.PropertyToID("_SourceTexture"),
            srcBelndId = Shader.PropertyToID("_CameraSrcBlend"),
            dstBlendId = Shader.PropertyToID("_CameraDstBlend");

        static readonly Rect fullViewRect = new(0.0f, 0.0f, 1.0f, 1.0f);
        static readonly bool copyTextureSupported = 
            SystemInfo.copyTextureSupport > CopyTextureSupport.None;


        public static bool RequiresRenderTargetResetAfterCopy => !copyTextureSupported;
        public readonly Camera Camera => camera;
        readonly Material material;
        readonly Camera camera;
        readonly CameraSettings.FinalBlendMode finalBlendMode;

        public CameraRendererCopier(Material material, Camera camera,
            CameraSettings.FinalBlendMode finalBlendMode)
        {
            this.material = material;
            this.camera = camera;
            this.finalBlendMode = finalBlendMode;
        }

        public readonly void Copy(
            CommandBuffer buffer,
            RenderTargetIdentifier from,
            RenderTargetIdentifier to,
            bool isDepth)
        {
            if(copyTextureSupported)
            {
                buffer.CopyTexture(from, to);
            }
            else
            {
                CopyByDrawing(buffer, from, to, isDepth);
            }
        }

        public readonly void CopyByDrawing(
            CommandBuffer buffer,
            RenderTargetIdentifier from,
            RenderTargetIdentifier to,
            bool isDepth)
        {
            buffer.SetGlobalTexture(sourceTextureId, from);
            buffer.SetRenderTarget(
                to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawProcedural(
                Matrix4x4.identity, material, isDepth ? 1 : 0,
                MeshTopology.Triangles, 3);
        }

        public readonly void CopyToCameraTarget(
            CommandBuffer buffer, RenderTargetIdentifier from)
        {
            buffer.SetGlobalFloat(srcBelndId, (float)finalBlendMode.src);
            buffer.SetGlobalFloat(dstBlendId, (float)finalBlendMode.dst);
            buffer.SetGlobalTexture(sourceTextureId,  from);
            buffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                finalBlendMode.dst == BlendMode.Zero && camera.rect == fullViewRect
                    ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store);
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
            buffer.SetGlobalFloat(srcBelndId, 1.0f);
            buffer.SetGlobalFloat(dstBlendId, 0.0f);
        }
    }
}
