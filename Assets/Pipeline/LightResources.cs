using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public readonly ref struct LightResources
    {
        public readonly BufferHandle directionalLightDataBuffer;
        public readonly BufferHandle additionalLightDataBuffer;

        public readonly ShadowResources shadowResources;

        public LightResources(
            BufferHandle directionalLightDataBuffer,
            BufferHandle additionalLightDataBuffer,
            ShadowResources shadowResources)
        {
            this.directionalLightDataBuffer = directionalLightDataBuffer;
            this.additionalLightDataBuffer = additionalLightDataBuffer;
            this.shadowResources = shadowResources;
        }
    }
}