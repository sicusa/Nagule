namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshRenderableManager : ResourceManagerBase<MeshRenderable>
{
    private class InitializeEntryCommand : Command<InitializeEntryCommand, RenderTarget>
    {
        public Guid RenderableId;
        public Guid MeshId;
        public MeshBufferMode BufferMode;
        public Matrix4x4 World;

        public override Guid? Id => RenderableId;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<MeshRenderableData>(RenderableId);
            InitializeEntry(context, RenderableId, ref data, MeshId, BufferMode, in World);
        }
    }

    private class ReinitializeEntryCommand : Command<ReinitializeEntryCommand, RenderTarget>
    {
        public Guid RenderableId;
        public Guid MeshId;
        public MeshBufferMode BufferMode;
        public Matrix4x4 World;
        
        public override Guid? Id => RenderableId;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<MeshRenderableData>(RenderableId);

            if (!data.Entries.TryGetValue(MeshId, out int index)) {
                throw new InvalidOperationException("Internal error: mesh entry not found");
            }

            if (index == -1 && BufferMode == MeshBufferMode.Variant
                    || BufferMode == MeshBufferMode.Instance) {
                return;
            }

            data.Entries.Remove(MeshId);
            UninitializeEntry(context, RenderableId, MeshId, index);
            InitializeEntry(context, RenderableId, ref data, MeshId, BufferMode, in World);
        }
    }

    private class UninitializeEntryCommand : Command<UninitializeEntryCommand, RenderTarget>
    {
        public Guid RenderableId;
        public Guid MeshId;

        public unsafe override void Execute(ICommandContext context)
        {
            if (!context.Remove<MeshRenderableData>(RenderableId, out var data)) {
                return;
            }
            if (!data.Entries.Remove(MeshId, out int index)) {
                throw new InvalidOperationException("Internal error: mesh entry not found");
            }
            UninitializeEntry(context, RenderableId, MeshId, index);
        }
    }
    
    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderableId;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<MeshRenderableData>(RenderableId);
            foreach (var (meshId, index) in data.Entries) {
                UninitializeEntry(context, RenderableId, meshId, index);
            }
            data.Entries.Clear();
        }
    }


    private Dictionary<Guid, int> _entriesToRemove = new();

    protected unsafe override void Initialize(IContext context, Guid id, MeshRenderable resource, MeshRenderable? prevResource)
    {
        if (prevResource != null) {
            ResourceLibrary<Mesh>.UpdateReferences(
                context, id, resource.Meshes,
                (context, id, meshId, mesh, bufferMode) => {
                    var cmd = InitializeEntryCommand.Create();
                    cmd.RenderableId = id;
                    cmd.MeshId = meshId;
                    cmd.BufferMode = bufferMode;
                    cmd.World = context.AcquireRaw<Transform>(id).World;
                    context.SendCommandBatched(cmd);
                },
                (context, id, meshId, mesh, bufferMode) => {
                    var cmd = ReinitializeEntryCommand.Create();
                    cmd.RenderableId = id;
                    cmd.MeshId = meshId;
                    cmd.BufferMode = bufferMode;
                    cmd.World = context.AcquireRaw<Transform>(id).World;
                    context.SendCommandBatched(cmd);
                },
                (context, id, meshId) => {
                    var cmd = UninitializeEntryCommand.Create();
                    cmd.RenderableId = id;
                    cmd.MeshId = meshId;
                    context.SendCommandBatched(cmd);
                });
        }
        else {
            var world = context.AcquireRaw<Transform>(id).World;

            foreach (var (mesh, bufferMode) in resource.Meshes) {
                var cmd = InitializeEntryCommand.Create();
                cmd.RenderableId = id;
                cmd.MeshId = ResourceLibrary<Mesh>.Reference(context, id, mesh);
                cmd.BufferMode = bufferMode;
                cmd.World = world;
                context.SendCommandBatched(cmd);
            }
        }
    }

    protected unsafe override void Uninitialize(IContext context, Guid id, MeshRenderable renderable)
    {
        var cmd = UninitializeCommand.Create();
        cmd.RenderableId = id;
        context.SendCommandBatched(cmd);
    }

    private static unsafe void InitializeEntry(
        ICommandContext context, Guid id, ref MeshRenderableData data, Guid meshId, MeshBufferMode mode, in Matrix4x4 world)
    {
        ref var state = ref context.Acquire<MeshRenderState>(meshId, out bool exists);

        if (mode == MeshBufferMode.Variant) {
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

        state.Instances[index].ObjectToWorld = Matrix4x4.Transpose(world);
        state.InstanceIds[index] = id;
        state.InstanceCount++;

        if (context.Contains<MeshData>(meshId)) {
            ref var meshData = ref context.Require<MeshData>(meshId);
            var pointer = meshData.InstanceBufferPointer;
            var instances = state.Instances;
            if (index >= meshData.InstanceCapacity) {
                ExpandMeshBuffer(index, ref meshData);
            }
            *((MeshInstance*)pointer + index) = instances[index];
        }
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

    private static unsafe void UninitializeEntry(ICommandContext context, Guid id, Guid meshId, int index)
    {
        ref var state = ref context.Acquire<MeshRenderState>(meshId);

        if (index == -1) {
            state.VariantIds.Remove(meshId);
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

        ref var meshData = ref context.Require<MeshData>(meshId);
        var pointer = meshData.InstanceBufferPointer;
        ((MeshInstance*)pointer + index)->ObjectToWorld.M11 = float.NaN;
    }

    private static void FindNextMinimumIndex(ref MeshRenderState state)
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

    private static void FindLastInstanceIndex(ref MeshRenderState state)
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