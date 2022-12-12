namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableUpdator : VirtualLayer, ILoadListener, IEngineUpdateListener
{
    private Query<Modified<MeshRenderable>, MeshRenderable> _modifiedQ = new();
    private Group<MeshRenderable> _renderables = new();

    [AllowNull] private IEnumerable<Guid> _dirtyRenderables;
    [AllowNull] private ParallelQuery<Guid> _dirtyRenderablesParallel;

    private ConcurrentDictionary<Guid, (int, int)> _dirtyMeshes = new();

    public void OnLoad(IContext context)
    {
        _dirtyRenderables = QueryUtil.Intersect(_renderables, context.DirtyTransformIds);
        _dirtyRenderablesParallel = _dirtyRenderables.AsParallel();
    }

    public unsafe void OnEngineUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _modifiedQ.Query(context)) {
            ref readonly var renderable = ref context.Inspect<MeshRenderable>(id);
            bool hasVariant = false;

            foreach (var (_, mode) in renderable.Meshes) {
                if (mode == MeshRenderMode.Variant) {
                    hasVariant = true;
                    break;
                }
            }

            if (hasVariant) {
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

        _renderables.Query(context);

        int count = _renderables.Count;
        if (count > 64) {
            _dirtyRenderablesParallel.ForAll(id => DoUpdate(context, id));
        }
        else {
            foreach (var id in _dirtyRenderables) {
                DoUpdate(context, id);
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

    private unsafe void DoUpdate(IContext context, Guid id)
    {
        bool variantUniformUpdated = false;
        ref readonly var data = ref context.Inspect<MeshRenderableData>(id);

        foreach (var (meshId, index) in data.Entries) {
            if (index == -1) {
                if (!variantUniformUpdated) {
                    UpdateVariantUniform(context, id);
                    variantUniformUpdated = true;
                }
                return;
            }

            ref readonly var meshState = ref context.Inspect<MeshRenderingState>(meshId);
            ref readonly var transform = ref context.Inspect<Transform>(id);
            meshState.Instances[index].ObjectToWorld = Matrix4x4.Transpose(transform.World);

            _dirtyMeshes.AddOrUpdate(meshId,
                id => (index, index),
                (id, range) => (Math.Min(index, range.Item1), Math.Max(index, range.Item2)));
        }
    }

    private unsafe void UpdateVariantUniform(IContext context, Guid id)
    {
        ref var buffer = ref context.Acquire<VariantUniformBuffer>(id, out bool exists);
        IntPtr pointer;
        if (!exists) {
            buffer.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, buffer.Handle);
            pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, MeshInstance.MemorySize + 4);
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
