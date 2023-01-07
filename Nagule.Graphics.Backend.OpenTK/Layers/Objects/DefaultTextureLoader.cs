namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultTextureLoader : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var texture = ref context.Acquire<Resource<Texture>>(Graphics.DefaultTextureId);
        texture.Value = Texture.White;
    }
}