namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Windowing.GraphicsLibraryFramework;

using Aeco;

public class RenderCommandExecutor
    : VirtualLayer, ILoadListener, IRenderListener, IUnloadListener
{
    private class WorkerTarget0 : ICommandTarget { }
    private class WorkerTarget1 : ICommandTarget { }
    private class WorkerTarget2 : ICommandTarget { }
    private class WorkerTarget3 : ICommandTarget { }
    private class WorkerTarget4 : ICommandTarget { }
    private class WorkerTarget5 : ICommandTarget { }
    private class WorkerTarget6 : ICommandTarget { }
    private class WorkerTarget7 : ICommandTarget { }

    private class SyncWorkerCommand : SingletonCommand<SyncWorkerCommand> {}
    private class StopWorkerCommand : SingletonCommand<StopWorkerCommand> {}

    [AllowNull] private IEnumerable<ICommand> _renderCommands;

    private GLSync _sync;
    private int _lastWorkerId;
    private int _workerCount;

    private volatile int _synchronizedWorkerCount;

    public void OnLoad(IContext context)
    {
        _renderCommands = context.ConsumeCommands<RenderTarget>();
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        _workerCount = 4;

        CreateWorkerThread<WorkerTarget0>(context);
        CreateWorkerThread<WorkerTarget1>(context);
        CreateWorkerThread<WorkerTarget2>(context);
        CreateWorkerThread<WorkerTarget3>(context);
    }

    public void OnUnload(IContext context)
    {
        for (int i = 0; i < _workerCount; ++i) {
            SendCommandToWorker(context, i, StopWorkerCommand.Instance);
        }
    }

    public void OnRender(IContext context)
    {
        SyncStatus status;
        do {
            status = GL.ClientWaitSync(_sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);

        void ExecuteCommand(ICommand command)
        {
            switch (command) {
            case AsynchronizedCommand asyncCmd:
                _lastWorkerId = ++_lastWorkerId % _workerCount;
                SendCommandToWorker(context, _lastWorkerId, asyncCmd.Inner!);
                asyncCmd.Dispose();
                break;

            case BatchedCommand batchedCmd:
                batchedCmd.Commands.ForEach(ExecuteCommand);
                batchedCmd.Dispose();
                break;
            
            default:
                command.SafeExecuteAndDispose(context);
                break;
            }
        }

        foreach (var command in _renderCommands) {
            if (command is FinishFrameCommand) {
                break;
            }
            ExecuteCommand(command);
        }

        for (int i = 0; i < _workerCount; ++i) {
            SendCommandToWorker(context, i, SyncWorkerCommand.Instance);
        }

        SpinWait.SpinUntil(IsAllWorkersSynchronized);
        _synchronizedWorkerCount = 0;

        GL.DeleteSync(_sync);
        _sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
    }

    private unsafe void CreateWorkerThread<TCommandTarget>(IContext context)
        where TCommandTarget : ICommandTarget
    {
        GLFW.WindowHint(WindowHintBool.Visible, false);
        var currentContext = GLFW.GetCurrentContext();
        var workerContext = GLFW.CreateWindow(1, 1, "", null, currentContext);
        var commands = context.ConsumeCommands<TCommandTarget>();

        void ExecuteCommand(ICommand command)
        {
            switch (command) {
            case SyncWorkerCommand:
                Interlocked.Increment(ref _synchronizedWorkerCount);
                break;
            
            case BatchedCommand batchedCmd:
                batchedCmd.Commands.ForEach(ExecuteCommand);
                batchedCmd.Dispose();
                break;
            
            default:
                command.SafeExecuteAndDispose(context);
                break;
            }
        }

        new Thread(() => {
            GLFW.MakeContextCurrent(workerContext);
            foreach (var command in commands) {
                if (command is StopWorkerCommand) {
                    Interlocked.Increment(ref _synchronizedWorkerCount);
                    GLFW.DestroyWindow(workerContext);
                    break;
                }
                ExecuteCommand(command);
            }
        }).Start();
    }

    private bool IsAllWorkersSynchronized()
      => _synchronizedWorkerCount == _workerCount;

    private void SendCommandToWorker(IContext context, int workerId, ICommand command)
    {
        switch (workerId) {
            case 0: context.SendCommand<WorkerTarget0>(command); break;
            case 1: context.SendCommand<WorkerTarget1>(command); break;
            case 2: context.SendCommand<WorkerTarget2>(command); break;
            case 3: context.SendCommand<WorkerTarget3>(command); break;
            case 4: context.SendCommand<WorkerTarget4>(command); break;
            case 5: context.SendCommand<WorkerTarget5>(command); break;
            case 6: context.SendCommand<WorkerTarget6>(command); break;
            case 7: context.SendCommand<WorkerTarget7>(command); break;
            default: throw new Exception("Invalid worker ID");
        }
    }
}