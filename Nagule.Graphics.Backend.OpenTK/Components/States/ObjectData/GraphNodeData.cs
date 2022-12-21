namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using Nagule.Graphics;

public struct GraphNodeData : IPooledComponent
{
    public List<Guid> LightIds = new();
    public List<Mesh> Meshes = new();
    public List<Guid> ChildrenIds = new();

    public GraphNodeData() {}
}