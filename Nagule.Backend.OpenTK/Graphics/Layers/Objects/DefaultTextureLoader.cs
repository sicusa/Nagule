namespace Nagule.Backend.OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public class DefaultTextureLoader : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var texture = ref context.Acquire<Texture>(Graphics.DefaultTextureId);
        texture.Resource = TextureResource.White;
        Console.WriteLine("Default texture loaded: " + Graphics.DefaultTextureId);
    }
}