namespace Nagule;

using Sia;

public readonly record struct AssetMetadata()
{
    private static long s_idAcc;

    public record struct OnReferred(EntityRef Entity) : IEvent;
    public record struct OnUnreferred(EntityRef Entity) : IEvent;

    public long Id { get; init; } = Interlocked.Increment(ref s_idAcc);
    public required Type AssetType { get; init; }
    public AssetLife AssetLife { get; init; }

    public readonly IReadOnlySet<EntityRef> Referrers => _referrers;
    public readonly IReadOnlySet<EntityRef> Referred => _referred;

    private readonly HashSet<EntityRef> _referrers = [];
    private readonly HashSet<EntityRef> _referred = [];

    public readonly record struct Refer(EntityRef Asset) : ICommand, ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());

        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            try {
                if (!metadata._referred.Add(Asset)) {
                    return;
                }
                Asset.Get<AssetMetadata>()._referrers.Add(target);
                world.Send(Asset, new OnReferred(Asset));
            }
            catch {
                throw new InvalidAssetException("The asset currently referring is invalid");
            }
        }
    }

    public readonly record struct Unrefer(EntityRef Asset) : ICommand, ICommand<AssetMetadata>
    {
        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<AssetMetadata>());
            
        public void Execute(World world, in EntityRef target, ref AssetMetadata metadata)
        {
            try {
                if (!metadata._referred.Remove(Asset)) {
                    return;
                }
                Asset.Get<AssetMetadata>()._referrers.Remove(target);
                world.Send(Asset, new OnUnreferred(Asset));
            }
            catch {
                throw new InvalidAssetException("The asset currently unreferring is invalid");
            }
        }
    }

    public EntityRef? FindReferrer<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referrer in _referrers) {
            if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                return referrer;
            }
        }
        return null;
    }

    public EntityRef? FindReferrerRecursively<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referrer in _referrers) {
            var meta = referrer.Get<AssetMetadata>();
            if (meta.AssetType.IsAssignableTo(assetType)) {
                return referrer;
            }
            if (meta._referrers != null) {
                return meta.FindReferrerRecursively<TAsset>();
            }
        }
        return null;
    }

    public IEnumerable<EntityRef> FindReferrers<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referrer in _referrers) {
            if (referrer.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                yield return referrer;
            }
        }
    }

    public IEnumerable<EntityRef> FindReferrersRecursively<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referrer in _referrers) {
            var meta = referrer.Get<AssetMetadata>();
            if (meta.AssetType.IsAssignableTo(assetType)) {
                yield return referrer;
            }
            if (meta._referrers != null) {
                foreach (var found in meta.FindReferrersRecursively<TAsset>()) {
                    yield return found;
                }
            }
        }
    }

    public EntityRef? FindReferred<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referred in _referred) {
            if (referred.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                return referred;
            }
        }
        return null;
    }

    public EntityRef? FindReferredRecursively<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referee in _referred) {
            var meta = referee.Get<AssetMetadata>();
            if (meta.AssetType.IsAssignableTo(assetType)) {
                return referee;
            }
            if (meta._referred != null) {
                return meta.FindReferredRecursively<TAsset>();
            }
        }
        return null;
    }

    public IEnumerable<EntityRef> FindAllReferred<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referee in _referred) {
            if (referee.Get<AssetMetadata>().AssetType.IsAssignableTo(assetType)) {
                yield return referee;
            }
        }
    }

    public IEnumerable<EntityRef> FindAllReferredRecursively<TAsset>()
        where TAsset : IAsset
    {
        var assetType = typeof(TAsset);
        foreach (var referee in _referred) {
            var meta = referee.Get<AssetMetadata>();
            if (meta.AssetType.IsAssignableTo(assetType)) {
                yield return referee;
            }
            if (meta._referred != null) {
                foreach (var found in meta.FindAllReferredRecursively<TAsset>()) {
                    yield return found;
                }
            }
        }
    }
}