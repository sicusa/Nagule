namespace Nagule.Graphics.Backend.OpenTK;

public struct ActivateMaterialBuiltInBuffersPass : IRenderPass
{
    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        GLHelper.ActivateMaterialBuiltInBuffers(host);
    }
}