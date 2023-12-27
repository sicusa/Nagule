namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Sia;

public class AssetLibrary : ViewBase<TypeUnion<AssetMetadata>>
{
    [AllowNull] public ILogger Logger { get; private set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        Logger = world.CreateLogger<AssetLibrary>();
    }

    protected override void OnEntityAdded(in EntityRef entity) {}

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var metadata = ref entity.Get<AssetMetadata>();
        if (metadata.Referrers.Count != 0) {
            Logger.LogWarning("Destroyed asset [{Entity}] is refered by other assets.",
                entity.Get<Sid<Guid>>().Value);
            return;
        }
        DestroyAssetRecursively(entity, ref metadata);
    }

    private void DestroyAssetRecursively(in EntityRef entity, ref AssetMetadata meta)
    {
        foreach (var referee in meta.Referred) {
            entity.Modify(ref meta, new AssetMetadata.Unrefer(referee));

            ref var refereeMeta = ref referee.Get<AssetMetadata>();
            if (refereeMeta.AssetLife == AssetLife.Automatic
                    && refereeMeta.Referrers.Count == 0) {
                DestroyAssetRecursively(referee, ref refereeMeta);
                referee.Destroy();
            }
        }
    }
}