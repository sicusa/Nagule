namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    IEngineUpdateListener, IRenderListener
{
    private AutoResetEvent _renderFinishedEvent = new(false);

    public void OnEngineUpdate(IContext context)
    {
        context.SendCommand<RenderTarget>(FinishFrameCommand.Instance);
        _renderFinishedEvent.WaitOne();
    }

    public void OnRender(IContext context)
    {
        _renderFinishedEvent.Set();
    }
}