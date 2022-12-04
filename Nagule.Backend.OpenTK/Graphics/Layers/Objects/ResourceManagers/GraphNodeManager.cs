namespace Nagule.Backend.OpenTK;

using Nagule.Graphics;

public class GraphNodeManager : ResourceManagerBase<GraphNode, GraphNodeData, GraphNodeResource>
{
    protected override void Initialize(IContext context, Guid id, ref GraphNode node, ref GraphNodeData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in node, in data);
        }

        var resource = node.Resource;
        context.Acquire<Name>(id).Value = resource.Name;

        if (resource.Metadata != null) {
            var metaDict = context.Acquire<Metadata>(id).Dictionary;
            foreach (var (k, v) in resource.Metadata) {
                metaDict[k] = v;
            }
        }

        ref var transform = ref context.Acquire<Transform>(id);
        transform.LocalPosition = resource.Position;
        transform.LocalRotation = resource.Rotation;
        transform.LocalScale = resource.Scale;

        void SetParent(IContext context, Guid parentId, List<Guid> childrenIds)
        {
            foreach (var childId in childrenIds) {
                context.Acquire<Parent>(childId).Id = parentId;
            }
        }

        var lights = resource.Lights;
        if (lights != null) {
            data.LightIds.AddRange(
                lights.Select(light =>
                    ResourceLibrary<LightResourceBase>.Reference<Light>(context, in light, id)));
            SetParent(context, id, data.LightIds);
        }

        var meshes = resource.Meshes;
        if (meshes != null) {
            data.Meshes.AddRange(meshes);
            var renderableMeshes = context.Acquire<MeshRenderable>(id).Meshes;
            foreach (var mesh in meshes) {
                renderableMeshes[mesh] = MeshRenderMode.Instance;
            }
        }

        var children = resource.Children;
        if (children != null) {
            data.ChildrenIds.AddRange(
                children.Select(child =>
                    ResourceLibrary<GraphNodeResource>.Reference<GraphNode>(context, in child, id)));
            SetParent(context, id, data.ChildrenIds);
        }
    }

    protected override void Uninitialize(IContext context, Guid id, in GraphNode node, in GraphNodeData data)
    {
        foreach (var lightId in data.LightIds) {
            ResourceLibrary<LightResourceBase>.Unreference(context, lightId, id);
        }
        data.LightIds.Clear();

        var renderableMeshes = context.Acquire<MeshRenderable>(id).Meshes;
        foreach (var mesh in data.Meshes) {
            renderableMeshes.Remove(mesh);
        }
        data.Meshes.Clear();

        foreach (var childId in data.ChildrenIds) {
            ResourceLibrary<GraphNodeResource>.Unreference(context, childId, id);
        }
        data.ChildrenIds.Clear();
    }
}