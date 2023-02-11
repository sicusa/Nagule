namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

public class RenderDepthPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
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