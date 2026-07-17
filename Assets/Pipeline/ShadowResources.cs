using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public readonly ref struct ShadowResources
    {
        public readonly TextureHandle directionalAtlas, additionalAtlas;

        public readonly BufferHandle directionalShadowCascadesBuffer;
        public readonly BufferHandle directionalShadowMatricesBuffer;
        public readonly BufferHandle additionalShadowDataBuffer;

        public ShadowResources(
            TextureHandle directionalAtlas,
            TextureHandle additionalAtlas,
            BufferHandle directionalShadowCascadesBuffer,
            BufferHandle directionalShadowMatricesBuffer,
            BufferHandle additionalShadowDataBuffer)
        {
            this.directionalAtlas = directionalAtlas;
            this.additionalAtlas = additionalAtlas;
            this.directionalShadowCascadesBuffer = directionalShadowCascadesBuffer;
            this.directionalShadowMatricesBuffer = directionalShadowMatricesBuffer;
            this.additionalShadowDataBuffer = additionalShadowDataBuffer;
        }

        
        
    }
}