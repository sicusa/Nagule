namespace Nagule.Graphics.Backend.OpenTK;

public class RenderDepthPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        GLHelper.RenderDepth(host, meshGroup.GetMeshIds(MeshFilter));
    }
}