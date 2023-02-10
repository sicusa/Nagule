namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

using Aeco;

public interface IRenderPipeline : IDataLayer<IComponent>
{
    public Guid RenderSettingsId { get; }

    public int Width { get; }
    public int Height { get; }

    public FramebufferHandle FramebufferHandle { get; }
    public BufferHandle UniformBufferHandle { get; }

    public TextureHandle ColorTextureHandle { get; }
    public TextureHandle DepthTextureHandle { get; }

    public IReadOnlyList<IRenderPass> RenderPasses { get; }

    public event Action<ICommandHost, IRenderPipeline>? OnResize;

    public void Initialize(ICommandHost host);
    public void Uninitialize(ICommandHost host);
    public void Render(ICommandHost host, MeshGroup meshGroup);
    public void Resize(ICommandHost host, int width, int height);
}