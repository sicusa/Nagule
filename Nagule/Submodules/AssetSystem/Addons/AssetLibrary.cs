namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Sia;

public class AssetLibrary : ViewBase<TypeUnion<AssetMetadata>>
{
    [AllowNull] public ILogger Logger { get; private set; }

    public IReadOnlyDictionary<ObjectKey<IAsset>, EntityRef> Entities => EntitiesRaw;

    public EntityRef this[IAsset record]
        => EntitiesRaw.TryGetValue(new(record), out var entity)
            ? entity : throw new KeyNotFoundException("Asset entity not found");

    internal readonly Dictionary<ObjectKey<IAsset>, EntityRef> EntitiesRaw = [];

    public bool TryGet(IAsset record, out EntityRef entity)
        => EntitiesRaw.TryGetValue(new(record), out entity);

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
            Logger.LogWarning("Destroyed asset [{Entity}] is referred by other assets.",
                entity.GetDisplayName());
            return;
        }
        ref var assetKey = ref entity.Get<Sid<IAsset>>();
        if (assetKey.Value != null) {
            EntitiesRaw.Remove(new(assetKey.Value));
        }
        DestroyAssetRecursively(entity, ref metadata);
    }

    private static void DestroyAssetRecursively(in EntityRef entity, ref AssetMetadata meta)
    {
        foreach (var referred in meta.Referred) {
            entity.UnreferAsset(referred);

            ref var refereeMeta = ref referred.Get<AssetMetadata>();
            if (refereeMeta.AssetLife == AssetLife.Automatic
                    && refereeMeta.Referrers.Count == 0) {
                DestroyAssetRecursively(referred, ref refereeMeta);
                referred.Dispose();
            }
        }
    }
}