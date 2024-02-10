namespace Nagule;

using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

using Sia;

public abstract class AssetManagerBase<TAsset> : ViewBase, IAssetManagerBase<TAsset>
    where TAsset : struct
{
    public delegate void CommandListener<TAssetCommand>(in EntityRef entity, in TAssetCommand command);
    public delegate void EntityCopyCommandListener<TAssetCommand>(EntityRef entity, in TAssetCommand command);
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

    public SimulationFramer SimulationFramer { get; private set; } = null!;

    private ILogger? _logger;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        SimulationFramer = world.GetAddon<SimulationFramer>();

        RuntimeHelpers.RunClassConstructor(typeof(TAsset).TypeHandle);
        world.AcquireAddon<AssetManagerHost<TAsset>>()._managers.Add(this);
    }

    public override void OnUninitialize(World world)
    {
        base.OnUninitialize(world);
        world.GetAddon<AssetManagerHost<TAsset>>()._managers.Remove(this);
    }

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

    protected void Listen<TEvent>(EntityCopyCommandListener<TEvent> listener)
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

    public virtual void AddStates(in EntityRef entity, in TAsset asset, ref DynEntityRef stateEntity) {}
    public virtual CancellationToken? DestroyState(in EntityRef entity, in TAsset asset, EntityRef stateEntity) => null;

    public virtual void LoadAsset(in EntityRef entity, ref TAsset asset, EntityRef stateEntity) {}
    public virtual void UnloadAsset(in EntityRef entity, in TAsset asset, EntityRef stateEntity) {}
}

public abstract class AssetManagerBase<TAsset, TAssetState> : AssetManagerBase<TAsset>
    where TAsset : struct
    where TAssetState : struct
{
    public override void AddStates(in EntityRef entity, in TAsset asset, ref DynEntityRef stateEntity)
        => stateEntity.Add(new TAssetState());
}