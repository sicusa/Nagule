namespace Nagule.Graphics;

public record Camera : ResourceBase
{
    public CameraMode Mode { get; init; } = CameraMode.Perspective;
    public RenderPipeline? RenderPipeline { get; init; }
    public RenderTexture? RenderTexture { get; init; }

    public ClearFlags ClearFlags { get; init; }
        = ClearFlags.Color | ClearFlags.Depth;

    public float FieldOfView { get; init; } = 60f;
    public float NearPlaneDistance { get; init; } = 0.01f;
    public float FarPlaneDistance { get; init; } = 200f;
    public int Depth { get; init; } = 0;
}