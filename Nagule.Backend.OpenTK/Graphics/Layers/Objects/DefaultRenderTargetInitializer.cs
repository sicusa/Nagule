namespace Nagule.Backend.OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public class DefaultRenderTargetLoader : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var renderTarget = ref context.Acquire<RenderTarget>(Graphics.DefaultRenderTargetId);
        renderTarget.Resource = RenderTargetResource.AutoResized;
        Console.WriteLine("Default render target initialized: " + Graphics.DefaultRenderTargetId);
    }
}