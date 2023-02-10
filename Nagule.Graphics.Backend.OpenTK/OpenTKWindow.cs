namespace Nagule.Graphics.Backend.OpenTK;

using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Windowing.Common;
using global::OpenTK.Windowing.Common.Input;
using global::OpenTK.Windowing.Desktop;
using global::OpenTK.Windowing.GraphicsLibraryFramework;
using global::OpenTK.Mathematics;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;
using Nagule;

using CursorState = global::OpenTK.Windowing.Common.CursorState;
using InputAction = global::OpenTK.Windowing.GraphicsLibraryFramework.InputAction;
using VSyncMode = global::OpenTK.Windowing.Common.VSyncMode;
using MouseButton = Nagule.MouseButton;
using KeyModifiers = Nagule.KeyModifiers;
using Window = Nagule.Window;

public class OpenTKWindow : Layer, ILoadListener, IUnloadListener, IEngineUpdateListener
{
    private class InternalWindow : NativeWindow
    {
        private IContext _context;
        private GraphicsCommandExecutor _commandExecutor;

        private GraphicsSpecification _spec;
        private GLDebugProc? _debugProc;
        private System.Numerics.Vector4 _clearColor;

        private Thread? _renderThread;

        private List<MouseButton> _downMouseButtons = new();
        private List<MouseButton> _upMouseButtons = new();
        private List<Key> _downKeys = new();
        private List<Key> _upKeys = new();

        private double _updateFramePeriod;
        private double _renderFramePeriod;

        private volatile bool _isRunningSlowly;

        public InternalWindow(IContext context, in GraphicsSpecification spec)
            : base(
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
            _context = context;
            _commandExecutor = _context.GetSublayerRecursively<GraphicsCommandExecutor>()
                ?? throw new InvalidOperationException("GraphicsCommandExecutor not found");

            _spec = spec;
            _clearColor = spec.ClearColor;

            VSync = _spec.VSyncMode switch {
                Nagule.VSyncMode.On => global::OpenTK.Windowing.Common.VSyncMode.On,
                Nagule.VSyncMode.Off => global::OpenTK.Windowing.Common.VSyncMode.Off,
                _ => global::OpenTK.Windowing.Common.VSyncMode.Adaptive
            };

            _renderFramePeriod = spec.RenderFrequency <= 0 ? 0 : 1 / (double)spec.RenderFrequency;

            if (_spec.UpdateFrequency is int updateFrequency) {
                _updateFramePeriod = updateFrequency <= 0 ? 0 : 1 / (double)updateFrequency!;
            }
        }

        private void DebugProc(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            string messageStr = Marshal.PtrToStringAnsi(message, length);
            Console.WriteLine($"[GL Message] type={type}, severity={severity}, message={messageStr}");
        }

        private void OnLoad()
        {
            GL.ClearDepth(1f);
            GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
            GL.Disable(EnableCap.Blend);

            if (_spec.IsDebugEnabled) {
                _debugProc = DebugProc;
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback(_debugProc, IntPtr.Zero);
            }

            foreach (var listener in _context.GetSublayersRecursively<IWindowInitilaizedListener>()) {
                listener.OnWindowInitialized(_context);
            }
        }

        public unsafe void Run()
        {
            Context?.MakeCurrent();
            OnLoad();
            OnResize(new ResizeEventArgs(Size));

            Context?.MakeNoneCurrent();
            _renderThread = new Thread(StartRenderThread);
            _renderThread.Start();

            var frameWatch = new Stopwatch();
            double elapsed;

            frameWatch.Start();

            while (!GLFW.WindowShouldClose(WindowPtr)) {
                elapsed = frameWatch.Elapsed.TotalSeconds;
                double sleepTime = _updateFramePeriod - elapsed;

                if (sleepTime > 0) {
                    SpinWait.SpinUntil(() => true, (int)Math.Floor(sleepTime * 1000));
                    continue;
                }

                frameWatch.Restart();

                ProcessInputEvents();
                ProcessWindowEvents(IsEventDriven);
                DispatchUpdate((float)elapsed);
            }

            if (_context.Running) {
                _context.Unload();
            }
        }

        private unsafe void StartRenderThread()
        {
            Context?.MakeCurrent();

            var frameWatch = new Stopwatch();
            double elapsed;

            frameWatch.Start();

            while (_context.Running) {
                elapsed = frameWatch.Elapsed.TotalSeconds;

                if (!_spec.UpdateFrequency.HasValue) {
                    _updateFramePeriod = elapsed;
                }

                double sleepTime = _renderFramePeriod - elapsed;

                if (sleepTime > 0) {
                    SpinWait.SpinUntil(() => true, (int)Math.Floor(sleepTime * 1000));
                    continue;
                }
                if (!_context.Running) { return; }

                frameWatch.Restart();
                DispatchRender((float)elapsed);

                if (_renderFramePeriod != 0) {
                    _isRunningSlowly = elapsed - _renderFramePeriod >= _renderFramePeriod;
                }
            }
        }

        private void DispatchRender(float elapsed)
        {
            _commandExecutor.Execute(_context);

            if (VSync == VSyncMode.Adaptive) {
                GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
            }
        }

        private void DispatchUpdate(float elapsed)
        {
            _context.Update(elapsed);

            ResetMouse();
            ResetKeys();
        }

        private void ResetMouse()
        {
            ref var mouse = ref _context.Acquire<Mouse>(Devices.MouseId);
            mouse.DeltaX = 0;
            mouse.DeltaY = 0;

            if (_downMouseButtons.Count != 0) {
                var buttons = mouse.Buttons;
                foreach (var button in _downMouseButtons) {
                    buttons[button] = MouseButtonState.PressedState;
                }
                _downMouseButtons.Clear();
            }

            if (_upMouseButtons.Count != 0) {
                var states = mouse.Buttons;
                foreach (var button in _upMouseButtons) {
                    states[button] = MouseButtonState.EmptyState;
                }
                _upMouseButtons.Clear();
            }
        }

        private void ResetKeys()
        {
            if (_downKeys.Count != 0) {
                ref var keyboard = ref _context.Acquire<Keyboard>(Devices.KeyboardId);
                var keys = keyboard.Keys;
                foreach (var key in _downKeys) {
                    keys[key] = KeyState.PressedState;
                }
                _downKeys.Clear();
            }

            if (_upKeys.Count != 0) {
                ref var keyboard = ref _context.Acquire<Keyboard>(Devices.KeyboardId);
                var states = keyboard.Keys;
                foreach (var key in _upKeys) {
                    states[key] = KeyState.EmptyState;
                }
                _upKeys.Clear();
            }
        }

        protected override void OnRefresh()
        {
            foreach (var listener in _context.GetListeners<IWindowRefreshListener>()) {
                listener.OnWindowRefresh(_context);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
            => _context.SetWindowSize(e.Width, e.Height);

        protected override void OnMove(WindowPositionEventArgs e)
            => _context.SetWindowPosition(e.X, e.Y);

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
            => _context.SetWindowFocused(e.IsFocused);

        protected override void OnMaximized(MaximizedEventArgs e)
            => _context.SetWindowState(Nagule.WindowState.Maximized);

        protected override void OnMinimized(MinimizedEventArgs e)
            => _context.SetWindowState(Nagule.WindowState.Minimized);

        protected override void OnMouseEnter()
            => _context.SetMouseInWindow( true);

        protected override void OnMouseLeave()
            => _context.SetMouseInWindow(false);

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Action == InputAction.Repeat) { return; }

            var button = (MouseButton)e.Button;
            _context.SetMouseDown(button, (KeyModifiers)e.Modifiers);
            _downMouseButtons.Add(button);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var button = (MouseButton)e.Button;
            _context.SetMouseUp(button, (KeyModifiers)e.Modifiers);
            _upMouseButtons.Add(button);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
            => _context.SetMousePosition(e.X, e.Y);

        protected override void OnMouseWheel(MouseWheelEventArgs e)
            => _context.SetMouseWheel(e.OffsetX, e.OffsetY);

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.IsRepeat) { return; }

            var key = (Key)e.Key;
            _context.SetKeyDown(key, (KeyModifiers)e.Modifiers);
            _downKeys.Add(key);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var key = (Key)e.Key;
            _context.SetKeyUp(key, (KeyModifiers)e.Modifiers);
            _upKeys.Add(key);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            var unicode = (char)e.Unicode;
            foreach (var listener in _context.GetListeners<ITextInputListener>()) {
                listener.OnTextInput(_context, unicode);
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
        if (context is not IContext eventContext) {
            throw new NotSupportedException("OpenTKWindow must be added to event context");
        }
        _window = new InternalWindow(eventContext, _spec);

        ref var window = ref context.Acquire<Window>(Devices.WindowId);
        window.Width = _spec.Width;
        window.Height = _spec.Height;

        var monitorInfo = global::OpenTK.Windowing.Desktop.Monitors.GetPrimaryMonitor();
        ref var screen = ref context.Acquire<Screen>(Devices.ScreenId);
        screen.Width = monitorInfo.HorizontalResolution;
        screen.Height = monitorInfo.VerticalResolution;
        screen.WidthScale = monitorInfo.HorizontalScale;
        screen.HeightScale = monitorInfo.VerticalScale;

        context.Set<GraphicsSpecification>(Guid.NewGuid(), in _spec);
        context.Acquire<Mouse>(Devices.MouseId);
        context.Acquire<Keyboard>(Devices.KeyboardId);
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
            throw new InvalidOperationException("Context not loaded");
        }
        try {
            _window.Run();
        }
        finally {
            _window = null;
        }
    }

    public void OnEngineUpdate(IContext context)
    {
        UpdateCursor(context);
    }

    private void UpdateCursor(IContext context)
    {
        if (!context.ContainsAny<AnyModified<Nagule.Cursor>>()) {
            return;
        }

        ref readonly var cursor = ref context.Inspect<Nagule.Cursor>(Devices.CursorId);

        _window!.CursorState = cursor.State switch {
            Nagule.CursorState.Normal => CursorState.Normal,
            Nagule.CursorState.Hidden => CursorState.Hidden,
            Nagule.CursorState.Grabbed => CursorState.Grabbed,
            _ => throw new InvalidDataException("Invalid cursor state")
        };

        _window.Cursor = cursor.Style switch {
            CursorStyle.Default => MouseCursor.Default,
            CursorStyle.TextInput => MouseCursor.IBeam,
            CursorStyle.Crosshair => MouseCursor.Crosshair,
            CursorStyle.Hand => MouseCursor.Hand,
            CursorStyle.ResizeVertical => MouseCursor.VResize,
            CursorStyle.ResizeHorizontal => MouseCursor.HResize,
            CursorStyle.Empty => MouseCursor.Empty,
            _ => throw new InvalidDataException("Invalid cursor style")
        };
    }
}