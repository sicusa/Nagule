namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public interface ICompositionPipeline : IDataLayer<IComponent>
{
    IReadOnlyList<ICompositionPass> Passes { get; }
    Guid RenderSettingsId { get; }
    Material Material { get; }
    Guid MaterialId { get; }

    int Width { get; }
    int Height { get; }

    event Action<ICommandHost, ICompositionPipeline>? OnResize;

    void LoadResources(IContext context);
    void UnloadResources(IContext context);
    void Initialize(ICommandHost host);
    void Uninitialize(ICommandHost host);
    void Execute(ICommandHost host, IRenderPipeline renderPipeline);
    void Resize(ICommandHost host, int width, int height);
}