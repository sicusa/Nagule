namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public record struct RenderSettingsState
{
    public int Width;
    public int Height;
    public EntityRef? SkyboxEntity;
}