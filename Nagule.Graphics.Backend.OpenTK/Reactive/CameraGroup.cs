namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;
using Aeco.Reactive;

public class CameraGroup : Group<Resource<Camera>>
{
    public override void Refresh(IReadableDataLayer<IComponent> dataLayer)
    {
        Reset(dataLayer, dataLayer.Query<Resource<Camera>>()
            .OrderBy(id => dataLayer.Inspect<Resource<Camera>>(id).Value.Depth));
    }
}