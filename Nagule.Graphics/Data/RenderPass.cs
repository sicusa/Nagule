namespace Nagule.Graphics;

public abstract record RenderPass
{
    public record ActivateMaterialBuiltInBuffers : RenderPass;
    public record GenerateHiZBuffer : RenderPass;

    public record CullMeshesByFrustum(
        MeshFilter MeshFilter = MeshFilter.Occluder) : RenderPass;
    public record CullMeshesByHiZ(
        MeshFilter MeshFilter = MeshFilter.Nonoccluder) : RenderPass;

    public record RenderDepth(MeshFilter MeshFilter = MeshFilter.Occluder) : RenderPass;
    public record RenderOpaque(MeshFilter MeshFilter = MeshFilter.Opaque) : RenderPass;
    public record RenderTransparent(MeshFilter MeshFilter = MeshFilter.Transparent) : RenderPass;
    public record RenderBlending(MeshFilter MeshFilter = MeshFilter.Blending) : RenderPass;
    public record RenderSkyboxCubemap() : RenderPass;
}