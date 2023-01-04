namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    ILoadListener, IEngineUpdateListener, IRenderListener, IRenderPreparedListener
{
    private AutoResetEvent _renderFinishedEvent = new(true);
    private GLSync _sync;

    public void OnLoad(IContext context)
    {
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public void OnEngineUpdate(IContext context)
    {
        _renderFinishedEvent.WaitOne();
    }

    public void OnRenderPrepared(IContext context)
    {
        SyncStatus status;
        do {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);
    }

    public void OnRender(IContext context)
    {
        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _renderFinishedEvent.Set();
    }
}