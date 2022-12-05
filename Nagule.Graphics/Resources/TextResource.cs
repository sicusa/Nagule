namespace Nagule.Graphics;

public record TextResource : ResourceBase
{
    public string Content;

    public TextResource(string content)
    {
        Content = content;
    }
}