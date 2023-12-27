namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class DrawPassBase(MeshFilter meshFilter) : RenderPassSystemBase
{
    public MeshFilter MeshFilter { get; init; } = meshFilter;
    public int MinimumInstanceCount { get; init; }
    public int MaximumInstanceCount { get; init; } = int.MaxValue;

    [AllowNull] protected GLSLProgramManager ProgramManager { get; private set; }
    [AllowNull] protected MaterialManager MaterialManager { get; private set; }
    [AllowNull] protected Mesh3DManager MeshManager { get; private set; }
    [AllowNull] protected GLMesh3DInstanceLibrary MeshInstanceLibrary { get; private set; }
    [AllowNull] protected Framebuffer Framebuffer { get; private set; }

    public DrawPassBase()
        : this(MeshFilter.All)
    {
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        ProgramManager = world.GetAddon<GLSLProgramManager>();
        MaterialManager = world.GetAddon<MaterialManager>();
        MeshManager = world.GetAddon<Mesh3DManager>();
        MeshInstanceLibrary = world.GetAddon<GLMesh3DInstanceLibrary>();
        Framebuffer = Pipeline.GetAddon<Framebuffer>();

        RenderFrame.Start(() => {
            BeginPass();

            foreach (var group in MeshInstanceLibrary.Groups.Values) {
                if (group.Count == 0
                        || group.Count > MaximumInstanceCount
                        || group.Count < MinimumInstanceCount) {
                    continue;
                }

                var matEntity = group.Key.MaterialEntity;
                if (!matEntity.Valid) {
                    continue;
                }
                ref var materialState = ref matEntity.GetState<MaterialState>();
                if (!materialState.Loaded
                        || !MeshFilter.Check(materialState)) {
                    continue;
                }
                if (!MeshManager.DataBuffers.TryGetValue(group.Key.MeshData, out var meshData)) {
                    continue;
                }

                ref var programState = ref GetShaderProgram(group, meshData, materialState)
                    .GetState<GLSLProgramState>();
                if (!programState.Loaded) {
                    continue;
                }

                if (!BeforeDraw(group, meshData, materialState, programState)) {
                    continue;
                }

                if (materialState.IsTwoSided) {
                    GL.Disable(EnableCap.CullFace);
                }

                GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialState.UniformBufferHandle.Handle);
                GL.UseProgram(programState.Handle.Handle);

                int startIndex = programState.EnableBuiltInBuffers();
                materialState.EnableTextures(programState, startIndex);

                Draw(group, meshData, materialState, programState);

                if (materialState.IsTwoSided) {
                    GL.Enable(EnableCap.CullFace);
                }
            }

            GL.BindVertexArray(Framebuffer.EmptyVertexArray.Handle);
            EndPass();
            return ShouldStop;
        });
    }

    protected virtual void BeginPass() {}
    protected virtual void EndPass() {}

    protected abstract EntityRef GetShaderProgram(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState);

    protected virtual bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState) => true;
    protected abstract void Draw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState);
}