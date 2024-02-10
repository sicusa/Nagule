namespace Nagule.Graphics.ShadowMapping;

using Sia;

public struct Light3DShadowMapState
{
    public ShadowMapHandle? ShadowMapHandle;
    public FramebufferHandle ShadowMapFramebufferHandle;
    public EntityRef ShadowSamplerCamera;
}