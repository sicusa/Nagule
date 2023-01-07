namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    IEngineUpdateListener, IRenderListener, IUnloadListener
{
    private AutoResetEvent _renderFinishedEvent = new(false);

    public void OnEngineUpdate(IContext context)
    {
        _renderFinishedEvent.WaitOne();
    }

    public void OnRender(IContext context)
    {
        _renderFinishedEvent.Set();
    }

    public void OnUnload(IContext context)
    {
        _renderFinishedEvent.Set();
    }
}