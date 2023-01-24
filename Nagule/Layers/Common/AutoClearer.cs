namespace Nagule;

using Aeco;

public class AutoClearer : Layer, ILateUpdateListener
{
    private IShrinkableDataLayer<IComponent> _dataLayer;

    public AutoClearer(IShrinkableDataLayer<IComponent> dataLayer)
    {
        _dataLayer = dataLayer;
    }

    public void OnLateUpdate(IContext context)
    {
        _dataLayer.Clear();
    }
}