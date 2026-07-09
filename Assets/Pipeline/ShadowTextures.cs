using UnityEngine.Rendering.RenderGraphModule;

namespace Graphics
{
    public readonly ref struct ShadowTextures
    {
        public readonly TextureHandle directionalAtlas, additionalAtlas;

        public ShadowTextures(
            TextureHandle directionalAtlas,
            TextureHandle additionalAtlas)
        {
            this.directionalAtlas = directionalAtlas;
            this.additionalAtlas = additionalAtlas;
        }

        
        
    }
}