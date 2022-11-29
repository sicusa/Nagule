namespace Nagule;

public interface IResourceObject<TResource> : IReactiveComponent
    where TResource : IResource
{
    TResource Resource { get; set; }
}