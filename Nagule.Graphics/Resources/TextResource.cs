namespace Nagule.Graphics;

public record TextResource : ResourceBase
{
    public string Content { get; init; } = "";
}