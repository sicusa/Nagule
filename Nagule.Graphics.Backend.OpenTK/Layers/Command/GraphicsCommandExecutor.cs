namespace Nagule.Graphics.Backend.OpenTK;

using System.Reactive.Disposables;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Windowing.GraphicsLibraryFramework;

using Aeco;

public unsafe class GraphicsCommandExecutor
    : Layer, ILoadListener, IUnloadListener
{
    private class ResourceWorkerTarget0 : ICommandTarget {}
    private class ResourceWorkerTarget1 : ICommandTarget {}
    private class ResourceWorkerTarget2 : ICommandTarget {}
    private class ResourceWorkerTarget3 : ICommandTarget {}

    private class SwapBuffersCommand : SingletonCommand<SwapBuffersCommand> {}
    private class SynchronizeCommand : SingletonCommand<SynchronizeCommand> {}
    private class StopCommand : SingletonCommand<StopCommand> {}

    [AllowNull] private IEnumerable<ICommand> _commands;
    [AllowNull] private ICommandHost _renderHost;

    private GLSync _sync;
    private IDisposable? _threadsDisposable;
    private Window* _mainWindow;

    private CommandRecorder _compositionCommandRecorder = new("CompositionCommands");

    public void OnLoad(IContext context)
    {
        _mainWindow = GLFW.GetCurrentContext();
        _commands = context.ConsumeCommands<CompositionTarget>();
        _renderHost = new CommandHost(context);

        _threadsDisposable = new CompositeDisposable(
            CreateRenderCommandThread<RenderTarget>(context, _renderHost),

            CreateCommandDispatcherThread<GraphicsResourceTarget>(context, (cmd, counter) => {
                switch (counter % 4) {
                    case 0: context.SendCommand<ResourceWorkerTarget0>(cmd); break;
                    case 1: context.SendCommand<ResourceWorkerTarget1>(cmd); break;
                    case 2: context.SendCommand<ResourceWorkerTarget2>(cmd); break;
                    case 3: context.SendCommand<ResourceWorkerTarget3>(cmd); break;
                }
            }),

            CreateResourceCommandThread<ResourceWorkerTarget0>(context),
            CreateResourceCommandThread<ResourceWorkerTarget1>(context),
            CreateResourceCommandThread<ResourceWorkerTarget2>(context),
            CreateResourceCommandThread<ResourceWorkerTarget3>(context));
    }

    public void OnUnload(IContext context)
    {
        _threadsDisposable?.Dispose();
        _threadsDisposable = null;
    }

    public void Execute(ICommandBus commandBus)
    {
        commandBus.SendCommand<RenderTarget>(SynchronizeCommand.Instance);

        foreach (var command in _commands) {
            if (command is SynchronizeCommand) {
                break;
            }
            _compositionCommandRecorder.Record(command);
        }
        if (_compositionCommandRecorder.Count != 0) {
            GLHelper.WaitSync(_sync);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _compositionCommandRecorder.Execute(_renderHost);
            GLFW.SwapBuffers(_mainWindow);
        }
    }

    private unsafe IDisposable CreateCommandDispatcherThread<TCommandTarget>(IContext context, Action<ICommand, int> dispatch)
        where TCommandTarget : ICommandTarget
    {
        var commands = context.ConsumeCommands<TCommandTarget>();
        int commandCounter = 0;

        var thread = new Thread(() => {
            foreach (var command in commands) {
                if (command is StopCommand) {
                    break;
                }
                dispatch(command, ++commandCounter);
            }
        });

        thread.Name = typeof(TCommandTarget).Name;
        thread.Start();

        return Disposable.Create(() =>
            context.SendCommand<TCommandTarget>(StopCommand.Instance));
    }

    private unsafe IDisposable CreateRenderCommandThread<TCommandTarget>(IContext context, ICommandHost renderContext)
        where TCommandTarget : ICommandTarget
    {
        GLFW.WindowHint(WindowHintBool.Visible, false);
        var glfwContext = GLFW.CreateWindow(1, 1, "", null, _mainWindow);

        var commands = context.ConsumeCommands<TCommandTarget>();
        var spec = context.RequireAny<GraphicsSpecification>();
        var commandRecorder = new CommandRecorder("RenderCommands");

        var thread = new Thread(() => {
            GLFW.MakeContextCurrent(glfwContext);

            var clearColor = spec.ClearColor;
            GL.ClearDepth(1f);
            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);

            foreach (var command in commands) {
                if (command is StopCommand) {
                    break;
                }
                else if (command is SynchronizeCommand) {
                    if (commandRecorder.Count != 0) {
                        commandRecorder.Execute(renderContext);
                        GLHelper.FenceSync(ref _sync);
                        GLFW.SwapBuffers(glfwContext);
                    }
                    context.SendCommand<CompositionTarget>(SynchronizeCommand.Instance);
                    continue;
                }
                commandRecorder.Record(command);
            }
        });
        
        thread.Name = typeof(TCommandTarget).Name;
        thread.Start();

        return Disposable.Create(() =>
            context.SendCommand<TCommandTarget>(StopCommand.Instance));
    }

    private unsafe IDisposable CreateResourceCommandThread<TCommandTarget>(IContext context)
        where TCommandTarget : ICommandTarget
    {
        GLFW.WindowHint(WindowHintBool.Visible, false);
        var glfwContext = GLFW.CreateWindow(1, 1, "", null, _mainWindow);

        var commands = context.ConsumeCommands<TCommandTarget>();
        var commandHost = new CommandHost(context);

        void ExecuteCommand(ICommand command)
        {
            switch (command) {
            case BatchedCommand batchedCmd:
                batchedCmd.Commands.ForEach(ExecuteCommand);
                batchedCmd.Dispose();
                break;
            
            default:
                using (commandHost.Profile("ResourceCommands", command)) {
                    command.SafeExecuteAndDispose(commandHost);
                }
                break;
           }
        }

        var thread = new Thread(() => {
            GLFW.MakeContextCurrent(glfwContext);
            foreach (var command in commands) {
                if (command is StopCommand) {
                    break;
                }
                ExecuteCommand(command);
            }
        });
        
        thread.Name = typeof(TCommandTarget).Name;
        thread.Start();

        return Disposable.Create(() =>
            context.SendCommand<TCommandTarget>(StopCommand.Instance));
    }
}