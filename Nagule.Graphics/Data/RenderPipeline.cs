namespace Nagule.Graphics;

using System.Collections.Immutable;

public record RenderPipeline
{
    public static RenderPipeline Default { get; } = new() {
        Passes = ImmutableList.Create<RenderPass>(
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
        Passes = ImmutableList.Create<RenderPass>(
            new RenderPass.CullMeshesByFrustum(),
            new RenderPass.RenderDepth(),
            new RenderPass.GenerateHiZBuffer(),
            new RenderPass.CullMeshesByHiZ(MeshFilter.NonoccluderOpaque),
            new RenderPass.RenderDepth(MeshFilter.NonoccluderOpaque))
    };
    
    public ImmutableList<RenderPass> Passes { get; init; }
        = ImmutableList<RenderPass>.Empty;

    public RenderPipeline WithPass(RenderPass pass)
        => this with { Passes = Passes.Add(pass) };
    public RenderPipeline WithPasses(params RenderPass[] passes)
        => this with { Passes = Passes.AddRange(passes) };
    public RenderPipeline WithPasses(IEnumerable<RenderPass> passes)
        => this with { Passes = Passes.AddRange(passes) };
}