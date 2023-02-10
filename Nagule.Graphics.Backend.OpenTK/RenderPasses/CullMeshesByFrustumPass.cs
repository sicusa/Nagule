namespace Nagule.Graphics.Backend.OpenTK;

public class CullMeshesByFrustumPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        GLHelper.CullMeshesByFrustum(host, meshGroup.GetMeshIds(MeshFilter));
    }
}