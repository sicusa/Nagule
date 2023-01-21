namespace Nagule.Graphics;

public record RenderSettings : ResourceBase
{
    public static RenderSettings Default { get; } = new();

    public Cubemap? Skybox { get; init; }
}