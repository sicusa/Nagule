namespace Nagule.Graphics.Backend.OpenTK;

public abstract class RenderPassBase : IRenderPass
{
    protected Guid Id { get; } = Guid.NewGuid();

    public virtual void LoadResources(IContext context) {}

    public virtual void UnloadResources(IContext context)
        => context.GetResourceLibrary().UnreferenceAll(Id);

    public virtual void Initialize(ICommandHost host, IRenderPipeline pipeline) {}
    public virtual void Uninitialize(ICommandHost host, IRenderPipeline pipeline) {}

    public abstract void Execute(ICommandHost host, IRenderPipeline pipeline, Guid cameraId, MeshGroup meshGroup);
}