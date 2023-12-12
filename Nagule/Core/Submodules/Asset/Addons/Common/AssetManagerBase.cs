namespace Nagule;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

using Sia;

public abstract class AssetManagerBase<TAsset, TAssetTemplate> : ViewBase<TypeUnion<TAsset>>, IAssetManager<TAssetTemplate>
    where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
    where TAssetTemplate : IAsset
{
    public delegate void CommandListener<TAssetCommand>(EntityRef entity, in TAssetCommand command);
    public delegate void SnapshotCommandListener<TAssetCommand>(EntityRef entity, ref TAsset snapshot, in TAssetCommand command);

    [AllowNull] protected ILogger Logger { get; private set; }

    private readonly Dictionary<TAssetTemplate, EntityRef> _cachedEntities = [];
    private readonly Dictionary<EntityRef, TAsset> _snapshots = [];

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

    public EntityRef Get(TAssetTemplate template)
        => _cachedEntities.TryGetValue(template, out var entity) ? entity : throw new KeyNotFoundException("Entity not found");
    
    protected ref TAsset GetSnapshot(in EntityRef entity)
    {
        ref var snapshot = ref CollectionsMarshal.GetValueRefOrAddDefault(_snapshots, entity, out bool exists);
        if (!exists) {
            snapshot = entity.Get<TAsset>();
        }
        return ref snapshot;
    }

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
        LoadAsset(entity, ref asset);
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var asset = ref entity.GetOrNullRef<Sid<IAsset>>();
        if (!Unsafe.IsNullRef(ref asset) && asset.Value is TAssetTemplate template) {
            _cachedEntities.Remove(template);
        }
        _snapshots.Remove(entity);
        UnloadAsset(entity, ref entity.Get<TAsset>());
    }

    protected abstract void LoadAsset(EntityRef entity, ref TAsset asset);
    protected abstract void UnloadAsset(EntityRef entity, ref TAsset asset);
}
