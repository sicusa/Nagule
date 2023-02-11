namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

public class RenderBlendingPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            if (meshData.RenderMode == RenderMode.Additive) {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            }
            else {
                // meshData.RenderMode == RenderMode.Multiplicative
                GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
            }
            GLHelper.Draw(host, id, in meshData);
        }

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
    }
}