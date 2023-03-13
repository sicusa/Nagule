namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public abstract class CompositionPassImplBase : ICompositionPass
{
    public virtual IEnumerable<MaterialProperty> Properties { get; }
        = Enumerable.Empty<MaterialProperty>();

    public virtual string? EntryPoint { get; }
    public virtual string? Source { get; }

    protected uint Id { get; } = IdFactory.New();

    public virtual void LoadResources(IContext context) {}
    public virtual void UnloadResources(IContext context)
        => context.GetResourceLibrary().UnreferenceAll(Id);

    public virtual void Uninitialize(ICommandHost host, ICompositionPipeline pipeline) {}
    public virtual void Initialize(ICommandHost host, ICompositionPipeline pipeline) {}
}