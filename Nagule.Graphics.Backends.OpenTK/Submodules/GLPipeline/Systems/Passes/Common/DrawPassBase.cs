namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public delegate bool GroupPredicate(Mesh3DInstanceGroup group);
public delegate bool MaterialPredicate(in MaterialState state);

public static class GroupPredicates
{
    public static bool Any(Mesh3DInstanceGroup g) => true;
    public static bool IsOccluder(Mesh3DInstanceGroup g) => g.Key.MeshData.IsOccluder;
    public static bool IsNonOccluder(Mesh3DInstanceGroup g) => !g.Key.MeshData.IsOccluder;
}

public static class MaterialPredicates
{
    public static bool Any(in MaterialState s) => true;

    public static bool IsOpaque(in MaterialState s) => s.RenderMode == RenderMode.Opaque;
    public static bool IsCutoff(in MaterialState s) => s.RenderMode == RenderMode.Cutoff;
    public static bool IsOpaqueOrCutoff(in MaterialState s)
        => s.RenderMode == RenderMode.Opaque || s.RenderMode == RenderMode.Cutoff;
    public static bool IsTransparent(in MaterialState s) => s.RenderMode == RenderMode.Transparent;
    public static bool IsBlending(in MaterialState s) => s.RenderMode == RenderMode.Blending;
}

public abstract class DrawPassBase(
    GroupPredicate? groupPredicate = null,
    MaterialPredicate? materialPredicate = null)
    : RenderPassBase
{
    public GroupPredicate GroupPredicate { get; init; } =
        groupPredicate ?? GroupPredicates.Any;

    public MaterialPredicate MaterialPredicate { get; init; } =
        materialPredicate ?? MaterialPredicates.Any;
    
    public bool IsCulled { get; set; }
    public int DrawnGroupCount { get; set; }
    public int DrawnObjectCount { get; set; }

    private Mesh3DManager? _meshManager;
    private Mesh3DInstanceLibrary? _meshInstanceLib;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _meshManager = MainWorld.GetAddon<Mesh3DManager>();
        _meshInstanceLib = MainWorld.GetAddon<Mesh3DInstanceLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        BeginPass();

        foreach (var group in _meshInstanceLib!.Groups.Values) {
            if (group.Count == 0 || !GroupPredicate(group)) { continue; }

            var matState = group.Key.MaterialState.Get<MaterialState>();
            if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

            if (!_meshManager!.DataBuffers.TryGetValue(group.Key.MeshData, out var meshData)) {
                continue;
            }

            var programStateEntity = GetShaderProgramState(group, meshData, matState);
            if (!programStateEntity.Valid) { continue; }

            ref var programState = ref programStateEntity.Get<GLSLProgramState>();
            if (!programState.Loaded) { continue; }

            if (!BeforeDraw(group, meshData, matState, programState)) {
                continue;
            }

            if (matState.IsTwoSided) {
                GL.Disable(EnableCap.CullFace);
            }

            matState.Bind(programState);

            uint startIndex = programState.EnableInternalBuffers();
            matState.ActivateTextures(programState, startIndex);

            DrawnGroupCount++;
            DrawnObjectCount += Draw(group, meshData, matState, programState);

            if (matState.IsTwoSided) {
                GL.Enable(EnableCap.CullFace);
            }
        }

        GL.BindVertexArray(0);
        EndPass();

        DrawnGroupCount = 0;
        DrawnObjectCount = 0;
    }

    protected virtual void BeginPass() {}
    protected virtual void EndPass() {}

    protected abstract EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState);

    protected virtual bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState) => true;

    protected virtual int Draw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        if (IsCulled) {
            int culledCount = group.CulledCount;
            GL.BindVertexArray(group.CulledVertexArrayHandle.Handle);
            GL.DrawElementsInstanced(
                meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, culledCount);
            return culledCount;
        }
        else {
            GL.BindVertexArray(group.VertexArrayHandle.Handle);
            GL.DrawElementsInstanced(
                meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, group.Count);
            return group.Count;
        }
    }
}