namespace Nagule.Graphics;

public struct GraphNode : IResourceObject<GraphNodeResource>
{
    public GraphNodeResource Resource { get; set; } = GraphNodeResource.Empty;

    public GraphNode() {}
}