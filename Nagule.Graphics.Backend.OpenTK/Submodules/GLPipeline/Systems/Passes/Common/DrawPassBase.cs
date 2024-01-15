namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
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
    : RenderPassSystemBase
{
    public GroupPredicate GroupPredicate { get; init; } =
        groupPredicate ?? GroupPredicates.Any;

    public MaterialPredicate MaterialPredicate { get; init; } =
        materialPredicate ?? MaterialPredicates.Any;
    
    public bool Cull { get; set; }
    public int DrawnGroupCount { get; set; }
    public int DrawnObjectCount { get; set; }

    [AllowNull] protected Framebuffer Framebuffer { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var meshManager = world.GetAddon<Mesh3DManager>();
        var meshInstanceLibrary = world.GetAddon<GLMesh3DInstanceLibrary>();

        RenderFrame.Start(() => {
            Framebuffer = Pipeline.GetAddon<Framebuffer>();
            return true;
        });

        RenderFrame.Start(() => {
            BeginPass();

            foreach (var group in meshInstanceLibrary.Groups.Values) {
                if (group.Count == 0 || !GroupPredicate(group)) { continue; }

                var matState = group.Key.MaterialState.Get<MaterialState>();
                if (!matState.Loaded || !MaterialPredicate(matState)) { continue; }

                if (!meshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var meshData)) {
                    continue;
                }

                ref var programState = ref GetShaderProgramState(group, meshData, matState)
                    .Get<GLSLProgramState>();
                if (!programState.Loaded) {
                    continue;
                }

                if (!BeforeDraw(group, meshData, matState, programState)) {
                    continue;
                }

                if (matState.IsTwoSided) {
                    GL.Disable(EnableCap.CullFace);
                }

                matState.Bind(programState);

                uint startIndex = programState.EnableLightingBuffers();
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

            return NextFrame;
        });
    }

    protected virtual void BeginPass() {}
    protected virtual void EndPass() {}

    protected abstract EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState);

    protected virtual bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState) => true;

    protected virtual int Draw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        if (Cull) {
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