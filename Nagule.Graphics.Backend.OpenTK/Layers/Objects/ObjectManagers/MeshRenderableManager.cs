namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Numerics;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshRenderableManager : ObjectManagerBase<MeshRenderable, MeshRenderableData>
{
    private Dictionary<Guid, int> _entriesToRemove = new();

    protected unsafe override void Initialize(IContext context, Guid id, ref MeshRenderable renderable, ref MeshRenderableData data, bool updating)
    {
        if (updating) {
            foreach (var (meshId, instanceIndex) in data.Entries) {
                _entriesToRemove[meshId] = instanceIndex;
            }
        }

        foreach (var (meshRes, meshRenderMode) in renderable.Meshes) {
            InitializeEntry(context, id, ref data, meshRes, meshRenderMode);
        }

        if (updating) {
            var entries = data.Entries;
            foreach (var (meshId, instanceIndex) in _entriesToRemove) {
                UninitializeEntry(context, id, meshId, instanceIndex);
                entries.Remove(meshId);
            }
            _entriesToRemove.Clear();
        }
    }

    protected unsafe override void Uninitialize(IContext context, Guid id, in MeshRenderable renderable, in MeshRenderableData data)
    {
        foreach (var (meshId, instanceIndex) in data.Entries) {
            UninitializeEntry(context, id, meshId, instanceIndex);
        }
    }

    private unsafe void InitializeEntry(IContext context, Guid id, ref MeshRenderableData data, MeshResource meshRes, MeshRenderMode mode)
    {
        var meshId = ResourceLibrary<MeshResource>.Reference<Mesh>(context, in meshRes, id);
        if (_entriesToRemove.Remove(meshId)) {
            return;
        }

        ref var state = ref context.Acquire<MeshRenderingState>(meshId, out bool exists);

        if (mode == MeshRenderMode.Variant) {
            state.VariantIds.Add(id);
            data.Entries[meshId] = -1;
            return;
        }

        if (!exists) {
            state.Instances = new MeshInstance[MeshRenderingState.InitialCapacity];
            state.InstanceIds = new Guid[MeshRenderingState.InitialCapacity];
            state.InstanceCount = 0;
        }
        else {
            var capacity = state.Instances.Length;

            if (state.InstanceCount >= capacity) {
                var newInstances = new MeshInstance[capacity * 2];
                Array.Copy(state.Instances, newInstances, capacity);
                state.Instances = newInstances;

                var newInstanceIds = new Guid[capacity * 2];
                Array.Copy(state.InstanceIds, newInstanceIds, capacity);
                state.InstanceIds = newInstanceIds;
            }
        }

        int index = state.InstanceCount++;
        data.Entries[meshId] = index;

        var instances = state.Instances;
        ref var transform = ref context.Acquire<Transform>(id);
        instances[index].ObjectToWorld = Matrix4x4.Transpose(transform.World);
        state.InstanceIds[index] = id;

        if (context.Contains<MeshData>(meshId)) {
            ref var meshData = ref context.Require<MeshData>(meshId);
            if (state.InstanceCount <= meshData.InstanceCapacity) {
                *((MeshInstance*)meshData.InstanceBufferPointer + index) = instances[index];
            }
            else {
                meshData.InstanceCapacity *= 2;

                var newBuffer = GL.GenBuffer();
                MeshManager.InitializeInstanceBuffer(BufferTargetARB.ArrayBuffer, newBuffer, ref meshData);

                var instanceBufferHandle = meshData.BufferHandles[MeshBufferType.Instance];
                GL.BindBuffer(BufferTargetARB.CopyReadBuffer, instanceBufferHandle);
                GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.ArrayBuffer,
                    IntPtr.Zero, IntPtr.Zero, state.InstanceCount * MeshInstance.MemorySize);

                GL.BindBuffer(BufferTargetARB.CopyReadBuffer, BufferHandle.Zero);
                GL.DeleteBuffer(instanceBufferHandle);

                GL.BindVertexArray(meshData.VertexArrayHandle);
                MeshManager.InitializeInstanceCulling(ref meshData);
                GL.BindVertexArray(VertexArrayHandle.Zero);

                meshData.BufferHandles[MeshBufferType.Instance] = newBuffer;
            }
        }
    }

    private unsafe void UninitializeEntry(IContext context, Guid id, Guid meshId, int instanceIndex)
    {
        ResourceLibrary<MeshResource>.Unreference(context, meshId, id);

        ref var state = ref context.Acquire<MeshRenderingState>(meshId);

        if (instanceIndex == -1) {
            state.VariantIds.Remove(id);
            return;
        }

        var instances = state.Instances;
        for (int i = instanceIndex + 1; i < state.InstanceCount; ++i) {
            instances[i - 1] = instances[i];
        }
        var instanceIds = state.InstanceIds;
        for (int i = instanceIndex + 1; i < state.InstanceCount; ++i) {
            var otherId = instanceIds[i];
            instanceIds[i - 1] = otherId;
            --context.Require<MeshRenderableData>(otherId).Entries[meshId];
        }

        --state.InstanceCount;
        if (instanceIndex != state.InstanceCount && context.TryGet<MeshData>(meshId, out var meshData)) {
            fixed (MeshInstance* ptr = instances) {
                int offset = instanceIndex * MeshInstance.MemorySize;
                int length = (state.InstanceCount - instanceIndex) * MeshInstance.MemorySize;
                System.Buffer.MemoryCopy(ptr + instanceIndex, (void*)(meshData.InstanceBufferPointer + offset), length, length);
            }
        }
    }
}