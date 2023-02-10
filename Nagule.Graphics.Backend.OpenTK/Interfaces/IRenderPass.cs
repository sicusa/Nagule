namespace Nagule.Graphics.Backend.OpenTK;

public interface IRenderPass
{
    void Initialize(ICommandHost host, IRenderPipeline pipeline);
    void Uninitialize(ICommandHost host, IRenderPipeline pipeline);
    void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup);
}