namespace Nagule.Graphics;

using System.Collections.Immutable;

public record RenderPipeline
{
    public static RenderPipeline Default { get; } = new() {
        Passes = ImmutableArray.Create<RenderPass>(
            new RenderPass.CullMeshesByFrustum(),
            new RenderPass.RenderDepth(),
            new RenderPass.GenerateHiZBuffer(),
            new RenderPass.CullMeshesByHiZ(),
            new RenderPass.ActivateMaterialBuiltInBuffers(),
            new RenderPass.RenderOpaque(),
            new RenderPass.RenderSkyboxCubemap(),
            new RenderPass.RenderTransparent(),
            new RenderPass.RenderBlending())
    };
    
    public static RenderPipeline OpaqueShadowmap { get; } = new() {
        Passes = ImmutableArray.Create<RenderPass>(
            new RenderPass.CullMeshesByFrustum(),
            new RenderPass.RenderDepth(),
            new RenderPass.GenerateHiZBuffer(),
            new RenderPass.CullMeshesByHiZ(MeshFilter.NonoccluderOpaque),
            new RenderPass.RenderDepth(MeshFilter.NonoccluderOpaque))
    };
    
    public ImmutableArray<RenderPass> Passes { get; init; }
        = ImmutableArray<RenderPass>.Empty;
}