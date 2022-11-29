namespace Nagule;

using Aeco;
using Aeco.Local;

public class AutoClearCompositeLayer : CompositeLayer, ILateUpdateListener
{
    public AutoClearCompositeLayer(params ILayer<IComponent>[] sublayers)
        : base(sublayers)
    {
    }

    public void OnLateUpdate(IContext context, float deltaTime)
        => Clear();
}