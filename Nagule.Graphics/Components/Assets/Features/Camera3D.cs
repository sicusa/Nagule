namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Camera3D))]
[NaAsset]
public record RCamera3D : RFeatureBase
{
    public ProjectionMode ProjectionMode { get; init; } = ProjectionMode.Perspective;
    public ClearFlags ClearFlags { get; init; } = ClearFlags.Color | ClearFlags.Depth;

    public float? AspectRatio { get; init; }
    public float FieldOfView { get; init; } = 60f;
    public float OrthographicWidth { get; init; } = 10f;

    public float NearPlaneDistance { get; init; } = 0.01f;
    public float FarPlaneDistance { get; init; } = 20000f;

    public RRenderSettings RenderSettings { get; init; } = RRenderSettings.Default;
    public RenderPriority Priority { get; init; } = RenderPriority.Default;
    public RRenderTexture2D? TargetTexture { get; init; }
}