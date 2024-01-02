namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class FrustumCullingPass : RenderPassSystemBase
{
    private static readonly RGLSLProgram s_cullProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.cull_frustum"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.pipeline.cull_frustum.vert.glsl")),
            new(ShaderType.Geometry,
                ShaderUtils.LoadCore("nagule.pipeline.cull.geo.glsl")))
        .WithFeedback("CulledObjectToWorld");
    
    public MeshFilter MeshFilter { get; init; } = MeshFilter.All;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var meshManager = world.GetAddon<Mesh3DManager>();
        var instanceLib = world.GetAddon<GLMesh3DInstanceLibrary>();

        var cullProgramEntity = GLSLProgram.CreateEntity(
            world, s_cullProgramAsset, AssetLife.Persistent);

        RenderFrame.Start(() => {
            ref var cullProgramState = ref cullProgramEntity.GetState<GLSLProgramState>();
            if (!cullProgramState.Loaded) { return NextFrame; }

            GL.UseProgram(cullProgramState.Handle.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            foreach (var group in instanceLib.Groups.Values) {
                var materialEntity = group.Key.MaterialEntity;
                if (!materialEntity.Valid) {
                    continue;
                }
                ref var materialState = ref materialEntity.GetState<MaterialState>();
                if (!materialState.Loaded
                        || !MeshFilter.Check(materialState)) {
                    continue;
                }

                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var state)) {
                    continue;
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, state.Handle.Handle);
                GL.BindBufferBase(BufferTargetARB.TransformFeedbackBuffer, 0, group.PreCulledInstanceBuffer.Handle);
                GL.BindVertexArray(group.PreCullingVertexArrayHandle.Handle);

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