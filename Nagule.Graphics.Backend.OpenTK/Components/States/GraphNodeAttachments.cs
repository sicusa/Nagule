namespace Nagule.Graphics.Backend.OpenTK;

public struct GraphNodeAttachments : IHashComponent
{
    public readonly Dictionary<Light, uint> Lights = new();
    public readonly Dictionary<GraphNode, uint> Children = new();
    public readonly Dictionary<string, Dyn> Metadata = new();

    public GraphNodeAttachments() {}
}