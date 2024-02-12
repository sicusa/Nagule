namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class FrustumCullingPass : RenderPassBase
{
    public GroupPredicate GroupPredicate { get; init; } = GroupPredicates.Any;
    public MaterialPredicate MaterialPredicate { get; init; } = MaterialPredicates.Any;

    private Mesh3DManager? _meshManager;
    private Mesh3DInstanceLibrary? _instanceLib;
    private EntityRef _cullProgramEntity;
    private EntityRef _cullProgramState;

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

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _meshManager = MainWorld.GetAddon<Mesh3DManager>();
        _instanceLib = MainWorld.GetAddon<Mesh3DInstanceLibrary>();

        _cullProgramEntity = GLSLProgram.CreateEntity(
            MainWorld, s_cullProgramAsset, AssetLife.Persistent);
        _cullProgramState = _cullProgramEntity.GetStateEntity();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cullProgramState = ref _cullProgramState.Get<GLSLProgramState>();
        if (!cullProgramState.Loaded) { return; }

        GL.UseProgram(cullProgramState.Handle.Handle);
        GL.Enable(EnableCap.RasterizerDiscard);

        foreach (var group in _instanceLib!.Groups.Values) {
            if (group.Count == 0 || !GroupPredicate(group)) { continue; }

            var matState = group.Key.MaterialState.Get<MaterialState>();
            if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

            if (!_meshManager!.DataBuffers.TryGetValue(group.Key.MeshData, out var state)) {
                continue;
            }

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, state.Handle.Handle);
            group.Cull();
        }

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.RasterizerDiscard);
        GL.UseProgram(0);
    }
}