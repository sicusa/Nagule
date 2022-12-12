namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public class DefaultRenderTargetLoader : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
        ref var renderTarget = ref context.Acquire<RenderTarget>(Graphics.DefaultRenderTargetId);
        renderTarget.Resource = spec.IsResizable
            ? RenderTargetResource.AutoResized
            : new RenderTargetResource {
                Width = spec.Width,
                Height = spec.Height
            };
        Console.WriteLine("Default render target initialized: " + Graphics.DefaultRenderTargetId);
    }
}