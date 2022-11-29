namespace Nagule.Graphics;

public record TextResource : IResource
{
    public string Content;

    public TextResource(string content)
    {
        Content = content;
    }
}