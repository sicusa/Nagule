namespace Nagule;

using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public sealed class AssetManagerHost<TAsset> : ViewBase<TypeUnion<TAsset>>
{
    public IReadOnlyList<IAssetManagerBase<TAsset>> Managers => _managers;

    internal readonly List<IAssetManagerBase<TAsset>> _managers = [];

    protected override void OnEntityAdded(in EntityRef entity)
    {
        ref var asset = ref entity.Get<TAsset>();
        ref var state = ref entity.Get<AssetState>();

        ref var stateEntity = ref state.Entity;
        stateEntity.Add(new AssetSnapshot<TAsset>(asset));

        for (int i = 0; i < _managers.Count; ++i) {
            _managers[i].AddStates(entity, asset, ref stateEntity);
        }

        state.IsLocked = true;
        var currStateEntity = stateEntity.Current;

        for (int i = 0; i < _managers.Count; ++i) {
            _managers[i].LoadAsset(entity, ref asset, currStateEntity);
        }
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var asset = ref entity.Get<TAsset>();
        ref var state = ref entity.Get<AssetState>();
        var stateEntity = state.Entity.Current;

        for (int i = 0; i < _managers.Count; ++i) {
            _managers[i].UnloadAsset(entity, asset, stateEntity);
        }

        var tokens = SpanOwner<CancellationToken>.Allocate(0);
        var tokenSpan = tokens.Span;
        int tokenCount = 0;

        try {
            for (int i = 0; i < _managers.Count; ++i) {
                var token = _managers[i].DestroyState(entity, asset, stateEntity);
                if (token != null) {
                    if (tokens.Length == 0) {
                        tokens = SpanOwner<CancellationToken>.Allocate(_managers.Count);
                        tokenSpan = tokens.Span;
                    }
                    tokens.Span[tokenCount] = token.Value;
                    tokenCount++;
                }
            }

            if (tokenCount == 0) {
                stateEntity.Dispose();
            }
            else if (tokenCount == 1) {
                stateEntity.Hang(e => e.Dispose(), tokenSpan[0]);
            }
            else {
                var source = CancellationTokenSource.CreateLinkedTokenSource(tokenSpan[..tokenCount].ToArray());
                stateEntity.Hang(e => e.Dispose(), source.Token);
            }
        }
        finally {
            tokens.Dispose();
        }
    }
}