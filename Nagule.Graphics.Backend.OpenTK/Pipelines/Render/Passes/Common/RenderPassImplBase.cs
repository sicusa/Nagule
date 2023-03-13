namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public abstract class RenderPassImplBase : IRenderPass
{
    protected uint Id { get; } = IdFactory.New();

    public virtual void LoadResources(IContext context) {}

    public virtual void UnloadResources(IContext context)
        => context.GetResourceLibrary().UnreferenceAll(Id);

    public virtual void Initialize(ICommandHost host, IRenderPipeline pipeline) {}
    public virtual void Uninitialize(ICommandHost host, IRenderPipeline pipeline) {}

    public abstract void Execute(ICommandHost host, IRenderPipeline pipeline, uint cameraId, MeshGroup meshGroup);
}