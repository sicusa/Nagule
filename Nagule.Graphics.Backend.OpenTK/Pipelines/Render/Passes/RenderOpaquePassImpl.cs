namespace Nagule.Graphics.Backend.OpenTK;

public class RenderOpaquePassImpl : RenderPassImplBase
{
    public required MeshFilter MeshFilter { get; init; }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, uint cameraId, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        pipeline.EnsureColorTexture();

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.Draw(host, id, in meshData);
        }
    }
}