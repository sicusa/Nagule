namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;

using global::OpenTK.Graphics.OpenGL4;

using Nagule.Graphics;

public class MeshRenderableManager : ObjectManagerBase<MeshRenderable, MeshRenderableData>
{
    protected unsafe override void Initialize(IContext context, Guid id, ref MeshRenderable renderable, ref MeshRenderableData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in renderable, in data);
        }

        var meshId = ResourceLibrary<MeshResource>.Reference<Mesh>(context, renderable.Mesh, id);
        ref var state = ref context.Acquire<MeshRenderingState>(meshId, out bool exists);

        data.MeshId = meshId;

        if (renderable.IsVariant) {
            state.VariantIds.Add(id);
            data.InstanceIndex = -1;
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

        var instances = state.Instances;
        int index = state.InstanceCount++;
        data.InstanceIndex = index;

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
                MeshManager.InitializeInstanceBuffer(BufferTarget.ArrayBuffer, newBuffer, ref meshData);

                int instanceBufferHandle = meshData.BufferHandles[MeshBufferType.Instance];
                GL.BindBuffer(BufferTarget.CopyReadBuffer, instanceBufferHandle);
                GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.ArrayBuffer,
                    IntPtr.Zero, IntPtr.Zero, state.InstanceCount * MeshInstance.MemorySize);

                GL.BindBuffer(BufferTarget.CopyReadBuffer, 0);
                GL.DeleteBuffer(instanceBufferHandle);

                GL.BindVertexArray(meshData.VertexArrayHandle);
                MeshManager.InitializeInstanceCulling(ref meshData);
                GL.BindVertexArray(0);

                meshData.BufferHandles[MeshBufferType.Instance] = newBuffer;
            }
        }
    }

    protected unsafe override void Uninitialize(IContext context, Guid id, in MeshRenderable renderable, in MeshRenderableData data)
    {
        ResourceLibrary<MeshResource>.Unreference(context, data.MeshId, id);

        ref var state = ref context.Acquire<MeshRenderingState>(data.MeshId);

        int index = data.InstanceIndex;
        if (index == -1) {
            state.VariantIds.Remove(id);
            return;
        }

        var instances = state.Instances;
        for (int i = index + 1; i < state.InstanceCount; ++i) {
            instances[i - 1] = instances[i];
        }
        var instanceIds = state.InstanceIds;
        for (int i = index + 1; i < state.InstanceCount; ++i) {
            var otherId = instanceIds[i];
            instanceIds[i - 1] = otherId;
            --context.Require<MeshRenderableData>(otherId).InstanceIndex;
        }

        --state.InstanceCount;
        if (index != state.InstanceCount && context.TryGet<MeshData>(data.MeshId, out var meshData)) {
            fixed (MeshInstance* ptr = instances) {
                int offset = index * MeshInstance.MemorySize;
                int length = (state.InstanceCount - index) * MeshInstance.MemorySize;
                System.Buffer.MemoryCopy(ptr + index, (void*)(meshData.InstanceBufferPointer + offset), length, length);
            }
        }
    }
}