namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct RenderTarget : IResourceObject<RenderTargetResource>
{
    public RenderTargetResource Resource { get; set; } = RenderTargetResource.AutoResized;

    public RenderTarget() {}
}