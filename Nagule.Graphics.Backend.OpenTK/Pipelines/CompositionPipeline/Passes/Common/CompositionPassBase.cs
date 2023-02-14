namespace Nagule.Graphics.Backend.OpenTK;

public abstract class CompositionPassBase : ICompositionPass
{
    public virtual IEnumerable<MaterialProperty> Properties { get; }
        = Enumerable.Empty<MaterialProperty>();

    public virtual string? EntryPoint { get; }
    public virtual string? Source { get; }

    protected Guid Id { get; } = Guid.NewGuid();

    public virtual void LoadResources(IContext context) {}
    public virtual void UnloadResources(IContext context)
        => ResourceLibrary.UnreferenceAll(context, Id);

    public virtual void Uninitialize(ICommandHost host, ICompositionPipeline pipeline) {}
    public virtual void Initialize(ICommandHost host, ICompositionPipeline pipeline) {}
    public virtual void Execute(ICommandHost host, ICompositionPipeline pipeline, IRenderPipeline renderPipeline) {}
}