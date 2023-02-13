namespace Nagule.Graphics.Backend.OpenTK;

public class RenderDepthPass : RenderPassBase
{
    public required MeshFilter MeshFilter { get; init; }

    public override void Execute(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        GL.ColorMask(false, false, false, false);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.DrawDepth(host, id, in meshData);
        }

        GL.ColorMask(true, true, true, true);
    }
}