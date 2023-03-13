namespace Nagule.Graphics.Backend.OpenTK;

public class RenderBlendingPassImpl : RenderPassImplBase
{
    public required MeshFilter MeshFilter { get; init; }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, uint cameraId, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        pipeline.EnsureColorTexture();

        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.DrawBlending(host, id, in meshData);
        }

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
    }
}