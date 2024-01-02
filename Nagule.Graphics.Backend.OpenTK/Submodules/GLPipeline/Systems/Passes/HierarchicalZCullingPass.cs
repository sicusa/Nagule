namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[AfterSystem<HierarchicalZBufferGeneratePass>]
public class HierarchicalZCullingPass : RenderPassSystemBase
{
    private static readonly RGLSLProgram s_cullProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.cull_hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.pipeline.cull_hiz.vert.glsl")),
            new(ShaderType.Geometry,
                ShaderUtils.LoadCore("nagule.pipeline.cull.geo.glsl")))
        .WithFeedback("CulledObjectToWorld");
    
    public MeshFilter MeshFilter { get; init; } = MeshFilter.All;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var meshManager = world.GetAddon<Mesh3DManager>();
        var instanceLib = world.GetAddon<GLMesh3DInstanceLibrary>();

        var buffer = Pipeline.AcquireAddon<HierarchicalZBuffer>();
        var framebuffer = Pipeline.GetAddon<Framebuffer>();

        var cullProgramEntity = GLSLProgram.CreateEntity(
            world, s_cullProgramAsset, AssetLife.Persistent);

        RenderFrame.Start(() => {
            ref var cullProgramState = ref cullProgramEntity.GetState<GLSLProgramState>();
            if (!cullProgramState.Loaded) { return NextFrame; }

            GL.UseProgram(cullProgramState.Handle.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, buffer.TextureHandle.Handle);

            foreach (var group in instanceLib.Groups.Values) {
                var matEntity = group.Key.MaterialEntity;
                if (!matEntity.Valid) {
                    continue;
                }
                ref var materialState = ref matEntity.GetState<MaterialState>();
                if (!materialState.Loaded
                        || !MeshFilter.Check(materialState)) {
                    continue;
                }

                int visibleCount = 0;
                GL.GetQueryObjecti(group.CulledQueryHandle.Handle, QueryObjectParameterName.QueryResult, ref visibleCount);
                if (visibleCount == 0) { continue; }

                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var state)) {
                    continue;
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, state.Handle.Handle);
                GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, group.CulledInstanceBuffer.Handle);
                GL.BindVertexArray(group.CullingVertexArrayHandle.Handle);

                GL.BeginTransformFeedback(GLPrimitiveType.Points);
                GL.BeginQuery(QueryTarget.PrimitivesGenerated, group.CulledQueryHandle.Handle);
                GL.DrawArrays(GLPrimitiveType.Points, 0, visibleCount);
                GL.EndQuery(QueryTarget.PrimitivesGenerated);
                GL.EndTransformFeedback();
            }

            GL.UseProgram(0);
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.RasterizerDiscard);
            return NextFrame;
        });
    }
}