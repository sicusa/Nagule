namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;
using Aeco.Reactive;

public class CameraGroup : Group<CameraData>
{
    public override void Refresh(IReadableDataLayer<IComponent> dataLayer)
    {
        Reset(dataLayer, dataLayer.Query<CameraData>()
            .OrderBy(id => dataLayer.Inspect<CameraData>(id).Depth));
    }
}