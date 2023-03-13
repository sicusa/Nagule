namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class CullMeshesByFrustumPassImpl : RenderPassImplBase
{
    public required MeshFilter MeshFilter { get; init; }

    private uint _programId;

    private static GLSLProgram s_program =
        new GLSLProgram() {
            Name = "nagule.pipeline.cull_occluders"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.cull_occluders.vert.glsl")),
            new(ShaderType.Geometry,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.cull.geo.glsl")))
        .WithFeedback("CulledObjectToWorld");

    public override void LoadResources(IContext context)
    {
        _programId = context.GetResourceLibrary().Reference(Id, s_program);
    }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, uint cameraId, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        ref var occluderCullProgram = ref host.RequireOrNullRef<GLSLProgramData>(_programId);
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