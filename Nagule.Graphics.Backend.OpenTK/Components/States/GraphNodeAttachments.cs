namespace Nagule.Graphics.Backend.OpenTK;

public struct GraphNodeAttachments : IHashComponent
{
    public readonly Dictionary<Light, Guid> Lights = new();
    public readonly Dictionary<GraphNode, Guid> Children = new();
    public readonly Dictionary<string, Dyn> Metadata = new();

    public GraphNodeAttachments() {}
}