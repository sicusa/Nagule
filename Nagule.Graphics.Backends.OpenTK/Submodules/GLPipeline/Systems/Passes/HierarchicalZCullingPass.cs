namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class HierarchicalZCullingPass : RenderPassBase
{
    public GroupPredicate GroupPredicate { get; init; } = GroupPredicates.Any;
    public MaterialPredicate MaterialPredicate { get; init; } = MaterialPredicates.Any;

    private EntityRef _cullProgramEntity;
    private EntityRef _cullProgramState;

    private Mesh3DManager? _meshManager;
    private Mesh3DInstanceLibrary? _instanceLib;

    private static readonly RGLSLProgram s_cullProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.cull_hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.pipeline.cull_hiz.vert.glsl")),
            new(ShaderType.Geometry,
                ShaderUtils.LoadCore("nagule.pipeline.cull.geo.glsl")))
        .WithFeedbacks("CulledObjectToWorld", "CulledLayerMask");

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _cullProgramEntity = MainWorld.AcquireAsset(s_cullProgramAsset);
        _cullProgramState = _cullProgramEntity.GetStateEntity();

        _meshManager = MainWorld.GetAddon<Mesh3DManager>();
        _instanceLib = MainWorld.GetAddon<Mesh3DInstanceLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cullProgramState = ref _cullProgramState.Get<GLSLProgramState>();
        if (!cullProgramState.Loaded) { return; }

        var buffer = world.GetAddon<HierarchicalZBuffer>();

        GL.UseProgram(cullProgramState.Handle.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, buffer!.TextureHandle.Handle);

        foreach (var group in _instanceLib!.Groups.Values) {
            if (group.Count == 0 || !GroupPredicate(group)) { continue; }

            var matState = group.Key.MaterialState.Get<MaterialState>();
            if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

            if (!_meshManager!.DataBuffers.TryGetValue(group.Key.MeshData, out var meshBuffer)) {
                continue;
            }

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, meshBuffer.Handle.Handle);
            group.Cull();
        }

        GL.UseProgram(0);
        GL.BindVertexArray(0);
        GL.Disable(EnableCap.RasterizerDiscard);
    }
}