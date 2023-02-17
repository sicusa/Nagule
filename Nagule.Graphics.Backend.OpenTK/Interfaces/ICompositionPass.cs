namespace Nagule.Graphics.Backend.OpenTK;

public interface ICompositionPass
{
    IEnumerable<MaterialProperty> Properties { get; }

    string? EntryPoint { get; }
    string? Source { get; }

    void LoadResources(IContext context);
    void UnloadResources(IContext context);
    void Initialize(ICommandHost host, ICompositionPipeline pipeline);
    void Uninitialize(ICommandHost host, ICompositionPipeline pipeline);
}

public interface IExecutableCompositionPass : ICompositionPass
{
    void Execute(ICommandHost host, ICompositionPipeline pipeline, IRenderPipeline renderPipeline);
}