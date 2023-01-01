namespace Nagule.Graphics.Backend.OpenTK;

using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

using global::OpenTK.Graphics.OpenGL;
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
        private GraphicsSpecification _spec;
        private IEventContext _context;
        private GLDebugProc? _debugProc;
        private System.Numerics.Vector4 _clearColor;
        private volatile bool _unloaded;

        private SpinWait _updateSpinWait = new();
        private Thread? _updateThread;
        private Stopwatch _updateWatch = new Stopwatch();

        private ConcurrentBag<Key> _upKeys = new();
        private ConcurrentBag<MouseButton> _upMouseButtons = new();

        public InternalWindow(IEventContext context, in GraphicsSpecification spec)
            : base(
                new GameWindowSettings {
                    IsMultiThreaded = true,
                    RenderFrequency = spec.RenderFrequency,
                    UpdateFrequency = spec.UpdateFrequency
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

            VSync = _spec.VSyncMode switch {
                Nagule.VSyncMode.On => global::OpenTK.Windowing.Common.VSyncMode.On,
                Nagule.VSyncMode.Off => global::OpenTK.Windowing.Common.VSyncMode.Off,
                _ => global::OpenTK.Windowing.Common.VSyncMode.Adaptive
            };
        }

        private void DebugProc(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string messageStr = Marshal.PtrToStringAnsi(message, length);
            Console.WriteLine($"[GL Message] type={type}, severity={severity}, message={messageStr}");
        }

        protected override void OnLoad()
        {
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
            foreach (var listener in _context.GetSublayersRecursively<IWindowUninitilaizedListener>()) {
                listener.OnWindowUninitialized(_context);
            }
            _unloaded = true;
        }

        public override void Run()
        {
            _context.Update(0);
            _updateThread = new Thread(StartUpdateThread);
            _updateThread.Start();

            base.Run();

            _context.Unload();
        }

        private void StartUpdateThread()
        {
            _updateWatch.Start();

            while (!_unloaded) {
                var elapsed = _updateWatch.Elapsed.TotalSeconds;
                var updatePeriod = UpdateFrequency == 0 ? 0 : 1 / UpdateFrequency;

                if (elapsed > 0 && elapsed >= updatePeriod) {
                    _updateWatch.Restart();
                    UpdateTime = elapsed;
                    _context.Update((float)elapsed);

                    ref var mouse = ref _context.AcquireAny<Mouse>();
                    mouse.DeltaX = 0;
                    mouse.DeltaY = 0;

                    if (_upMouseButtons.Count != 0) {
                        var states = mouse.States;
                        foreach (var button in _upMouseButtons) {
                            states[(int)button] = MouseButtonState.EmptyState;
                        }
                        _upMouseButtons.Clear();
                    }

                    if (_upKeys.Count != 0) {
                        ref var keyboard = ref _context.AcquireAny<Keyboard>();
                        var states = keyboard.States.Raw;
                        foreach (var key in _upKeys) {
                            states[(int)key] = KeyState.EmptyState;
                        }
                        _upKeys.Clear();
                    }
                }
            }

            _updateSpinWait.SpinOnce();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            _context.Render((float)e.Time);
            SwapBuffers();
        }

        protected override void OnRefresh()
        {
            foreach (var listener in _context.GetListeners<IWindowRefreshListener>()) {
                listener.OnWindowRefresh(_context);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            _context.SetWindowSize(e.Width, e.Height);
        }

        protected override void OnMove(WindowPositionEventArgs e)
        {
            _context.SetWindowPosition(e.X, e.Y);
        }

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
            _context.SetWindowFocused(e.IsFocused);
        }

        protected override void OnMaximized(MaximizedEventArgs e)
        {
            _context.SetWindowState(Nagule.WindowState.Maximized);
        }

        protected override void OnMinimized(MinimizedEventArgs e)
        {
            _context.SetWindowState(Nagule.WindowState.Minimized);
        }

        protected override void OnMouseEnter()
        {
            _context.SetMouseInWindow( true);
        }

        protected override void OnMouseLeave()
        {
            _context.SetMouseInWindow( false);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Action == InputAction.Press) {
                _context.SetMousePressed((MouseButton)e.Button, (KeyModifiers)e.Modifiers);
            }
            else {
                _context.SetMouseDown((MouseButton)e.Button, (KeyModifiers)e.Modifiers);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var button = (MouseButton)e.Button;
            _context.SetMouseUp(button, (KeyModifiers)e.Modifiers);
            _upMouseButtons.Add(button);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            _context.SetMousePosition(e.X, e.Y);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            foreach (var listener in _context.GetListeners<IMouseWheelListener>()) {
                listener.OnMouseWheel(_context, e.OffsetX, e.OffsetY);
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.IsRepeat) {
                _context.SetKeyPressed((Key)e.Key, (KeyModifiers)e.Modifiers);
            }
            else {
                _context.SetKeyDown((Key)e.Key, (KeyModifiers)e.Modifiers);
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var key = (Key)e.Key;
            _context.SetKeyUp(key, (KeyModifiers)e.Modifiers);
            _upKeys.Add(key);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            foreach (var listener in _context.GetListeners<ITextInputListener>()) {
                listener.OnTextInput(_context, (char)e.Unicode);
            }
        }

        protected override void OnFileDrop(FileDropEventArgs e)
        {
            foreach (var listener in _context.GetListeners<IFileDropListener>()) {
                listener.OnFileDrop(_context, e.FileNames);
            }
        }

        protected override void OnJoystickConnected(JoystickEventArgs e)
        {
            foreach (var listener in _context.GetListeners<IJoystickConnectionListener>()) {
                listener.OnJoystickConnection(_context, e.JoystickId, e.IsConnected);
            }
        }
    }

    private GraphicsSpecification _spec;
    private InternalWindow? _window = null;

    public OpenTKWindow(in GraphicsSpecification spec)
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

        var monitorInfo = global::OpenTK.Windowing.Desktop.Monitors.GetPrimaryMonitor();
        ref var screen = ref context.AcquireAny<Screen>();
        screen.Width = monitorInfo.HorizontalResolution;
        screen.Height = monitorInfo.VerticalResolution;
        screen.WidthScale = monitorInfo.HorizontalScale;
        screen.HeightScale = monitorInfo.VerticalScale;

        context.Set<GraphicsSpecification>(Guid.NewGuid(), in _spec);
        context.AcquireAny<Mouse>();
        context.AcquireAny<Keyboard>();
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
        try {
            _window.Run();
        }
        finally {
            _window = null;
        }
    }
}