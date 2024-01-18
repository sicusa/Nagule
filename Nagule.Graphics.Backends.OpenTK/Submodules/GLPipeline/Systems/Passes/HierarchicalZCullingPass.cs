namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class HierarchicalZCullingPass : RenderPassSystemBase
{
    public GroupPredicate GroupPredicate { get; init; } = GroupPredicates.Any;
    public MaterialPredicate MaterialPredicate { get; init; } = MaterialPredicates.Any;

    private static readonly RGLSLProgram s_hizProgramAsset =
        new RGLSLProgram {
            Name = "nagule.pipeline.hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.pipeline.hiz.frag.glsl")))
        .WithParameter("LastMip", ShaderParameterType.Texture2D);

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
    
    private EntityRef _hizProgramEntity;
    private EntityRef _cullProgramEntity;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var meshManager = world.GetAddon<Mesh3DManager>();
        var instanceLib = world.GetAddon<GLMesh3DInstanceLibrary>();

        _hizProgramEntity = GLSLProgram.CreateEntity(
            world, s_hizProgramAsset, AssetLife.Persistent);
        var hizProgramStateEntity = _hizProgramEntity.GetStateEntity();

        _cullProgramEntity = GLSLProgram.CreateEntity(
            world, s_cullProgramAsset, AssetLife.Persistent);
        var cullProgramStateEntity = _cullProgramEntity.GetStateEntity();

        RenderFramer.Start(() => {
            ref var cullProgramState = ref cullProgramStateEntity.Get<GLSLProgramState>();
            if (!cullProgramState.Loaded) { return NextFrame; }

            var buffer = Pipeline.GetAddon<HierarchicalZBuffer>();
            var framebuffer = Pipeline.GetAddon<Framebuffer>();

            GL.UseProgram(cullProgramState.Handle.Handle);
            GL.Enable(EnableCap.RasterizerDiscard);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, buffer!.TextureHandle.Handle);

            foreach (var group in instanceLib.Groups.Values) {
                if (group.Count == 0 || !GroupPredicate(group)) { continue; }

                var matState = group.Key.MaterialState.Get<MaterialState>();
                if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var meshBuffer)) {
                    continue;
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshBuffer.Handle.Handle);
                group.Cull();
            }

            GL.UseProgram(0);
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.RasterizerDiscard);
            
            return NextFrame;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        _cullProgramEntity.Dispose();
        _hizProgramEntity.Dispose();
    }
}