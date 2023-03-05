namespace Nagule.Graphics.Backend.OpenTK;

public interface IRenderPass
{
    void LoadResources(IContext context);
    void UnloadResources(IContext context);
    void Initialize(ICommandHost host, IRenderPipeline pipeline);
    void Uninitialize(ICommandHost host, IRenderPipeline pipeline);
    void Execute(ICommandHost host, IRenderPipeline pipeline, Guid cameraId, MeshGroup meshGroup);
}