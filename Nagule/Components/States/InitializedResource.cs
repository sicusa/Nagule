namespace Nagule;

public struct InitializedResource<TResource> : IHashComponent
    where TResource : IResource
{
    public TResource Value;
    public IDisposable? Subscription;
}