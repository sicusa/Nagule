namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    ILoadListener, IUnloadListener, IEngineUpdateListener, ILateUpdateListener, IRenderListener, IRenderFinishedListener
{
    private AutoResetEvent? _updateFinishedEvent = new(true);
    private AutoResetEvent? _renderFinishedEvent = new(true);
    private GLSync _sync;

    public void OnLoad(IContext context)
    {
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public void OnUnload(IContext context)
    {
        _updateFinishedEvent!.Set();
        _renderFinishedEvent!.Set();

        _updateFinishedEvent = null;
        _renderFinishedEvent = null;
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        _updateFinishedEvent?.Set();
    }

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        _renderFinishedEvent?.WaitOne();
    }

    public void OnRender(IContext context, float deltaTime)
    {
        _updateFinishedEvent?.WaitOne();

        SyncStatus status = SyncStatus.WaitFailed;
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied) {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
    }

    public void OnRenderFinished(IContext context, float deltaTime)
    {
        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _renderFinishedEvent?.Set();
    }
}