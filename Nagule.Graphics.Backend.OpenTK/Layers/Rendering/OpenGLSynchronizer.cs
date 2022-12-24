namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class OpenGLSynchronizer : VirtualLayer, ILoadListener, IUnloadListener, ILateUpdateListener, IRenderListener, IRenderFinishedListener
{
    private GLSync _sync;
    private AutoResetEvent _renderFinishedEvent = new(true);

    public void OnLoad(IContext context)
    {
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public void OnUnload(IContext context)
    {
        _renderFinishedEvent.Set();
    }

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        _renderFinishedEvent.WaitOne();
    }

    public void OnRender(IContext context, float deltaTime)
    {
        SyncStatus status = SyncStatus.WaitFailed;
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied) {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
    }

    public void OnRenderFinished(IContext context, float deltaTime)
    {
        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _renderFinishedEvent.Set();
    }
}