namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sia;

public delegate bool GroupPredicate(Mesh3DInstanceGroup group);
public delegate bool MaterialPredicate(in MaterialState state);

public static class GroupPredicates
{
    public static bool Any(Mesh3DInstanceGroup g) => true;
    public static bool IsOccluder(Mesh3DInstanceGroup g) => g.Key.MeshData.IsOccluder ?? false;
    public static bool IsNonOccluder(Mesh3DInstanceGroup g) => !(g.Key.MeshData.IsOccluder ?? false);
}

public static class MaterialPredicates
{
    public static bool Any(in MaterialState s) => true;

    public static bool IsOpaque(in MaterialState s) => s.RenderMode == RenderMode.Opaque;
    public static bool IsCutoff(in MaterialState s) => s.RenderMode == RenderMode.Cutoff;
    public static bool IsTransparent(in MaterialState s) => s.RenderMode == RenderMode.Transparent;
    public static bool IsBlending(in MaterialState s) => s.RenderMode == RenderMode.Blending;
}

public abstract class DrawPassBase(
    GroupPredicate? groupPredicate = null,
    MaterialPredicate? materialPredicate = null)
    : RenderPassSystemBase
{
    public GroupPredicate GroupPredicate { get; init; } =
        groupPredicate ?? GroupPredicates.Any;

    public MaterialPredicate MaterialPredicate { get; init; } =
        materialPredicate ?? MaterialPredicates.Any;
    
    public int DrawnGroupCount { get; set; }
    public int DrawnObjectCount { get; set; }

    [AllowNull] protected Framebuffer Framebuffer { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        Framebuffer = Pipeline.GetAddon<Framebuffer>();

        var meshManager = world.GetAddon<Mesh3DManager>();
        var meshInstanceLibrary = world.GetAddon<GLMesh3DInstanceLibrary>();

        RenderFrame.Start(() => {
            BeginPass();

            foreach (var group in meshInstanceLibrary.Groups.Values) {
                if (group.Count == 0 || !GroupPredicate(group)) {
                    continue;
                }
                var matEntity = group.Key.MaterialEntity;
                if (!matEntity.Valid) {
                    continue;
                }
                ref var matState = ref matEntity.GetState<MaterialState>();
                if (!matState.Loaded || !MaterialPredicate(matState)) {
                    continue;
                }
                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var meshData)) {
                    continue;
                }

                ref var programState = ref GetShaderProgram(group, meshData, matState)
                    .GetStateOrNullRef<GLSLProgramState>();
                if (Unsafe.IsNullRef(ref programState) || !programState.Loaded) {
                    continue;
                }

                if (!BeforeDraw(group, meshData, matState, programState)) {
                    continue;
                }

                if (matState.IsTwoSided) {
                    GL.Disable(EnableCap.CullFace);
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, matState.UniformBufferHandle.Handle);
                GL.UseProgram(programState.Handle.Handle);

                uint startIndex = programState.EnableBuiltInBuffers();
                matState.EnableTextures(programState, startIndex);

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

            return NextFrame;
        });
    }

    protected virtual void BeginPass() {}
    protected virtual void EndPass() {}

    protected abstract EntityRef GetShaderProgram(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState);

    protected virtual bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState) => true;
    protected abstract int Draw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState);
}