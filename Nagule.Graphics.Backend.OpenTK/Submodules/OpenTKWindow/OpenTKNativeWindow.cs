using OpenTK.Core;

namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sia;

public class OpenTKNativeWindow : NativeWindow
{
    public World World { get; }
    public EntityRef WindowEntity { get; }

    public bool IsRunning { get; set; }
    public int ExpectedSchedulerPeriod { get; private set; } = 16;

    private readonly ILogger _logger;

    private readonly SimulationFrame _simFrame;
    private readonly RenderFrame _renderFrame;
    private Peripheral _peripheral;

    private System.Numerics.Vector4 _clearColor;

    public bool IsDebugEnabled { get; }
    private readonly GLDebugProc? _debugProc;

    private double _updatePeriod;
    private readonly double _renderPeriod;

    private readonly bool _adaptiveUpdateFramePeriod;

    private Thread? _renderThread;
    private bool _isRunningSlowly;
    private int _slowUpdates = 0;

    private readonly Stopwatch _updateWatch = new();
    private readonly Stopwatch _renderWatch = new();

    #region Win32 Function for timing

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr SetThreadAffinityMask(IntPtr hThread, IntPtr dwThreadAffinityMask);

    [DllImport("kernel32")]
    private static extern IntPtr GetCurrentThread();

    [DllImport("winmm")]
    private static extern uint timeBeginPeriod(uint uPeriod);

    [DllImport("winmm")]
    private static extern uint timeEndPeriod(uint uPeriod);

    #endregion

    public OpenTKNativeWindow(World world, in EntityRef window)
        : this(world, window,
            ref window.Get<Window>(),
            ref window.Get<SimulationContext>(),
            ref window.Get<GraphicsContext>())
    {
    }

    private OpenTKNativeWindow(World world, in EntityRef windowEntity,
        ref Window window, ref SimulationContext simulation, ref GraphicsContext graphics)
        : base(
            new NativeWindowSettings {
                    Size = window.Size,
                    AutoLoadBindings = false,
                    MaximumSize = window.MaximumSize == null
                        ? null : new Vector2i(window.MaximumSize.Value.Item1, window.MaximumSize.Value.Item2),
                    MinimumSize = window.MinimumSize == null
                        ? null : new Vector2i(window.MinimumSize.Value.Item1, window.MinimumSize.Value.Item2),
                    Location = window.Location == null
                        ? null : new Vector2i(window.Location.Value.Item1, window.Location.Value.Item2),
                    Title = window.Title,
                    APIVersion = new Version(4, 1),
                    SrgbCapable = true,
                    RedBits = 16,
                    GreenBits = 16,
                    BlueBits = 16,
                    Flags = ContextFlags.ForwardCompatible,
                    WindowBorder = window.HasBorder
                        ? (window.IsResizable ? WindowBorder.Resizable : WindowBorder.Fixed)
                        : WindowBorder.Hidden,
                    WindowState = window.IsFullscreen
                        ? TKWindowState.Fullscreen
                        : TKWindowState.Normal
                })
    {
        GLLoader.LoadBindings(new GLFWBindingsContext());

        World = world;
        WindowEntity = windowEntity;

        _logger = world.GetAddon<LogLibrary>().Create<OpenTKNativeWindow>();
        _simFrame = world.GetAddon<SimulationFrame>();
        _renderFrame = world.GetAddon<RenderFrame>();
        _peripheral = world.GetAddon<Peripheral>();

        var renderFreq = graphics.RenderFrequency ?? 60;
        _renderPeriod = renderFreq <= 0 ? 0 : 1 / renderFreq;

        var updateFreq = simulation.UpdateFrequency;
        if (updateFreq == null) {
            _adaptiveUpdateFramePeriod = true;
        }
        else {
            _updatePeriod = updateFreq.Value <= 0 ? 0 : 1 / updateFreq.Value;
        }

        _clearColor = graphics.ClearColor;

        VSync = graphics.VSyncMode switch {
            VSyncMode.On => TKVSyncMode.On,
            VSyncMode.Off => TKVSyncMode.Off,
            _ => TKVSyncMode.Adaptive
        };

        IsDebugEnabled = graphics.IsDebugEnabled;
        if (IsDebugEnabled) {
            _debugProc = DebugProc;
            GCHandle.Alloc(_debugProc);
        }
    }

    private void DebugProc(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
    {
        string messageStr = Marshal.PtrToStringAnsi(message, length);
        _logger.LogError("type={Type}, severity={Severity}, message={Message}", type, severity, messageStr);
    }

    private void OnLoad()
    {
        GL.ClearDepth(1f);
        GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        if (IsDebugEnabled) {
            GL.DebugMessageCallback(_debugProc!, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
        }
    }

    public unsafe void Run()
    {
        const int TimePeriod = 8;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1));
            timeBeginPeriod(TimePeriod);
            ExpectedSchedulerPeriod = TimePeriod;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
            ExpectedSchedulerPeriod = 1;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            ExpectedSchedulerPeriod = 1;
        }

        IsRunning = true;

        Context?.MakeCurrent();
        OnLoad();
        OnResize(new ResizeEventArgs(Size));

        Context?.MakeNoneCurrent();
        _renderThread = new Thread(StartRenderThread);
        _renderThread.Start();

        _updateWatch.Start();
        _renderWatch.Start();

        while (!GLFW.WindowShouldClose(WindowPtr)) {
            double sleepTime = DispatchUpdate();
            if (sleepTime > 0) {
                Utils.AccurateSleep(sleepTime, ExpectedSchedulerPeriod);
                continue;
            }
        }

        IsRunning = false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            timeEndPeriod(TimePeriod);
        }
    }

    private double DispatchUpdate()
    {
        var elapsed = _updateWatch.Elapsed.TotalSeconds;
        if (elapsed <= _updatePeriod) {
            return _updatePeriod - elapsed;
        }

        _updateWatch.Restart();

        _peripheral.Keyboard.Frame = _simFrame.FrameCount;
        _peripheral.Mouse.Frame = _simFrame.FrameCount;

        NewInputFrame();
        ProcessWindowEvents(IsEventDriven);

        _updateWatch.Restart();
        _simFrame.Update((float)elapsed);

        ResetMouse();

        const int MaxSlowUpdates = 80;
        const int SlowUpdatesThreshold = 45;

        elapsed = _updateWatch.Elapsed.TotalSeconds;

        if (_updatePeriod < elapsed) {
            _slowUpdates++;
            if (_slowUpdates > MaxSlowUpdates) {
                _slowUpdates = MaxSlowUpdates;
            }
        }
        else {
            _slowUpdates--;
            if (_slowUpdates < 0) {
                _slowUpdates = 0;
            }
        }

        _isRunningSlowly = _slowUpdates > SlowUpdatesThreshold;

        if (API != ContextAPI.NoAPI) {
            if (VSync == TKVSyncMode.Adaptive) {
                GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
            }
        }
        return _updatePeriod - elapsed;
    }

    private unsafe void StartRenderThread()
    {
        Context?.MakeCurrent();

        var frameWatch = new Stopwatch();
        double elapsed;

        frameWatch.Start();

        while (IsRunning) {
            elapsed = frameWatch.Elapsed.TotalSeconds;

            if (_adaptiveUpdateFramePeriod) {
                _updatePeriod = elapsed;
            }

            double sleepTime = _renderPeriod - elapsed;
            if (sleepTime > 0) {
                Utils.AccurateSleep(sleepTime, ExpectedSchedulerPeriod);
                continue;
            }
            if (!IsRunning) { return; }

            frameWatch.Restart();
            DispatchRender(elapsed);

            if (_renderPeriod != 0) {
                _isRunningSlowly = elapsed - _renderPeriod >= _renderPeriod;
            }
        }
    }

    private void DispatchRender(double elapsed)
    {
        _renderFrame.Update((float)elapsed);

        if (VSync == TKVSyncMode.Adaptive) {
            GLFW.SwapInterval(_isRunningSlowly ? 0 : 1);
        }
    }

    private void ResetMouse()
    {
        _peripheral.Mouse.Delta = System.Numerics.Vector2.Zero;
    }

    protected override void OnRefresh()
    {
        ref var window = ref WindowEntity.Get<Window>();

        var monitor = Monitors.GetMonitorFromWindow(this);
        var size = (monitor.HorizontalResolution, monitor.VerticalResolution);
        var scale = new System.Numerics.Vector2(monitor.HorizontalScale, monitor.VerticalScale);

        if (window.ScreenSize != size) {
            window.ScreenSize = size;
            World.Send(WindowEntity, new Window.OnScreenSizeChanged(size));
        }
        if (window.ScreenScale != scale) {
            window.ScreenScale = scale;
            window.PhysicalSize = ((int)(window.Size.Item1 * scale.X), (int)(window.Size.Item2 * scale.Y));
            World.Send(WindowEntity, new Window.OnScreenScaleChanged(scale));
        }
        World.Send(WindowEntity, Window.OnRefresh.Instance);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        ref var window = ref WindowEntity.Get<Window>();
        var size = (e.Width, e.Height);
        window.Size = size;

        var scale = window.ScreenScale;
        window.PhysicalSize = ((int)(window.Size.Item1 * scale.X), (int)(window.Size.Item2 * scale.Y));

        World.Send(WindowEntity, new Window.OnSizeChanged(size));
    }

    protected override void OnMove(WindowPositionEventArgs e)
    {
        var location = (e.X, e.Y);
        WindowEntity.Get<Window>().Location = location;
        World.Send(WindowEntity, new Window.OnLocationChanged(location));
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        WindowEntity.Get<Window>().IsFocused = e.IsFocused;
        World.Send(WindowEntity, new Window.OnFocusChanged(e.IsFocused));
    }

    protected override void OnMaximized(MaximizedEventArgs e)
    {
        WindowEntity.Get<Window>().State = Nagule.WindowState.Maximized;
        World.Send(WindowEntity, new Window.OnStateChanged(Nagule.WindowState.Maximized));
    }

    protected override void OnMinimized(MinimizedEventArgs e)
    {
        WindowEntity.Get<Window>().State = Nagule.WindowState.Minimized;
        World.Send(WindowEntity, new Window.OnStateChanged(Nagule.WindowState.Minimized));
    }

    protected override void OnTextInput(TextInputEventArgs e)
        => World.Send(WindowEntity, new Window.OnTextInput((char)e.Unicode));

    protected override void OnFileDrop(FileDropEventArgs e)
        => World.Send(WindowEntity, new Window.OnFileDrop(e.FileNames.ToImmutableArray()));

    protected override void OnJoystickConnected(JoystickEventArgs e)
        => World.Send(WindowEntity, new Window.OnJoystickConnectionChanged(e.JoystickId, e.IsConnected));

    protected override void OnMouseEnter()
    {
        _peripheral.Mouse.InWindow = true;
        World.Send(_peripheral.Entity, new Mouse.OnInWindowChanged(true));
    }

    protected override void OnMouseLeave()
    {
        _peripheral.Mouse.InWindow = false;
        World.Send(_peripheral.Entity, new Mouse.OnInWindowChanged(false));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.Action == InputAction.Repeat) { return; }

        var button = (MouseButton)e.Button;
        ref var mouse = ref _peripheral.Mouse;
        ref var state = ref mouse.ButtonStates[button];

        state = new(true, mouse.Frame);
        World.Send(_peripheral.Entity, new Mouse.OnButtonStateChanged(button, state));
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var button = (MouseButton)e.Button;
        ref var mouse = ref _peripheral.Mouse;
        ref var state = ref mouse.ButtonStates[button];

        state = new(false, mouse.Frame);
        World.Send(_peripheral.Entity, new Mouse.OnButtonStateChanged(button, state));
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        ref var mouse = ref _peripheral.Mouse;
        mouse.Position = new(e.X, e.Y);
        World.Send(_peripheral.Entity, new Mouse.OnPositionChanged(mouse.Position));
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ref var mouse = ref _peripheral.Mouse;
        mouse.WheelOffset = new(e.OffsetX, e.OffsetY);
        World.Send(_peripheral.Entity, new Mouse.OnWheelOffsetChanged(mouse.Position));
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        if (e.IsRepeat) { return; }

        var key = (Key)e.Key;
        ref var keyboard = ref _peripheral.Keyboard;
        ref var state = ref keyboard.KeyStates[key];

        state = new(true, keyboard.Frame);
        World.Send(_peripheral.Entity, new Keyboard.OnKeyStateChanged(key, state));
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        var key = (Key)e.Key;
        ref var keyboard = ref _peripheral.Keyboard;
        ref var state = ref keyboard.KeyStates[key];

        state = new(false, keyboard.Frame);
        World.Send(_peripheral.Entity, new Keyboard.OnKeyStateChanged(key, state));
    }
}