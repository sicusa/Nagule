namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultRenderPipelineInitializer : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
        ResourceLibrary<RenderPipeline>.Reference(context,
            spec.IsResizable
                ? RenderPipeline.AutoResized with { Id = Graphics.DefaultRenderPipelineId }
                : new RenderPipeline {
                    Width = spec.Width,
                    Height = spec.Height,
                    Id = Graphics.DefaultRenderPipelineId
                },
            Graphics.RootId);
        Console.WriteLine("Default render target initialized: " + Graphics.DefaultRenderPipelineId);
    }
}