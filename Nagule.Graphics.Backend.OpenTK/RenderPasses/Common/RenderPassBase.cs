namespace Nagule.Graphics.Backend.OpenTK;

public abstract class RenderPassBase : IRenderPass
{
    protected Guid Id { get; } = Guid.NewGuid();

    public virtual void LoadResources(IContext context) {}

    public void UnloadResources(IContext context)
        => ResourceLibrary.UnreferenceAll(context, Id);

    public virtual void Initialize(ICommandHost host, IRenderPipeline pipeline) {}
    public virtual void Uninitialize(ICommandHost host, IRenderPipeline pipeline) {}
    public abstract void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup);
}