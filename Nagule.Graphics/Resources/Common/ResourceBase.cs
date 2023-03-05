namespace Nagule.Graphics;

using Aeco;

public abstract record ResourceBase : IResource
{
    public Guid? Id { get; init; }
    public string Name { get; set; } = "";
}

public abstract record ResourceBase<TProperties> : ResourceBase
    where TProperties : IComponent, new()
{
    public static ref TProperties GetProps(IContext context, Guid id)
        => ref context.Acquire<TProperties>(id);
}