namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public class RenderCommandExecutor
    : VirtualLayer, ILoadListener, IRenderListener
{
    [AllowNull] private IEnumerable<ICommand> _renderCommands;
    private GLSync _sync;

    public void OnLoad(IContext context)
    {
        _renderCommands = context.ConsumeCommands<RenderTarget>();
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    public void OnRender(IContext context)
    {
        SyncStatus status;
        do {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);

        foreach (var command in _renderCommands) {
            if (command is FinishFrameCommand) {
                break;
            }
            try {
                command.Execute(context);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to execute render command {command.GetType()}: " + e);
            }
            finally {
                command.Dispose();
            }
        }

        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }
}