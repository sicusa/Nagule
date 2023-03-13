namespace Nagule.Graphics;

public struct CameraProps : IHashComponent
{
    public ReactiveObject<ProjectionMode> ProjectionMode { get; } = new();
    public ReactiveObject<ClearFlags> ClearFlags { get; } = new();

    public ReactiveObject<float?> AspectRatio { get; } = new();
    public ReactiveObject<float> FieldOfView { get; } = new();
    public ReactiveObject<float> OrthographicSize { get; } = new();

    public ReactiveObject<float> NearPlaneDistance { get; } = new();
    public ReactiveObject<float> FarPlaneDistance { get; } = new();
    public ReactiveObject<int> Depth { get; } = new();

    public ReactiveObject<RenderSettings> RenderSettings { get; } = new();

    public CameraProps() {}

    public void Set(Camera resource)
    {
        ProjectionMode.Value = resource.ProjectionMode;
        ClearFlags.Value = resource.ClearFlags;

        AspectRatio.Value = resource.AspectRatio;
        FieldOfView.Value = resource.FieldOfView;
        OrthographicSize.Value = resource.OrthographicSize;

        NearPlaneDistance.Value = resource.NearPlaneDistance;
        FarPlaneDistance.Value = resource.FarPlaneDistance;
        Depth.Value = resource.Depth;

        RenderSettings.Value = resource.RenderSettings;
    }
}

public record Camera : ResourceBase<CameraProps>
{
    public ProjectionMode ProjectionMode { get; init; } = ProjectionMode.Perspective;
    public ClearFlags ClearFlags { get; init; } = ClearFlags.Color | ClearFlags.Depth;

    public float? AspectRatio { get; init; }
    public float FieldOfView { get; init; } = 60f;
    public float OrthographicSize { get; init; } = 10f;

    public float NearPlaneDistance { get; init; } = 0.01f;
    public float FarPlaneDistance { get; init; } = 200f;
    public int Depth { get; init; } = 0;

    public RenderSettings RenderSettings { get; init; } = RenderSettings.Default;
}