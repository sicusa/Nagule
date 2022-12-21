namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultRenderTargetInitializer : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
        ref var renderTarget = ref context.Acquire<Resource<RenderTarget>>(Graphics.DefaultRenderTargetId);
        renderTarget.Value = spec.IsResizable
            ? RenderTarget.AutoResized
            : new RenderTarget {
                Width = spec.Width,
                Height = spec.Height
            };
        Console.WriteLine("Default render target initialized: " + Graphics.DefaultRenderTargetId);
    }
}