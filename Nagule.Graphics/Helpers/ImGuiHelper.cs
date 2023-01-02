namespace Nagule.Graphics;

using System.Numerics;
using System.Runtime.InteropServices;

using ImGuiNET;

public static class ImGuiHelper
{
    public static unsafe ImFontPtr AddFont(IContext context, Font font, int fontSize)
    {
        var scaleFactor = context.InspectAny<Screen>().WidthScale;

        var ptr = Marshal.AllocHGlobal(font.Bytes.Length);
        var dstSpan = new Span<byte>((void*)ptr, font.Bytes.Length);
        font.Bytes.AsSpan().CopyTo(dstSpan);

        var io = ImGui.GetIO();
        return io.Fonts.AddFontFromMemoryTTF(ptr, font.Bytes.Length, fontSize * scaleFactor);
    }

    public static IntPtr GetTextureId(IContext context, Guid id)
    {
        return context.TryGet<ImGuiTextureId>(id, out var comp)
            ? comp.Value : IntPtr.Zero;
    }

    public static void SetDefaultStyle()
    {
        var style = ImGui.GetStyle();

        style.WindowMinSize        = new Vector2(160, 20);
        style.FramePadding         = new Vector2(4, 2);
        style.ItemSpacing          = new Vector2(6, 2);
        style.ItemInnerSpacing     = new Vector2(6, 4);
        style.Alpha                = 0.95f;
        style.WindowRounding       = 4.0f;
        style.FrameRounding        = 2.0f;
        style.IndentSpacing        = 6.0f;
        style.ItemInnerSpacing     = new Vector2(2, 4);
        style.ColumnsMinSpacing    = 50.0f;
        style.GrabMinSize          = 14.0f;
        style.GrabRounding         = 16.0f;
        style.ScrollbarSize        = 12.0f;
        style.ScrollbarRounding    = 16.0f;

        style.Colors[(int)ImGuiCol.Text]                  = new Vector4(0.86f, 0.93f, 0.89f, 0.78f);
        style.Colors[(int)ImGuiCol.TextDisabled]          = new Vector4(0.86f, 0.93f, 0.89f, 0.28f);
        style.Colors[(int)ImGuiCol.WindowBg]              = new Vector4(0.13f, 0.14f, 0.17f, 0.8f);
        style.Colors[(int)ImGuiCol.DockingPreview]        = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.Border]                = new Vector4(0.31f, 0.31f, 1.00f, 0.00f);
        style.Colors[(int)ImGuiCol.BorderShadow]          = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.FrameBg]               = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.FrameBgHovered]        = new Vector4(0.92f, 0.18f, 0.29f, 0.3f);
        style.Colors[(int)ImGuiCol.FrameBgActive]         = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.TitleBg]               = new Vector4(0.20f, 0.22f, 0.27f, 0.75f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed]      = new Vector4(0.20f, 0.22f, 0.27f, 0.75f);
        style.Colors[(int)ImGuiCol.TitleBgActive]         = new Vector4(0.92f, 0.18f, 0.29f, 0.8f);
        style.Colors[(int)ImGuiCol.MenuBarBg]             = new Vector4(0.20f, 0.22f, 0.27f, 0.47f);
        style.Colors[(int)ImGuiCol.ScrollbarBg]           = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab]         = new Vector4(0.00f, 0.00f, 0.00f, 0.3f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered]  = new Vector4(0.92f, 0.18f, 0.29f, 0.78f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive]   = new Vector4(0.92f, 0.18f, 0.29f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark]             = new Vector4(0.71f, 0.22f, 0.27f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab]            = new Vector4(0.47f, 0.77f, 0.83f, 0.14f);
        style.Colors[(int)ImGuiCol.SliderGrabActive]      = new Vector4(0.92f, 0.18f, 0.29f, 1.0f);
        style.Colors[(int)ImGuiCol.Button]                = new Vector4(0.47f, 0.77f, 0.83f, 0.14f);
        style.Colors[(int)ImGuiCol.ButtonHovered]         = new Vector4(0.92f, 0.18f, 0.29f, 0.86f);
        style.Colors[(int)ImGuiCol.ButtonActive]          = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.Header]                = new Vector4(0.92f, 0.18f, 0.29f, 0.76f);
        style.Colors[(int)ImGuiCol.HeaderHovered]         = new Vector4(0.92f, 0.18f, 0.29f, 0.86f);
        style.Colors[(int)ImGuiCol.HeaderActive]          = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.TabActive]             = new Vector4(0.92f, 0.18f, 0.29f, 0.76f);
        style.Colors[(int)ImGuiCol.TabHovered]            = new Vector4(0.92f, 0.18f, 0.29f, 0.86f);
        style.Colors[(int)ImGuiCol.Tab]                   = new Vector4(0.62f, 0.13f, 0.20f, 0.8f);
        style.Colors[(int)ImGuiCol.TabUnfocused]          = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive]    = new Vector4(0.20f, 0.22f, 0.27f, 0.7f);
        style.Colors[(int)ImGuiCol.Separator]             = new Vector4(0.14f, 0.16f, 0.19f, 0.5f);
        style.Colors[(int)ImGuiCol.SeparatorHovered]      = new Vector4(0.92f, 0.18f, 0.29f, 0.78f);
        style.Colors[(int)ImGuiCol.SeparatorActive]       = new Vector4(0.92f, 0.18f, 0.29f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip]            = new Vector4(0.47f, 0.77f, 0.83f, 0.04f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered]     = new Vector4(0.92f, 0.18f, 0.29f, 0.78f);
        style.Colors[(int)ImGuiCol.ResizeGripActive]      = new Vector4(0.92f, 0.18f, 0.29f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines]             = new Vector4(0.86f, 0.93f, 0.89f, 0.63f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered]      = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.PlotHistogram]         = new Vector4(0.86f, 0.93f, 0.89f, 0.63f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered]  = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.TextSelectedBg]        = new Vector4(0.92f, 0.18f, 0.29f, 0.43f);
        style.Colors[(int)ImGuiCol.PopupBg]               = new Vector4(0.20f, 0.22f, 0.27f, 0.9f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg]      = new Vector4(0.20f, 0.22f, 0.27f, 0.73f);
    }

    public static void SetKeyMappings()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Insert] = (int)Key.Insert;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Backspace;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.Apostrophe] = (int)Key.Apostrophe;
        io.KeyMap[(int)ImGuiKey.Comma] = (int)Key.Comma;
        io.KeyMap[(int)ImGuiKey.Minus] = (int)Key.Minus;
        io.KeyMap[(int)ImGuiKey.Period] = (int)Key.Period;
        io.KeyMap[(int)ImGuiKey.Slash] = (int)Key.Slash;
        io.KeyMap[(int)ImGuiKey.Semicolon] = (int)Key.Semicolon;
        io.KeyMap[(int)ImGuiKey.Equal] = (int)Key.Equal;
        io.KeyMap[(int)ImGuiKey.LeftBracket] = (int)Key.LeftBracket;
        io.KeyMap[(int)ImGuiKey.Backslash] = (int)Key.Backslash;
        io.KeyMap[(int)ImGuiKey.RightBracket] = (int)Key.RightBracket;
        io.KeyMap[(int)ImGuiKey.GraveAccent] = (int)Key.GraveAccent;
        io.KeyMap[(int)ImGuiKey.CapsLock] = (int)Key.CapsLock;
        io.KeyMap[(int)ImGuiKey.ScrollLock] = (int)Key.ScrollLock;
        io.KeyMap[(int)ImGuiKey.NumLock] = (int)Key.NumLock;
        io.KeyMap[(int)ImGuiKey.PrintScreen] = (int)Key.PrintScreen;
        io.KeyMap[(int)ImGuiKey.Pause] = (int)Key.Pause;
        io.KeyMap[(int)ImGuiKey.Keypad0] = (int)Key.KeyPad0;
        io.KeyMap[(int)ImGuiKey.Keypad1] = (int)Key.KeyPad1;
        io.KeyMap[(int)ImGuiKey.Keypad2] = (int)Key.KeyPad2;
        io.KeyMap[(int)ImGuiKey.Keypad3] = (int)Key.KeyPad3;
        io.KeyMap[(int)ImGuiKey.Keypad4] = (int)Key.KeyPad4;
        io.KeyMap[(int)ImGuiKey.Keypad5] = (int)Key.KeyPad5;
        io.KeyMap[(int)ImGuiKey.Keypad6] = (int)Key.KeyPad6;
        io.KeyMap[(int)ImGuiKey.Keypad7] = (int)Key.KeyPad7;
        io.KeyMap[(int)ImGuiKey.Keypad8] = (int)Key.KeyPad8;
        io.KeyMap[(int)ImGuiKey.Keypad9] = (int)Key.KeyPad9;
        io.KeyMap[(int)ImGuiKey.KeypadDecimal] = (int)Key.KeyPadDecimal;
        io.KeyMap[(int)ImGuiKey.KeypadDivide] = (int)Key.KeyPadDivide;
        io.KeyMap[(int)ImGuiKey.KeypadMultiply] = (int)Key.KeyPadMultiply;
        io.KeyMap[(int)ImGuiKey.KeypadSubtract] = (int)Key.KeyPadSubtract;
        io.KeyMap[(int)ImGuiKey.KeypadAdd] = (int)Key.KeyPadAdd;
        io.KeyMap[(int)ImGuiKey.KeypadEnter] = (int)Key.KeyPadEnter;
        io.KeyMap[(int)ImGuiKey.KeypadEqual] = (int)Key.KeyPadEqual;
        io.KeyMap[(int)ImGuiKey.LeftShift] = (int)Key.LeftShift;
        io.KeyMap[(int)ImGuiKey.LeftCtrl] = (int)Key.LeftControl;
        io.KeyMap[(int)ImGuiKey.LeftAlt] = (int)Key.LeftAlt;
        io.KeyMap[(int)ImGuiKey.LeftSuper] = (int)Key.LeftSuper;
        io.KeyMap[(int)ImGuiKey.RightShift] = (int)Key.RightShift;
        io.KeyMap[(int)ImGuiKey.RightCtrl] = (int)Key.RightControl;
        io.KeyMap[(int)ImGuiKey.RightAlt] = (int)Key.RightAlt;
        io.KeyMap[(int)ImGuiKey.RightSuper] = (int)Key.RightSuper;
        io.KeyMap[(int)ImGuiKey.Menu] = (int)Key.Menu;
        io.KeyMap[(int)ImGuiKey._0] = (int)Key.D0;
        io.KeyMap[(int)ImGuiKey._1] = (int)Key.D1;
        io.KeyMap[(int)ImGuiKey._2] = (int)Key.D2;
        io.KeyMap[(int)ImGuiKey._3] = (int)Key.D3;
        io.KeyMap[(int)ImGuiKey._4] = (int)Key.D4;
        io.KeyMap[(int)ImGuiKey._5] = (int)Key.D5;
        io.KeyMap[(int)ImGuiKey._6] = (int)Key.D6;
        io.KeyMap[(int)ImGuiKey._7] = (int)Key.D7;
        io.KeyMap[(int)ImGuiKey._8] = (int)Key.D8;
        io.KeyMap[(int)ImGuiKey._9] = (int)Key.D9;
        io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.B] = (int)Key.B;
        io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.D] = (int)Key.D;
        io.KeyMap[(int)ImGuiKey.E] = (int)Key.E;
        io.KeyMap[(int)ImGuiKey.F] = (int)Key.F;
        io.KeyMap[(int)ImGuiKey.G] = (int)Key.G;
        io.KeyMap[(int)ImGuiKey.H] = (int)Key.H;
        io.KeyMap[(int)ImGuiKey.I] = (int)Key.I;
        io.KeyMap[(int)ImGuiKey.J] = (int)Key.J;
        io.KeyMap[(int)ImGuiKey.K] = (int)Key.K;
        io.KeyMap[(int)ImGuiKey.L] = (int)Key.L;
        io.KeyMap[(int)ImGuiKey.M] = (int)Key.M;
        io.KeyMap[(int)ImGuiKey.N] = (int)Key.N;
        io.KeyMap[(int)ImGuiKey.O] = (int)Key.O;
        io.KeyMap[(int)ImGuiKey.P] = (int)Key.P;
        io.KeyMap[(int)ImGuiKey.Q] = (int)Key.Q;
        io.KeyMap[(int)ImGuiKey.R] = (int)Key.R;
        io.KeyMap[(int)ImGuiKey.S] = (int)Key.S;
        io.KeyMap[(int)ImGuiKey.T] = (int)Key.T;
        io.KeyMap[(int)ImGuiKey.U] = (int)Key.U;
        io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.W] = (int)Key.W;
        io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        io.KeyMap[(int)ImGuiKey.F1] = (int)Key.F1;
        io.KeyMap[(int)ImGuiKey.F2] = (int)Key.F2;
        io.KeyMap[(int)ImGuiKey.F3] = (int)Key.F3;
        io.KeyMap[(int)ImGuiKey.F4] = (int)Key.F4;
        io.KeyMap[(int)ImGuiKey.F5] = (int)Key.F5;
        io.KeyMap[(int)ImGuiKey.F6] = (int)Key.F6;
        io.KeyMap[(int)ImGuiKey.F7] = (int)Key.F7;
        io.KeyMap[(int)ImGuiKey.F8] = (int)Key.F8;
        io.KeyMap[(int)ImGuiKey.F9] = (int)Key.F9;
        io.KeyMap[(int)ImGuiKey.F10] = (int)Key.F10;
        io.KeyMap[(int)ImGuiKey.F11] = (int)Key.F11;
        io.KeyMap[(int)ImGuiKey.F12] = (int)Key.F12;
    }

}