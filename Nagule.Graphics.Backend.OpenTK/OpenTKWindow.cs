namespace Nagule.Graphics.Backend.OpenTK;

using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics.OpenGL;
using global::OpenTK.Windowing.Common;
using global::OpenTK.Windowing.Desktop;
using global::OpenTK.Windowing.GraphicsLibraryFramework;
using global::OpenTK.Mathematics;

using ImGuiNET;

using Aeco;

using Nagule.Graphics;
using Nagule;

using Vector2 = System.Numerics.Vector2;
using InputAction = global::OpenTK.Windowing.GraphicsLibraryFramework.InputAction;
using VSyncMode = global::OpenTK.Windowing.Common.VSyncMode;
using MouseButton = Nagule.MouseButton;
using KeyModifiers = Nagule.KeyModifiers;
using Window = Nagule.Window;

public class OpenTKWindow : Layer, ILoadListener, IUnloadListener
{
    private class InternalWindow : NativeWindow
    {
        private GraphicsSpecification _spec;
        private IContext _context;
        private GLDebugProc? _debugProc;
        private System.Numerics.Vector4 _clearColor;

        private Thread? _renderThread;

        private List<Key> _upKeys = new();
        private List<MouseButton> _upMouseButtons = new();

        private Vector2 _scaleFactor;

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
            _spec = spec;
            _context = context;
            _clearColor = spec.ClearColor;

            var monitor = Monitors.GetPrimaryMonitor();
            _scaleFactor = new Vector2(monitor.HorizontalScale, monitor.VerticalScale);

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

            ProcessInputEvents();
            ProcessWindowEvents(IsEventDriven);

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
            _context.Render(elapsed);

            if (VSync == VSyncMode.Adaptive) {
                GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
            }
        }

        private void DispatchUpdate(float elapsed)
        {
            _context.Update(elapsed);

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
            var button = (MouseButton)e.Button;
            var io = ImGui.GetIO();

            if (io.MouseDown.Count <= (int)button) {
                return;
            }
            io.MouseDown[(int)button] = true;

            if (io.WantCaptureMouse) {
                return;
            }

            if (e.Action == InputAction.Press) {
                _context.SetMousePressed(button, (KeyModifiers)e.Modifiers);
            }
            else {
                _context.SetMouseDown(button, (KeyModifiers)e.Modifiers);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var button = (MouseButton)e.Button;
            var io = ImGui.GetIO();

            if (io.MouseDown.Count <= (int)button) {
                return;
            }
            io.MouseDown[(int)button] = false;

            if (io.WantCaptureMouse) {
                return;
            }

            _context.SetMouseUp(button, (KeyModifiers)e.Modifiers);
            _upMouseButtons.Add(button);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            var io = ImGui.GetIO();
            io.MousePos = new Vector2(e.X, e.Y) / _scaleFactor;

            if (io.WantCaptureMouse && ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow)) {
                return;
            }

            _context.SetMousePosition(e.X, e.Y);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var io = ImGui.GetIO();
            io.MouseWheel = e.OffsetY;
            io.MouseWheelH = e.OffsetX;

            if (io.WantCaptureMouse) {
                return;
            }

            foreach (var listener in _context.GetListeners<IMouseWheelListener>()) {
                listener.OnMouseWheel(_context, e.OffsetX, e.OffsetY);
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            var modifiers = (KeyModifiers)e.Modifiers;
            var io = ImGui.GetIO();
            int index = (int)e.Key;

            if (index >= io.KeysDown.Count) { return; }
            io.KeysDown[index] = true;

            io.KeyCtrl = (modifiers & KeyModifiers.Control) != 0;
            io.KeyAlt = (modifiers & KeyModifiers.Alt) != 0;
            io.KeyShift = (modifiers & KeyModifiers.Shift) != 0;
            io.KeySuper = (modifiers & KeyModifiers.Super) != 0;

            if (io.WantCaptureKeyboard) {
                return;
            }

            if (e.IsRepeat) {
                _context.SetKeyPressed((Key)e.Key, modifiers);
            }
            else {
                _context.SetKeyDown((Key)e.Key, modifiers);
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var key = (Key)e.Key;
            var io = ImGui.GetIO();
            int index = (int)key;

            if (index >= io.KeysDown.Count) { return; }
            io.KeysDown[index] = false;

            if (io.WantCaptureKeyboard) {
                return;
            }
            _context.SetKeyUp(key, (KeyModifiers)e.Modifiers);
            _upKeys.Add(key);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            var unicode = (char)e.Unicode;
            var io = ImGui.GetIO();

            io.AddInputCharacter(unicode);

            if (io.WantTextInput) {
                return;
            }

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