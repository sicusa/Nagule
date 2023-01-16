namespace Nagule.Graphics.Backend.OpenTK;

using Nagule.Graphics;

public class GraphNodeManager : ResourceManagerBase<GraphNode>
{
    protected override void Initialize(IContext context, Guid id, GraphNode resource, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource);
        }

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

        ref var data = ref context.Acquire<GraphNodeData>(id);

        var meshRenderable = resource.MeshRenderable;
        if (meshRenderable != null) {
            context.SetResource(id, meshRenderable);
        }

        var lights = resource.Lights;
        if (lights != null) {
            data.LightIds.AddRange(
                lights.Select(light =>
                    ResourceLibrary<Light>.Reference(context, id, light)));
            SetParent(context, id, data.LightIds);
        }

        var children = resource.Children;
        if (children != null) {
            data.ChildrenIds.AddRange(
                children.Select(child =>
                    ResourceLibrary<GraphNode>.Reference(context, id, child)));
            SetParent(context, id, data.ChildrenIds);
        }
    }

    protected override void Uninitialize(IContext context, Guid id, GraphNode resource)
    {
        ref var data = ref context.Acquire<GraphNodeData>(id);

        foreach (var lightId in data.LightIds) {
            ResourceLibrary<Light>.Unreference(context, id, lightId);
        }
        data.LightIds.Clear();

        foreach (var childId in data.ChildrenIds) {
            ResourceLibrary<GraphNode>.Unreference(context, id, childId);
        }
        data.ChildrenIds.Clear();
    }
}