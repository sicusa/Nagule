namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public record struct RenderSettingsState : IAssetState
{
    public readonly bool Loaded => Width != 0;

    public int Width;
    public int Height;
    public EntityRef? SkyboxEntity;
}