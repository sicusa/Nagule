namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class FrustumCullingPass : RenderPassSystemBase
{
    private static readonly GLSLProgramAsset s_cullProgramAsset = 
        new GLSLProgramAsset {
            Name = "nagule.pipeline.cull_frustum"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadEmbedded("nagule.pipeline.cull_frustum.vert.glsl")),
            new(ShaderType.Geometry,
                ShaderUtils.LoadEmbedded("nagule.pipeline.cull.geo.glsl")))
        .WithFeedback("CulledObjectToWorld");
    
    public MeshFilter MeshFilter { get; init; } = MeshFilter.All;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var materialManager = world.GetAddon<MaterialManager>();
        var programManager = world.GetAddon<GLSLProgramManager>();
        var meshManager = world.GetAddon<Mesh3DManager>();
        var instanceLib = world.GetAddon<GLMesh3DInstanceLibrary>();
        var framebuffer = Pipeline.GetAddon<Framebuffer>();

        var cullProgramEntity = GLSLProgram.CreateEntity(
            world, s_cullProgramAsset, AssetLife.Persistent);

        RenderFrame.Start(() => {
            ref var cullProgramState = ref programManager.RenderStates.GetOrNullRef(cullProgramEntity);
            if (Unsafe.IsNullRef(ref cullProgramState)) {
                return ShouldStop;
            }

            GL.UseProgram(cullProgramState.Handle.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            foreach (var group in instanceLib.Groups.Values) {
                ref var materialState = ref materialManager.RenderStates.Get(group.Key.MaterialEntity);
                if (Unsafe.IsNullRef(ref materialState)
                        || !MeshFilter.Check(materialState)) {
                    continue;
                }

                if (!meshManager.DataStates.TryGetValue(group.Key.MeshData, out var state)) {
                    continue;
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, state.UniformBufferHandle.Handle);
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
            return ShouldStop;
        });
    }
}