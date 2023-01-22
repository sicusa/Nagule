namespace Nagule.Graphics.Backend.OpenTK;

public struct GraphNodeData : IPooledComponent
{
    public List<Guid> LightIds = new();
    public List<Guid> ChildrenIds = new();

    public GraphNodeData() {}
}