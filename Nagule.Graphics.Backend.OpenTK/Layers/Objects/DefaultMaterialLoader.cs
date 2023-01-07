namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultMaterialLoader : VirtualLayer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var texture = ref context.Acquire<Resource<Material>>(Graphics.DefaultTextureId);
        texture.Value = Material.Default;
    }
}