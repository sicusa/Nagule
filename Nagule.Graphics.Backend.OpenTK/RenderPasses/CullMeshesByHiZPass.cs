namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;

public class CullMeshesByHiZPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        ref var cullProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.CullingShaderProgramId);
        if (Unsafe.IsNullRef(ref cullProgram)) { return; }

        ref var hiZBuffer = ref pipeline.RequireAny<HiearchicalZBuffer>();

        GL.UseProgram(cullProgram.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, hiZBuffer.TextureHandle);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.Cull(host, id, in meshData);
        }

        GL.Disable(EnableCap.RasterizerDiscard);
    }
}