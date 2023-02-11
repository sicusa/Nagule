namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;

public class CullMeshesByFrustumPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        ref var occluderCullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.OccluderCullingShaderProgramId);
        if (Unsafe.IsNullRef(ref occluderCullProgram)) {
            return;
        }

        GL.UseProgram(occluderCullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.Cull(host, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);
    }
}