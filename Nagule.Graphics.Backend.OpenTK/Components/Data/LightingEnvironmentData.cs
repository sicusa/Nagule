namespace Nagule.Graphics.Backend.OpenTK;

public struct LightingEnvironmentData : IHashComponent
{
    public TextureArrayPool ShadowMapTexturePool;
    public CubemapArrayPool ShadowMapCubemapPool;

    public uint ShadowMapRenderSettingsId;
}