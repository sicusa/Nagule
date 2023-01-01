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
        style.Colors[(int)ImGuiCol.Border]                = new Vector4(0.31f, 0.31f, 1.00f, 0.00f);
        style.Colors[(int)ImGuiCol.BorderShadow]          = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        style.Colors[(int)ImGuiCol.FrameBg]               = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.FrameBgHovered]        = new Vector4(0.92f, 0.18f, 0.29f, 0.78f);
        style.Colors[(int)ImGuiCol.FrameBgActive]         = new Vector4(0.92f, 0.18f, 0.29f, 0.5f);
        style.Colors[(int)ImGuiCol.TitleBg]               = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed]      = new Vector4(0.20f, 0.22f, 0.27f, 0.75f);
        style.Colors[(int)ImGuiCol.TitleBgActive]         = new Vector4(0.92f, 0.18f, 0.29f, 0.8f);
        style.Colors[(int)ImGuiCol.MenuBarBg]             = new Vector4(0.20f, 0.22f, 0.27f, 0.47f);
        style.Colors[(int)ImGuiCol.ScrollbarBg]           = new Vector4(0.20f, 0.22f, 0.27f, 0.5f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab]         = new Vector4(0.09f, 0.15f, 0.16f, 0.5f);
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
}