namespace Nagule;

using Sia;

public partial record struct Feature
{
    public sealed class OnIsEnabledChanged : SingletonEvent<OnIsEnabledChanged>;
    public readonly record struct OnNodeTransformChanged(EntityRef Node) : IEvent;
    public readonly record struct OnNodeLayerChanged(EntityRef Node, Layer Layer) : IEvent;

    public EntityRef Node { get; private set; }
    public readonly bool IsEnabled => IsSelfEnabled && Node.Get<NodeHierarchy>().IsEnabled;

    [SiaProperty]
    public bool IsSelfEnabled {
        readonly get => _isSelfEnabled;
        set {
            if (_isSelfEnabled == value) {
                return;
            }
            _isSelfEnabled = value;

            if (Node.Get<NodeHierarchy>().IsEnabled) {
                _self.Send(OnIsEnabledChanged.Instance);
            }
        }
    }

    internal EntityRef _self;
    private bool _isSelfEnabled;

    internal Feature(EntityRef node, bool enabled)
    {
        Node = node;
        IsSelfEnabled = enabled;
    }
}