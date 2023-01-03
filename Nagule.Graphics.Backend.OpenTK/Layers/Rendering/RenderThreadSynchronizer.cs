namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class RenderThreadSynchronizer : VirtualLayer,
    ILoadListener, IUnloadListener, IFrameStartListener, IEngineUpdateListener, IRenderListener, IRenderPreparedListener
{
    private AutoResetEvent? _frameStartEvent = new(true);
    private AutoResetEvent? _renderFinishedEvent = new(true);
    private GLSync _sync;

    public void OnLoad(IContext context)
    {
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public void OnUnload(IContext context)
    {
        _frameStartEvent!.Set();
        _renderFinishedEvent!.Set();

        _frameStartEvent = null;
        _renderFinishedEvent = null;
    }

    public void OnFrameStart(IContext context, float deltaTime)
    {
        _frameStartEvent?.Set();
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        _renderFinishedEvent?.WaitOne();
    }

    public void OnRenderPrepared(IContext context, float deltaTime)
    {
        _frameStartEvent?.WaitOne();

        SyncStatus status;
        do {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);
    }

    public void OnRender(IContext context, float deltaTime)
    {
        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _renderFinishedEvent?.Set();
    }
}