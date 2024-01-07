namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[AfterSystem<HierarchicalZBufferGeneratePass>]
public class HierarchicalZCullingPass : RenderPassSystemBase
{
    public GroupPredicate GroupPredicate { get; init; } = GroupPredicates.Any;
    public MaterialPredicate MaterialPredicate { get; init; } = MaterialPredicates.Any;

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
                if (group.Count == 0 || !GroupPredicate(group)) { continue; }

                var matEntity = group.Key.MaterialEntity;
                if (!matEntity.Valid) { continue; }

                ref var matState = ref matEntity.GetState<MaterialState>();
                if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var buffer)) {
                    continue;
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, buffer.Handle.Handle);
                GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, group.CulledInstanceBuffer.Handle);
                GL.BindVertexArray(group.CullingVertexArrayHandle.Handle);

                GL.BeginTransformFeedback(GLPrimitiveType.Points);
                GL.BeginQuery(QueryTarget.PrimitivesGenerated, group.CulledQueryHandle.Handle);
                GL.DrawArrays(GLPrimitiveType.Points, 0, group.Count);
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