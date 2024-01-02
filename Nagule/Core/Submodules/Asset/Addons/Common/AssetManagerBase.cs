namespace Nagule;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Sia;

public abstract class AssetManagerBase<TAsset, TAssetTemplate, TAssetState>
    : ViewBase<TypeUnion<TAsset, Sid<IAsset>>>, IAssetManager<TAssetTemplate>
    where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
    where TAssetTemplate : IAsset
    where TAssetState : struct
{
    public delegate void CommandListener<TAssetCommand>(EntityRef entity, in TAssetCommand command);
    public delegate void SnapshotCommandListener<TAssetCommand>(EntityRef entity, ref TAsset snapshot, in TAssetCommand command);

    public EntityRef this[TAssetTemplate template]
        => _cachedEntities.TryGetValue(template, out var entity)
            ? entity : throw new KeyNotFoundException("Entity not found");

    [AllowNull] protected ILogger Logger { get; private set; }

    private readonly Dictionary<TAssetTemplate, EntityRef> _cachedEntities = [];

    public EntityRef Acquire(TAssetTemplate template, AssetLife life = AssetLife.Persistent)
    {
        ref var entity = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _cachedEntities, template, out bool exists);
        if (!exists) {
            entity = TAsset.CreateEntity(World, template, life);
        }
        return entity;
    }

    public EntityRef Acquire(TAssetTemplate template, in EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = Acquire(template, life);
        referrer.Modify(new AssetMetadata.Refer(entity));
        return entity;
    }

    public bool TryGet(TAssetTemplate template, out EntityRef entity)
        => _cachedEntities.TryGetValue(template, out entity);
    
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
        Logger = world.GetAddon<LogLibrary>().Create<TAsset>();
    }

    protected override void OnEntityAdded(in EntityRef entity)
    {
        ref var asset = ref entity.Get<TAsset>();
        var stateEntity = CreateState(entity, asset);
        LoadAsset(entity, ref asset, stateEntity);
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var assetKey = ref entity.Get<Sid<IAsset>>();
        if (assetKey.Value is TAssetTemplate template) {
            _cachedEntities.Remove(template);
        }
        ref var asset = ref entity.Get<TAsset>();
        ref var state = ref entity.Get<State>();
        UnloadAsset(entity, ref asset, state.Entity);
        DestroyState(entity, asset, ref state);
    }

    protected virtual EntityRef CreateState(EntityRef entity, in TAsset asset)
    {
        ref var state = ref entity.Get<State>();
        state.Entity = World.CreateInBucketHost(
            Tuple.Create(new TAssetState(), new AssetSnapshot<TAsset>(asset)));
        return state.Entity;
    }

    protected virtual void DestroyState(EntityRef entity, in TAsset asset, ref State state)
    {
        state.Entity.Dispose();
        state.Entity = default;
    }

    protected abstract void LoadAsset(EntityRef entity, ref TAsset asset, EntityRef stateEntity);
    protected virtual void UnloadAsset(EntityRef entity, ref TAsset asset, EntityRef stateEntity) {}
}