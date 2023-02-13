namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultMaterialLoader : Layer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var texture = ref context.Acquire<Resource<Material>>(Graphics.DefaultMaterialId);
        texture.Value = Material.Default;
    }
}