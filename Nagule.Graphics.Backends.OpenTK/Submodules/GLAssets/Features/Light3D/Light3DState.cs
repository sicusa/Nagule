namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public struct Light3DState : IAssetState
{
    public readonly bool Loaded => Type != LightType.None;

    public bool IsEnabled;
    public LightType Type;
    public int Index;

    public ShadowMapHandle? ShadowMapHandle;
    public FramebufferHandle ShadowMapFramebufferHandle;
    public EntityRef ShadowSamplerCamera;
}