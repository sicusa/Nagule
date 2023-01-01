namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer, IUnloadListener, ILateUpdateListener, IRenderFinishedListener
{
    private AutoResetEvent _renderFinishedEvent = new(true);

    public void OnUnload(IContext context)
    {
        _renderFinishedEvent.Set();
    }

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        _renderFinishedEvent.WaitOne();
    }

    public void OnRenderFinished(IContext context, float deltaTime)
    {
        _renderFinishedEvent.Set();
    }
}