namespace Nagule;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

using Sia;

public abstract class AssetManagerBase<TAsset, TAssetRecord>
    : ViewBase<TypeUnion<TAsset, Sid<IAsset>>>, IAssetManager<TAssetRecord>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAsset
{
    public delegate void CommandListener<TAssetCommand>(EntityRef entity, in TAssetCommand command);
    public delegate void SnapshotCommandListener<TAssetCommand>(EntityRef entity, ref TAsset snapshot, in TAssetCommand command);

    public EntityRef this[TAssetRecord record]
        => CachedEntities.TryGetValue(record, out var entity)
            ? entity : throw new KeyNotFoundException("Entity not found");

    protected ILogger Logger {
        get {
            if (_logger != null) {
                return _logger;
            }
            _logger = World.GetAddon<LogLibrary>().Create<TAsset>();
            return _logger;
        }
    }

    private ILogger? _logger;
    protected readonly Dictionary<TAssetRecord, EntityRef> CachedEntities = [];

    public EntityRef Acquire(TAssetRecord record, AssetLife life = AssetLife.Persistent)
    {
        ref var entity = ref CollectionsMarshal.GetValueRefOrAddDefault(
            CachedEntities, record, out bool exists);
        if (!exists) {
            entity = TAsset.CreateEntity(World, record, life);
        }
        return entity;
    }

    public EntityRef Acquire(TAssetRecord record, in EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = Acquire(record, life);
        referrer.ReferAsset(entity);
        return entity;
    }

    public bool TryGet(TAssetRecord record, out EntityRef entity)
        => CachedEntities.TryGetValue(record, out entity);
    
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
        ref var assetKey = ref entity.Get<Sid<IAsset>>();
        if (assetKey.Value is TAssetRecord record) {
            CachedEntities.Remove(record);
        }
    }
}

public abstract class AssetManagerBase<TAsset, TAssetRecord, TAssetState>
    : AssetManagerBase<TAsset, TAssetRecord>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAsset
    where TAssetState : struct
{
    protected virtual EntityRef CreateState(in EntityRef entity, in TAsset asset)
    {
        ref var state = ref entity.Get<State>();
        state.Entity = World.CreateInBucketHost(
            Tuple.Create(new TAssetState(), new AssetSnapshot<TAsset>(asset)));
        return state.Entity;
    }

    protected virtual void DestroyState(in EntityRef entity, in TAsset asset, ref State state)
    {
        state.Entity.Dispose();
        state.Entity = default;
    }
}