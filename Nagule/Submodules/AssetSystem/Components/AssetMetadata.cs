namespace Nagule;

using System.Collections.Immutable;
using Sia;

public record struct AssetMetadata()
{
    public record struct OnReferred(EntityRef Entity) : IEvent;
    public record struct OnUnreferred(EntityRef Entity) : IEvent;

    public AssetId AssetId { get; init; } = new(Interlocked.Increment(ref s_idAcc));

    public required Type AssetType { get; init; }
    public AssetLife AssetLife { get; init; }
    public IAssetRecord? AssetRecord { get; init; }

    public readonly IReadOnlySet<EntityRef> Referrers =>
        (IReadOnlySet<EntityRef>?)_referrers ?? ImmutableHashSet<EntityRef>.Empty;

    public readonly IReadOnlySet<EntityRef> Referred =>
        (IReadOnlySet<EntityRef>?)_referred ?? ImmutableHashSet<EntityRef>.Empty;

    private HashSet<EntityRef>? _referrers;
    private HashSet<EntityRef>? _referred;

    private static int s_idAcc;

    public readonly record struct Refer(EntityRef Asset) : ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());

        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            try {
                ref var referred = ref metadata._referred;
                referred ??= [];
                if (!referred.Add(Asset)) {
                    return;
                }

                ref var referers = ref Asset.Get<AssetMetadata>()._referrers;
                referers ??= [];
                referers.Add(target);

                world.Send(Asset, new OnReferred(Asset));
            }
            catch (Exception e) {
                throw new InvalidAssetException("The asset currently referring is invalid", e);
            }
        }
    }

    public readonly record struct Unrefer(EntityRef Asset) : ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());
            
        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            ref var referred = ref metadata._referred;
            if (referred == null || !referred.Remove(Asset)) {
                return;
            }
            try {
                Asset.Get<AssetMetadata>()._referrers!.Remove(target);
                world.Send(Asset, new OnUnreferred(Asset));
            }
            catch (Exception e) {
                throw new InvalidAssetException("The asset currently unreferring is invalid", e);
            }
        }
    }

    public readonly EntityRef? FindReferrer<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referrers == null) {
            return null;
        }
        var assetType = typeof(TAsset);
        if (recurse) {
            foreach (var referrer in _referrers) {
                var meta = referrer.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    return referrer;
                }
                if (meta._referrers != null) {
                    return meta.FindReferrer<TAsset>(recurse: true);
                }
            }
        }
        else {
            foreach (var referrer in _referrers) {
                if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    return referrer;
                }
            }
        }
        return null;
    }

    public readonly IEnumerable<EntityRef> FindReferrers<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referrers == null) {
            yield break;
        }
        var assetType = typeof(TAsset);
        if (recurse) {
            foreach (var referrer in _referrers) {
                var meta = referrer.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    yield return referrer;
                }
                if (meta._referrers != null) {
                    foreach (var found in meta.FindReferrers<TAsset>(recurse: true)) {
                        yield return found;
                    }
                }
            }
        }
        else {
            foreach (var referrer in _referrers) {
                if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    yield return referrer;
                }
            }
        }
    }

    public readonly EntityRef? FindReferred<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referred == null) {
            return null;
        }
        var assetType = typeof(TAsset);
        if (recurse) {
            foreach (var referee in _referred) {
                var meta = referee.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    return referee;
                }
                if (meta._referred != null) {
                    return meta.FindReferred<TAsset>(recurse: true);
                }
            }
        }
        else {
            foreach (var referred in _referred) {
                if (referred.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    return referred;
                }
            }
        }
        return null;
    }

    public readonly IEnumerable<EntityRef> FindAllReferred<TAsset>(bool recurse = false)
        where TAsset : struct
    {
        if (_referred == null) {
            yield break;
        }
        var assetType = typeof(TAsset);
        if (recurse) {
            foreach (var referee in _referred) {
                var meta = referee.Get<AssetMetadata>();
                if (meta.AssetType.IsAssignableTo(assetType)) {
                    yield return referee;
                }
                if (meta._referred != null) {
                    foreach (var found in meta.FindAllReferred<TAsset>(recurse: true)) {
                        yield return found;
                    }
                }
            }
        }
        else {
            foreach (var referee in _referred) {
                if (referee.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                    yield return referee;
                }
            }
        }
    }
}