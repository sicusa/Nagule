namespace Nagule;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

using Sia;

public abstract class AssetManagerBase<TAsset, TAssetRecord> : ViewBase<TypeUnion<TAsset>>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAssetRecord
{
    public delegate void CommandListener<TAssetCommand>(in EntityRef entity, in TAssetCommand command);
    public delegate void SnapshotCommandListener<TAssetCommand>(in EntityRef entity, ref TAsset snapshot, in TAssetCommand command);

    protected ILogger Logger {
        get {
            if (_logger != null) {
                return _logger;
            }
            _logger = World.CreateLogger<TAsset>();
            return _logger;
        }
    }

    public SimulationFramer SimulationFramer => World.GetAddon<SimulationFramer>();

    private ILogger? _logger;

    protected ref TAsset GetSnapshot(in EntityRef entity)
        => ref entity.GetState<AssetSnapshot<TAsset>>().Asset;

    protected void Listen<TEvent>(CommandListener<TEvent> listener)
        where TEvent : IEvent
    {
        bool Listener(in EntityRef entity, in TEvent e)
        {
            listener(entity, e);
            return false;
        }
        Listen<TEvent>(Listener);
    }

    protected void Listen<TCommand>(SnapshotCommandListener<TCommand> listener)
        where TCommand : ICommand<TAsset>
    {
        bool Listener(in EntityRef entity, in TCommand command)
        {
            ref var snapshot = ref GetSnapshot(entity);
            listener(entity, ref snapshot, command);
            command.Execute(World, entity, ref snapshot);
            return false;
        }
        Listen<TCommand>(Listener);
    }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RuntimeHelpers.RunClassConstructor(typeof(TAsset).TypeHandle);
    }

    protected override void OnEntityAdded(in EntityRef entity) {}
    protected override void OnEntityRemoved(in EntityRef entity)
    {
    }
}

public abstract class AssetManagerBase<TAsset, TAssetRecord, TAssetState>
    : AssetManagerBase<TAsset, TAssetRecord>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAssetRecord
    where TAssetState : struct
{
    protected virtual EntityRef CreateState(in EntityRef entity, in TAsset asset)
    {
        ref var state = ref entity.Get<State>();
        state.Entity = World.CreateInBucketHost(
            Bundle.Create(new TAssetState(), new AssetSnapshot<TAsset>(asset)));
        return state.Entity;
    }

    protected virtual void DestroyState(in EntityRef entity, in TAsset asset, ref State state)
    {
        state.Entity.Dispose();
        state.Entity = default;
    }
}