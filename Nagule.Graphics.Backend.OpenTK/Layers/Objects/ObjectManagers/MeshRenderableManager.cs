namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshRenderableManager : ObjectManagerBase<MeshRenderable, MeshRenderableData>
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public Guid MeshId;
        public int Index;

        public unsafe override void Execute(IContext context)
        {
            ref var meshData = ref context.Require<MeshData>(MeshId);
            var pointer = meshData.InstanceBufferPointer;

            ref var state = ref context.Require<MeshRenderState>(MeshId);
            var instances = state.Instances;

            if (Index >= meshData.InstanceCapacity) {
                ExpandMeshBuffer(Index, ref meshData);
            }
            *((MeshInstance*)pointer + Index) = instances[Index];
        }

        private static void ExpandMeshBuffer(int index, ref MeshData meshData)
        {
            int prevCapacity = meshData.InstanceCapacity;
            int newCapacity = prevCapacity;

            while (index <= newCapacity) { newCapacity *= 2; }
            meshData.InstanceCapacity = newCapacity;

            var newBuffer = GL.GenBuffer();
            MeshHelper.InitializeInstanceBuffer(BufferTargetARB.ArrayBuffer, newBuffer, ref meshData);

            var instanceBufferHandle = meshData.BufferHandles[MeshBufferType.Instance];
            GL.BindBuffer(BufferTargetARB.CopyReadBuffer, instanceBufferHandle);
            GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.ArrayBuffer,
                IntPtr.Zero, IntPtr.Zero, prevCapacity * MeshInstance.MemorySize);

            GL.BindBuffer(BufferTargetARB.CopyReadBuffer, BufferHandle.Zero);
            GL.DeleteBuffer(instanceBufferHandle);

            GL.BindVertexArray(meshData.VertexArrayHandle);
            MeshHelper.InitializeInstanceCulling(in meshData);
            GL.BindVertexArray(VertexArrayHandle.Zero);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public MeshData MeshData;
        public int Index;

        public unsafe override void Execute(IContext context)
        {
            var pointer = MeshData.InstanceBufferPointer;
            ((MeshInstance*)pointer + Index)->ObjectToWorld.M11 = float.NaN;
        }
    }

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

    private unsafe void InitializeEntry(IContext context, Guid id, ref MeshRenderableData data, Mesh meshRes, MeshRenderMode mode)
    {
        var meshId = ResourceLibrary<Mesh>.Reference(context, in meshRes, id);
        if (_entriesToRemove.Remove(meshId)) {
            return;
        }

        ref var state = ref context.Acquire<MeshRenderState>(meshId, out bool exists);

        if (mode == MeshRenderMode.Variant) {
            state.VariantIds.Add(id);
            data.Entries[meshId] = -1;
            return;
        }

        if (!exists) {
            state.Instances = new MeshInstance[MeshRenderState.InitialCapacity];
            foreach (ref var instance in state.Instances.AsSpan()) {
                instance.ObjectToWorld.M11 = float.PositiveInfinity;
            }
            state.InstanceIds = new Guid[MeshRenderState.InitialCapacity];
            state.InstanceCount = 0;
            state.MinimumEmptyIndex = 0;
            state.MaximumEmptyIndex = MeshRenderState.InitialCapacity - 1;
            state.MaximumInstanceIndex = 0;
        }
        else {
            var capacity = state.Instances.Length;
            if (state.InstanceCount >= capacity) {
                int newCapacity = capacity * 2;
                var newInstances = new MeshInstance[newCapacity];
                var newInstanceIds = new Guid[newCapacity];

                state.Instances.AsSpan().CopyTo(newInstances.AsSpan());
                state.InstanceIds.AsSpan().CopyTo(newInstanceIds.AsSpan());

                foreach (ref var instance in newInstances.AsSpan(capacity, newCapacity - capacity)) {
                    instance.ObjectToWorld.M11 = float.PositiveInfinity;
                }

                state.Instances = newInstances;
                state.InstanceIds = newInstanceIds;
                state.MaximumEmptyIndex = newCapacity - 1;

                FindNextMinimumIndex(ref state);
            }
        }

        int index = state.MinimumEmptyIndex;
        if (index > state.MaximumInstanceIndex) {
            state.MaximumInstanceIndex = index;
        }
        FindNextMinimumIndex(ref state);

        data.Entries[meshId] = index;

        ref readonly var transform = ref context.Inspect<Transform>(id);
        state.Instances[index].ObjectToWorld = Matrix4x4.Transpose(transform.World);
        state.InstanceIds[index] = id;
        state.InstanceCount++;

        if (context.Contains<MeshData>(meshId)) {
            var cmd = InitializeCommand.Create();
            cmd.MeshId = meshId;
            cmd.Index = index;
            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }

    private unsafe void UninitializeEntry(IContext context, Guid id, Guid meshId, int index)
    {
        ResourceLibrary<Mesh>.Unreference(context, meshId, id);

        ref var state = ref context.Acquire<MeshRenderState>(meshId);

        if (index == -1) {
            state.VariantIds.Remove(id);
            return;
        }

        state.Instances[index].ObjectToWorld.M11 = float.NaN;
        state.InstanceCount--;

        if (index < state.MinimumEmptyIndex) {
            state.MinimumEmptyIndex = index;
        }
        else if (index > state.MaximumEmptyIndex) {
            state.MaximumEmptyIndex = index;
        }

        if (index == state.MaximumInstanceIndex) {
            FindLastInstanceIndex(ref state);
        }

        if (context.TryGet<MeshData>(meshId, out var data)) {
            var cmd = UninitializeCommand.Create();
            cmd.MeshData = data;
            cmd.Index = index;
            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }

    private void FindNextMinimumIndex(ref MeshRenderState state)
    {
        var instances = state.Instances;
        int index = state.MinimumEmptyIndex;

        while (index < state.MaximumEmptyIndex) {
            index++;
            if (instances[index].ObjectToWorld.M11 == float.PositiveInfinity) {
                break;
            }
        }

        state.MinimumEmptyIndex = index;
    }

    private void FindLastInstanceIndex(ref MeshRenderState state)
    {
        var instances = state.Instances;
        int index = state.MaximumInstanceIndex;

        while (index > 0) {
            index--;
            if (instances[index].ObjectToWorld.M11 != float.PositiveInfinity) {
                break;
            }
        }

        state.MaximumInstanceIndex = index;
    }
}