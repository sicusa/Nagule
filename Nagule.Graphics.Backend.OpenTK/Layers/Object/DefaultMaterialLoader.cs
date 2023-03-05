namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class DefaultMaterialLoader : Layer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        ref var material = ref context.Acquire<Resource<Material>>(Graphics.DefaultMaterialId);
        material.Value = Material.Default;
    }
}