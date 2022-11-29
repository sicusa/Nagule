namespace Nagule.Backend.OpenTK;

using System.Runtime.InteropServices;

using global::OpenTK.Graphics.OpenGL4;
using global::OpenTK.Windowing.Common;
using global::OpenTK.Windowing.Desktop;
using global::OpenTK.Mathematics;

using Aeco;

using Nagule.Graphics;
using Nagule;

using InputAction = global::OpenTK.Windowing.GraphicsLibraryFramework.InputAction;

public class OpenTKWindow : VirtualLayer, ILoadListener, IUnloadListener
{
    private class InternalWindow : GameWindow
    {
        private RendererSpec _spec;
        private IEventContext _context;
        private DebugProc? _debugProc;
        private System.Numerics.Vector4 _clearColor;

        public InternalWindow(IEventContext context, in RendererSpec spec)
            : base(
                new GameWindowSettings {
                    RenderFrequency = spec.RenderFrequency,
                    UpdateFrequency = spec.UpdateFrequency,
                },
                new NativeWindowSettings {
                    Size = (spec.Width, spec.Height),
                    MaximumSize = spec.MaximumSize == null
                        ? null : new Vector2i(spec.MaximumSize.Value.Item1, spec.MaximumSize.Value.Item2),
                    MinimumSize = spec.MinimumSize == null
                        ? null : new Vector2i(spec.MinimumSize.Value.Item1, spec.MinimumSize.Value.Item2),
                    Location = spec.Location == null
                        ? null : new Vector2i(spec.Location.Value.Item1, spec.Location.Value.Item2),
                    Title = spec.Title,
                    APIVersion = new Version(4, 1),
                    SrgbCapable = true,
                    Flags = ContextFlags.ForwardCompatible,
                    WindowBorder = spec.HasBorder
                        ? (spec.IsResizable ? WindowBorder.Resizable : WindowBorder.Fixed)
                        : WindowBorder.Hidden,
                    WindowState = spec.IsFullscreen
                        ? global::OpenTK.Windowing.Common.WindowState.Fullscreen
                        : global::OpenTK.Windowing.Common.WindowState.Normal
                })
        {
            _spec = spec;
            _context = context;
            _clearColor = spec.ClearColor;
        }

        private void DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
        {
            string message = Marshal.PtrToStringAnsi(messagePtr, length);
            Console.WriteLine($"[GL Message] type={type}, severity={severity}, message={message}");
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            GL.ClearDepth(1f);
            GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.DepthMask(true);

            if (_spec.IsDebugEnabled) {
                _debugProc = DebugProc;
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            }

            foreach (var listener in _context.GetSublayersRecursively<IWindowInitilaizedListener>()) {
                listener.OnWindowInitialized(_context);
            }
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            foreach (var listener in _context.GetSublayersRecursively<IWindowUninitilaizedListener>()) {
                listener.OnWindowUninitialized(_context);
            }
        }

        public override void Run()
        {
            base.Run();
            _context.Unload();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _context.Render((float)e.Time);
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            _context.Update((float)e.Time);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            foreach (var listener in _context.GetListeners<IWindowRefreshListener>()) {
                listener.OnWindowRefresh(_context);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            _context.SetWindowSize(e.Width, e.Height);
        }

        protected override void OnMove(WindowPositionEventArgs e)
        {
            base.OnMove(e);
            _context.SetWindowPosition(e.X, e.Y);
        }

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
            base.OnFocusedChanged(e);
            _context.SetWindowFocused(e.IsFocused);
        }

        protected override void OnMaximized(MaximizedEventArgs e)
        {
            base.OnMaximized(e);
            _context.SetWindowState(Nagule.WindowState.Maximized);
        }

        protected override void OnMinimized(MinimizedEventArgs e)
        {
            base.OnMinimized(e);
            _context.SetWindowState(Nagule.WindowState.Minimized);
        }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            _context.SetMouseInWindow( true);
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseEnter();
            _context.SetMouseInWindow( false);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Action == InputAction.Repeat) {
                foreach (var listener in _context.GetListeners<IMousePressedListener>()) {
                    listener.OnMousePressed(_context, (MouseButton)e.Button, (KeyModifiers)e.Modifiers);
                }
            }
            else {
                foreach (var listener in _context.GetListeners<IMouseDownListener>()) {
                    listener.OnMouseDown(_context, (MouseButton)e.Button, (KeyModifiers)e.Modifiers);
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            foreach (var listener in _context.GetListeners<IMouseUpListener>()) {
                listener.OnMouseUp(_context, (MouseButton)e.Button, (KeyModifiers)e.Modifiers);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            _context.SetMousePosition( e.X, e.Y);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            foreach (var listener in _context.GetListeners<IMouseWheelListener>()) {
                listener.OnMouseWheel(_context, e.OffsetX, e.OffsetY);
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.IsRepeat) {
                _context.SetKeyPressed((Key)e.Key, (KeyModifiers)e.Modifiers);
            }
            else {
                _context.SetKeyDown((Key)e.Key, (KeyModifiers)e.Modifiers);
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            _context.SetKeyUp((Key)e.Key, (KeyModifiers)e.Modifiers);
        }
    }

    private RendererSpec _spec;
    private InternalWindow? _window = null;

    public OpenTKWindow(in RendererSpec spec)
    {
        _spec = spec;
    }

    public void OnLoad(IContext context)
    {
        if (context is not IEventContext eventContext) {
            throw new NotSupportedException("OpenTKWindow must be added to event context");
        }
        _window = new InternalWindow(eventContext, _spec);

        ref var window = ref context.AcquireAny<Window>();
        window.Width = _spec.Width;
        window.Height = _spec.Height;

        context.AcquireAny<Mouse>();
        context.AcquireAny<Keyboard>();

        Console.WriteLine("OpenTK window initialized.");
    }

    public void OnUnload(IContext context)
    {
        if (_window != null) {
            _window.Close();
            _window = null;
        }
    }

    public void Run()
    {
        if (_window == null) {
            throw new InvalidOperationException("Nagule context not loaded");
        }
        Console.WriteLine("OpenTK window is running.");

        try {
            _window.Run();
        }
        finally {
            _window = null;
        }
    }
}