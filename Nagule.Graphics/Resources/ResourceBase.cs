namespace Nagule.Graphics;

public abstract record ResourceBase : IResource
{
    public Guid? Id { get; init; }
}