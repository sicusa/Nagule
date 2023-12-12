namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sia;

public abstract class DrawPassBase : RenderPassSystemBase
{
    public MeshFilter MeshFilter { get; init; } = MeshFilter.All;
    public int MinimumInstanceCount { get; init; }
    public int MaximumInstanceCount { get; init; } = int.MaxValue;

    [AllowNull] protected GLSLProgramManager ProgramManager { get; private set; }
    [AllowNull] protected MaterialManager MaterialManager { get; private set; }
    [AllowNull] protected Mesh3DManager MeshManager { get; private set; }
    [AllowNull] protected TextureLibrary TextureLibrary { get; private set; }
    [AllowNull] protected GLMesh3DInstanceLibrary MeshInstanceLibrary { get; private set; }
    [AllowNull] protected Framebuffer Framebuffer { get; private set; }

    public DrawPassBase()
        : this(MeshFilter.All)
    {
    }

    public DrawPassBase(MeshFilter meshFilter)
    {
        MeshFilter = meshFilter;
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        ProgramManager = world.GetAddon<GLSLProgramManager>();
        MaterialManager = world.GetAddon<MaterialManager>();
        MeshManager = world.GetAddon<Mesh3DManager>();
        TextureLibrary = world.GetAddon<TextureLibrary>();
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

                ref var materialState = ref MaterialManager.RenderStates.Get(group.Key.MaterialEntity);
                if (Unsafe.IsNullRef(ref materialState)
                        || !MeshFilter.Check(materialState)) {
                    continue;
                }
                if (!MeshManager.DataStates.TryGetValue(group.Key.MeshData, out var meshData)) {
                    continue;
                }

                ref var programState = ref ProgramManager.RenderStates.GetOrNullRef(
                    GetShaderProgram(group, meshData, materialState));
                if (Unsafe.IsNullRef(ref programState)) {
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
                materialState.EnableTextures(TextureLibrary, programState, startIndex);

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
        Mesh3DInstanceGroup group, Mesh3DDataState meshData, in MaterialState materialState);

    protected virtual bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataState meshData, in MaterialState materialState, in GLSLProgramState programState) => true;
    protected abstract void Draw(Mesh3DInstanceGroup group, Mesh3DDataState meshData, in MaterialState materialState, in GLSLProgramState programState);
}