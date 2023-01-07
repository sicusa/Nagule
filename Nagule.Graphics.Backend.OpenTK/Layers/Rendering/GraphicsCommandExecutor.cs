namespace Nagule.Graphics.Backend.OpenTK;

using System.Reactive.Disposables;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Windowing.GraphicsLibraryFramework;

using Aeco;

public class GraphicsCommandExecutor
    : VirtualLayer, ILoadListener, IRenderListener, IUnloadListener
{
    private class ResourceWorkerTarget0 : ICommandTarget {}
    private class ResourceWorkerTarget1 : ICommandTarget {}
    private class ResourceWorkerTarget2 : ICommandTarget {}
    private class ResourceWorkerTarget3 : ICommandTarget {}

    private class SwapBuffersCommand : SingletonCommand<SwapBuffersCommand> {}
    private class SynchronizeCommand : SingletonCommand<SynchronizeCommand> {}
    private class StopCommand : SingletonCommand<StopCommand> {}

    [AllowNull] private IEnumerable<ICommand> _commands;

    private GLSync _sync;
    
    private IDisposable? _threadsDisposable;

    public void OnLoad(IContext context)
    {
        _commands = context.ConsumeCommands<RenderCompositionTarget>();
        GLHelper.FenceSync(ref _sync);

        _threadsDisposable = new CompositeDisposable(
            CreateGLCommandThread<RenderTarget>(context),

            CreateCommandDispatcherThread<GraphicsResourceTarget>(context, (cmd, counter) => {
                switch (counter % 4) {
                    case 0: context.SendCommand<ResourceWorkerTarget0>(cmd); break;
                    case 1: context.SendCommand<ResourceWorkerTarget1>(cmd); break;
                    case 2: context.SendCommand<ResourceWorkerTarget2>(cmd); break;
                    case 3: context.SendCommand<ResourceWorkerTarget3>(cmd); break;
                }
            }),

            CreateGLCommandThread<ResourceWorkerTarget0>(context),
            CreateGLCommandThread<ResourceWorkerTarget1>(context),
            CreateGLCommandThread<ResourceWorkerTarget2>(context),
            CreateGLCommandThread<ResourceWorkerTarget3>(context));
    }

    public void OnUnload(IContext context)
    {
        _threadsDisposable?.Dispose();
        _threadsDisposable = null;
    }

    public void OnRender(IContext context)
    {
        context.SendCommand<RenderTarget>(SynchronizeCommand.Instance);
        GLHelper.WaitSync(_sync);

        void ExecuteCommand(ICommand command)
        {
            switch (command) {
            case BatchedCommand batchedCmd:
                batchedCmd.Commands.ForEach(ExecuteCommand);
                batchedCmd.Dispose();
                break;
            
            default:
                command.SafeExecuteAndDispose(context);
                break;
            }
        }

        foreach (var command in _commands) {
            if (command is SynchronizeCommand) {
                break;
            }
            ExecuteCommand(command);
        }

        GLHelper.FenceSync(ref _sync);
        context.SendCommand<RenderTarget>(SwapBuffersCommand.Instance);
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

    private unsafe IDisposable CreateGLCommandThread<TCommandTarget>(IContext context)
        where TCommandTarget : ICommandTarget
    {
        GLFW.WindowHint(WindowHintBool.Visible, false);

        var primaryContext = GLFW.GetCurrentContext();
        var currentContext = GLFW.CreateWindow(1, 1, "", null, primaryContext);

        var commands = context.ConsumeCommands<TCommandTarget>();
        var spec = context.RequireAny<GraphicsSpecification>();
        var defaultVertexArray = VertexArrayHandle.Zero;

        void ExecuteCommand(ICommand command)
        {
            switch (command) {
            case SynchronizeCommand:
                GL.BindVertexArray(defaultVertexArray);
                context.SendCommand<RenderCompositionTarget>(SynchronizeCommand.Instance);
                break;
            
            case SwapBuffersCommand:
                GLFW.SwapBuffers(currentContext);
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

        var thread = new Thread(() => {
            GLFW.MakeContextCurrent(currentContext);

            defaultVertexArray = GL.GenVertexArray();
            GL.BindVertexArray(defaultVertexArray);

            var clearColor = spec.ClearColor;
            GL.ClearDepth(1f);
            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);

            GL.Flush();

            foreach (var command in commands) {
                if (command is StopCommand) {
                    GLFW.DestroyWindow(currentContext);
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