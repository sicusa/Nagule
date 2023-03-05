namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Reactive.Disposables;

using Nagule.Graphics;

public class MeshRenderableManager : ResourceManagerBase<MeshRenderable>
{
    private class InitializeEntryCommand : Command<InitializeEntryCommand, RenderTarget>
    {
        public Guid RenderableId;
        public Guid MeshId;
        public Matrix4x4 World;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<MeshRenderableData>(RenderableId);
            InitializeEntry(host, RenderableId, ref data, MeshId, in World);
        }
    }

    private class UninitializeEntryCommand : Command<UninitializeEntryCommand, RenderTarget>
    {
        public Guid RenderableId;
        public Guid MeshId;

        public unsafe override void Execute(ICommandHost host)
        {
            if (!host.Remove<MeshRenderableData>(RenderableId, out var data)) {
                return;
            }
            if (!data.Entries.Remove(MeshId, out int index)) {
                throw new InvalidOperationException("Internal error: mesh entry not found");
            }
            UninitializeEntry(host, RenderableId, MeshId, index);
        }
    }
    
    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderableId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<MeshRenderableData>(RenderableId);
            foreach (var (meshId, index) in data.Entries) {
                UninitializeEntry(host, RenderableId, meshId, index);
            }
            data.Entries.Clear();
        }
    }


    private Dictionary<Guid, int> _entriesToRemove = new();

    protected unsafe override void Initialize(IContext context, Guid id, MeshRenderable resource, MeshRenderable? prevResource)
    {
        MeshRenderable.GetProps(context, id).Set(resource);

        var world = context.AcquireRaw<Transform>(id).World;
        if (prevResource != null) {
            ResourceLibrary.UpdateReferences(
                context, id, resource.Meshes,
                (context, id, meshId, mesh) => {
                    var cmd = InitializeEntryCommand.Create();
                    cmd.RenderableId = id;
                    cmd.MeshId = meshId;
                    cmd.World = world;
                    context.SendCommandBatched(cmd);
                },
                (context, id, meshId, mesh) => {},
                (context, id, meshId) => {
                    var cmd = UninitializeEntryCommand.Create();
                    cmd.RenderableId = id;
                    cmd.MeshId = meshId;
                    context.SendCommandBatched(cmd);
                });
        }
        else {
            foreach (var mesh in resource.Meshes) {
                var cmd = InitializeEntryCommand.Create();
                cmd.RenderableId = id;
                cmd.MeshId = ResourceLibrary.Reference(context, id, mesh);
                cmd.World = world;
                context.SendCommandBatched(cmd);
            }
        }
    }

    protected override IDisposable? Subscribe(IContext context, Guid id, MeshRenderable resource)
    {
        ref var props = ref MeshRenderable.GetProps(context, id);
        
        return new CompositeDisposable(
            props.Meshes.Subscribe(e => {
                switch (e.Operation) {
                case ReactiveSetOperation.Add:
                    var initializeCmd = InitializeEntryCommand.Create();
                    initializeCmd.RenderableId = id;
                    initializeCmd.MeshId = ResourceLibrary.Reference(context, id, e.Value);
                    initializeCmd.World = context.AcquireRaw<Transform>(id).World;
                    context.SendCommandBatched(initializeCmd);
                    break;
                case ReactiveSetOperation.Remove:
                    if (!ResourceLibrary.Unreference(context, id, e.Value, out var meshId)) {
                        Console.WriteLine("Internal error: MeshRenderabel mesh not found");
                    }
                    var uninitializeCmd = UninitializeEntryCommand.Create();
                    uninitializeCmd.RenderableId = id;
                    uninitializeCmd.MeshId = meshId;
                    context.SendCommandBatched(uninitializeCmd);
                    break;
                }
            })
        );
    }

    protected unsafe override void Uninitialize(IContext context, Guid id, MeshRenderable renderable)
    {
        var cmd = UninitializeCommand.Create();
        cmd.RenderableId = id;
        context.SendCommandBatched(cmd);
    }

    private static unsafe void InitializeEntry(
        ICommandHost host, Guid id, ref MeshRenderableData data, Guid meshId, in Matrix4x4 world)
    {
        ref var state = ref host.Acquire<MeshRenderState>(meshId, out bool exists);

        if (!exists) {
            state.Instances = new MeshInstance[MeshRenderState.InitialCapacity];
            state.InstanceIds = new Guid[MeshRenderState.InitialCapacity];
            state.InstanceCount = 0;
        }
        else {
            var capacity = state.Instances.Length;
            if (state.InstanceCount >= capacity) {
                int newCapacity = capacity * 2;
                var oldInstances = state.Instances.AsSpan();
                var oldInstanceIds = state.InstanceIds.AsSpan();

                state.Instances = new MeshInstance[newCapacity];
                state.InstanceIds = new Guid[newCapacity];

                oldInstances.CopyTo(state.Instances.AsSpan());
                oldInstanceIds.CopyTo(state.InstanceIds.AsSpan());
            }
        }

        int index = state.InstanceCount;
        data.Entries[meshId] = index;

        state.Instances[index].ObjectToWorld = world;
        state.InstanceIds[index] = id;
        state.InstanceCount++;

        if (!host.Contains<MeshData>(meshId)) {
            return;
        }

        ref var meshData = ref host.Require<MeshData>(meshId);
        MeshHelper.EnsureBufferCapacity(ref meshData, index + 1);

        var pointer = meshData.InstanceBufferPointer;
        ((MeshInstance*)pointer)[index] = state.Instances[index];
    }

    private static unsafe void UninitializeEntry(ICommandHost host, Guid id, Guid meshId, int index)
    {
        ref var state = ref host.Acquire<MeshRenderState>(meshId);
        state.InstanceCount--;

        if (state.InstanceCount == index) {
            return;
        }

        var instances = state.Instances;
        var instanceIds = state.InstanceIds;

        ref var meshData = ref host.Require<MeshData>(meshId);
        var pointer = (MeshInstance*)meshData.InstanceBufferPointer;

        int lastInstanceIndex = state.InstanceCount;
        instances[index] = instances[lastInstanceIndex];
        pointer[index] = pointer[lastInstanceIndex];

        var lastInstanceId = instanceIds[lastInstanceIndex];
        host.Require<MeshRenderableData>(lastInstanceId).Entries[meshId] = index;
    }
}