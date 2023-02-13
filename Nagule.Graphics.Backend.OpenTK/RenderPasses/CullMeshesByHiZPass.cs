namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class CullMeshesByHiZPass : RenderPassBase
{
    public required MeshFilter MeshFilter { get; init; }
    
    private Guid _programId;

    private GLSLProgram s_program = 
        new GLSLProgram {
            Name = "nagule.pipeline.cull"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.cull.vert.glsl")),
            new(ShaderType.Geometry,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.cull.geo.glsl")))
        .WithFeedback("CulledObjectToWorld");

    public override void LoadResources(IContext context)
    {
        _programId = ResourceLibrary.Reference(context, Id, s_program);
    }

    public override void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        ref var cullProgram = ref host.RequireOrNullRef<GLSLProgramData>(_programId);
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