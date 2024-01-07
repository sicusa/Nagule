namespace Nagule.Graphics;

using System.Numerics;

using Sia;

[SiaTemplate(nameof(GraphicsSettings))]
[NaAsset]
public record RGraphicsSettings : AssetBase
{
    // Window
    public int Width { get; init; } = 800;
    public int Height { get; init; } = 600;
    public string Title { get; init; } = "Nagule Engine";
    public bool IsResizable { get; init; } = true;
    public bool IsFullscreen { get; init; } = false;
    public bool HasBorder { get; init; } = true;
    public (int, int)? MaximumSize { get; init; } = null;
    public (int, int)? MinimumSize { get; init; } = null;
    public (int, int)? Location { get; init; } = null;

    // Rendering
    public int? UpdateFrequency { get; init; } = null;
    public int RenderFrequency { get; init; } = 60;
    public Vector4 ClearColor { get; init; } = Vector4.Zero;
    public VSyncMode VSyncMode { get; init; } = VSyncMode.Off;

    // Debug
    public bool IsDebugEnabled { get; init; } = false;
}