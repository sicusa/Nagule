namespace Nagule.Graphics.Backend.OpenTK;

public class RenderOpaquePass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.Draw(host, id, in meshData);
        }
    }
}