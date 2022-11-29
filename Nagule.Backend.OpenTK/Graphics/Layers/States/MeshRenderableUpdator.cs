namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Collections.Concurrent;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableUpdator : VirtualLayer, IUpdateListener
{
    private Group<MeshRenderable> _renderables = new();
    private ParallelQuery<Guid> _renderablesParallel;
    private ConcurrentDictionary<Guid, (int, int)> _dirtyMeshes = new();

    public MeshRenderableUpdator()
    {
        _renderablesParallel = _renderables.AsParallel();
    }

    public unsafe void OnUpdate(IContext context, float deltaTime)
    {
        foreach (var id in context.Query<Modified<MeshRenderable>>()) {
            if (!context.TryGet<MeshRenderable>(id, out var renderable))  {
                continue;
            }
            if (renderable.IsVariant) {
                UpdateVariantUniform(context, id);
            }
            else if (context.Remove<VariantUniformBuffer>(id, out var handle)) {
                GL.DeleteBuffer(handle.Handle);
            }
        }

        foreach (var id in context.Query<Removed<MeshRenderable>>()) {
            if (context.Remove<VariantUniformBuffer>(id, out var handle)) {
                GL.DeleteBuffer(handle.Handle);
            }
        }

        var dirtyIds = context.AcquireAny<DirtyTransforms>().Ids;
        _renderables.Query(context);

        int count = _renderables.Count;
        if (count > 64) {
            _renderablesParallel.ForAll(id => DoUpdate(context, id, dirtyIds));
        }
        else {
            foreach (var id in _renderables) {
                DoUpdate(context, id, dirtyIds);
            }
        }

        foreach (var (meshId, range) in _dirtyMeshes) {
            ref readonly var meshData = ref context.Inspect<MeshData>(meshId);
            ref readonly var meshState = ref context.Inspect<MeshRenderingState>(meshId);
            var src = new Span<MeshInstance>(meshState.Instances, range.Item1, range.Item2 - range.Item1 + 1);
            var dst = new Span<MeshInstance>((void*)meshData.InstanceBufferPointer, meshState.InstanceCount);
            src.CopyTo(dst);
        }

        _dirtyMeshes.Clear();
    }

    private unsafe void DoUpdate(IContext context, Guid id, HashSet<Guid> dirtyIds)
    {
        if (!dirtyIds.Contains(id)) {
            return;
        }

        ref readonly var data = ref context.Inspect<MeshRenderableData>(id);
        int index = data.InstanceIndex;

        if (index == -1) {
            UpdateVariantUniform(context, id);
            return;
        }

        ref readonly var meshState = ref context.Inspect<MeshRenderingState>(data.MeshId);
        ref readonly var transform = ref context.Inspect<Transform>(id);
        meshState.Instances[index].ObjectToWorld = Matrix4x4.Transpose(transform.World);

        _dirtyMeshes.AddOrUpdate(data.MeshId,
            id => (index, index),
            (id, range) => (Math.Min(index, range.Item1), Math.Max(index, range.Item2)));
    }

    private unsafe void UpdateVariantUniform(IContext context, Guid id)
    {
        ref var buffer = ref context.Acquire<VariantUniformBuffer>(id, out bool exists);
        IntPtr pointer;
        if (!exists) {
            buffer.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, buffer.Handle);
            pointer = GLHelper.InitializeBuffer(BufferTarget.UniformBuffer, MeshInstance.MemorySize + 4);
            buffer.Pointer = pointer;
        }
        else {
            pointer = buffer.Pointer;
        }

        ref var matrices = ref context.UnsafeInspect<Transform>(id);
        var world = Matrix4x4.Transpose(matrices.World);

        var ptr = (Matrix4x4*)pointer;
        *ptr = world;
        *((bool*)(ptr + 1)) = true;
    }
}
