namespace Nagule;

public struct InitializedResource<TResource> : IPooledComponent
    where TResource : IResource
{
    public TResource Value;
    public IDisposable? Subscription;
}