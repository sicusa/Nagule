namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    IUnloadListener, IEngineUpdateListener, ILateUpdateListener, IRenderListener, IRenderFinishedListener
{
    private AutoResetEvent _updateFinishedEvent = new(true);
    private AutoResetEvent _renderFinishedEvent = new(true);

    public void OnUnload(IContext context)
    {
        _updateFinishedEvent.Set();
        _renderFinishedEvent.Set();
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        _updateFinishedEvent.Set();
    }

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        _renderFinishedEvent.WaitOne();
    }

    public void OnRender(IContext context, float deltaTime)
    {
        _updateFinishedEvent.WaitOne();
    }

    public void OnRenderFinished(IContext context, float deltaTime)
    {
        _renderFinishedEvent.Set();
    }
}