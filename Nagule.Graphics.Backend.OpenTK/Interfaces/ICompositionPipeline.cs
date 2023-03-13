namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public interface ICompositionPipeline : IDataLayer<IComponent>
{
    IReadOnlyList<ICompositionPass> Passes { get; }
    uint RenderSettingsId { get; }
    Material Material { get; }
    uint MaterialId { get; }

    int ViewportWidth { get; }
    int ViewportHeight { get; }

    void LoadResources(IContext context);
    void UnloadResources(IContext context);
    void Initialize(ICommandHost host);
    void Uninitialize(ICommandHost host);
    void Execute(ICommandHost host, uint cameraId, IRenderPipeline renderPipeline);

    void Blit(ICommandHost host);
    void SetViewportSize(int width, int height);
}