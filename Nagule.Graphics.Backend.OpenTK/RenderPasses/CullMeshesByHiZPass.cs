namespace Nagule.Graphics.Backend.OpenTK;

public class CullMeshesByHiZPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var hiZBuffer = ref pipeline.RequireAny<HiearchicalZBuffer>();
        GLHelper.CullMeshesByHiZ(host, in hiZBuffer, meshGroup.GetMeshIds(MeshFilter));
    }
}