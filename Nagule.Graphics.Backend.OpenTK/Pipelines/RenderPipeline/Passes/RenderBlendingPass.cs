namespace Nagule.Graphics.Backend.OpenTK;

public class RenderBlendingPass : RenderPassBase
{
    public required MeshFilter MeshFilter { get; init; }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, Guid cameraId, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        pipeline.AcquireColorTexture();

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