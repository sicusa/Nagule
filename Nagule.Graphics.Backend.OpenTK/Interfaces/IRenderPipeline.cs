namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public interface IRenderPipeline : IDataLayer<IComponent>
{
    IReadOnlyList<IRenderPass> Passes { get; }
    uint RenderSettingsId { get; }

    int Width { get; }
    int Height { get; }

    FramebufferHandle FramebufferHandle { get; }
    BufferHandle UniformBufferHandle { get; }

    TextureHandle? ColorTextureHandle { get; }
    TextureHandle? DepthTextureHandle { get; }

    event Action<ICommandHost, IRenderPipeline>? OnResize;

    void LoadResources(IContext context);
    void UnloadResources(IContext context);
    void Initialize(ICommandHost host);
    void Uninitialize(ICommandHost host);
    void Execute(ICommandHost host, uint CameraId, MeshGroup meshGroup);
    void Resize(ICommandHost host, int width, int height);

    TextureHandle EnsureColorTexture();
    TextureHandle EnsureDepthTexture();
}