namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public partial struct NodeHierarchy()
{
    public record struct OnIsEnabledChanged(bool Value) : IEvent;
    public record struct OnChildAdded(EntityRef Child) : IEvent;
    public record struct OnChildRemoved(EntityRef Child) : IEvent;

    public bool IsEnabled { get; internal set; }

    [SiaProperty]
    public EntityRef? Parent {
        readonly get => _parent;
        set {
            if (_parent.HasValue) {
                var parent = _parent.Value;
                ref var info = ref parent.GetOrNullRef<NodeHierarchy>();
                if (!Unsafe.IsNullRef(ref info) && (info._children ??= []).Remove(_self)) {
                    Context<World>.Current!.Send(parent, new OnChildRemoved(_self));
                }
            }

            _parent = value;

            if (_parent.HasValue) {
                var parent = _parent.Value;
                ref var info = ref parent.GetOrNullRef<NodeHierarchy>();
                if (!Unsafe.IsNullRef(ref info) && (info._children ??= []).Add(_self)) {
                    Context<World>.Current!.Send(parent, new OnChildAdded(_self));
                }
            }
        }
    }

    public readonly IReadOnlySet<EntityRef> Children => _children ?? s_emptyChildren;

    internal EntityRef _self;

    private EntityRef? _parent;
    private HashSet<EntityRef>? _children;

    private static readonly HashSet<EntityRef> s_emptyChildren = [];

    public readonly HashSet<EntityRef>.Enumerator GetEnumerator()
        => (_children ?? s_emptyChildren).GetEnumerator();
}