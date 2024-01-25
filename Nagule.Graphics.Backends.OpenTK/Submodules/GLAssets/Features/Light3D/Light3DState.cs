using System.Numerics;

namespace Nagule.Graphics.Backends.OpenTK;

public struct Light3DState : IAssetState
{
    public readonly bool Loaded => Type != LightType.None;

    public LightType Type;
    public int Index;

    public ShadowMapHandle? ShadowMapHandle;
    public FramebufferHandle ShadowMapFramebufferHandle;
}