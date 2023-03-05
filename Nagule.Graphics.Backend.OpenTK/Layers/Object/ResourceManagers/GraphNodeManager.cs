namespace Nagule.Graphics.Backend.OpenTK;

using System.Reactive.Disposables;

using Nagule.Graphics;

public class GraphNodeManager : ResourceManagerBase<GraphNode>
{
    protected override void Initialize(IContext context, Guid id, GraphNode resource, GraphNode? prevResource)
    {
        ref var attachments = ref context.Acquire<GraphNodeAttachments>(id);

        if (prevResource != null) {
            ClearGraphNode(context, id, in attachments);
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

        var meshRenderable = resource.MeshRenderable;
        if (meshRenderable != null) {
            context.SetResource(id, meshRenderable);
        }

        GraphNode.GetProps(context, id).Set(resource);

        attachments.Lights.Clear();
        attachments.Children.Clear();
        attachments.Metadata.Clear();

        var lights = resource.Lights;
        if (lights != null) {
            foreach (var light in lights) {
                var lightId = light.Id ?? Guid.NewGuid();
                if (!attachments.Lights.TryAdd(light, lightId)) {
                    Console.WriteLine("GraphNode light ids conflict.");
                    continue;
                }
                context.Remove<Destroy>(lightId);
                context.SetResource(lightId, light);
                context.Acquire<Parent>(lightId).Id = id;
            }
        }

        var children = resource.Children;
        if (children != null) {
            foreach (var child in children) {
                var childId = child.Id ?? Guid.NewGuid();
                if (!attachments.Children.TryAdd(child, childId)) {
                    Console.WriteLine("GraphNode children ids conflict");
                    continue;
                }
                context.Remove<Destroy>(childId);
                context.SetResource(childId, child);
                context.Acquire<Parent>(childId).Id = id;
            }
        }

        var metadata = resource.Metadata;
        if (metadata != null && metadata.Count != 0) {
            foreach (var (k, v) in metadata) {
                attachments.Metadata[k] = v;
            }
        }
    }

    protected override IDisposable? Subscribe(IContext context, Guid id, GraphNode resource)
    {
        ref var props = ref GraphNode.GetProps(context, id);

        return new CompositeDisposable(
            props.Position.Subscribe(pos =>
                context.Acquire<Transform>(id).LocalPosition = pos),
            props.Rotation.Subscribe(rot =>
                context.Acquire<Transform>(id).LocalRotation = rot),
            props.Scale.Subscribe(scale =>
                context.Acquire<Transform>(id).LocalScale = scale),

            props.MeshRenderable.Subscribe(resource => {
                if (resource != null) {
                    context.SetResource(id, resource);
                }
                else {
                    context.Remove<Resource<MeshRenderable>>(id);
                }
            }),

            props.Lights.Subscribe(e => {
                var lights = context.Require<GraphNodeAttachments>(id).Lights;
                switch (e.Operation) {
                case ReactiveSetOperation.Add:
                    lights[e.Value] = ResourceLibrary.Reference(context, id, e.Value);
                    break;
                case ReactiveSetOperation.Remove:
                    if (!lights.Remove(e.Value, out var lightId)) {
                        Console.WriteLine("Internal error: GraphNode light not found");
                        break;
                    }
                    ResourceLibrary.Unreference(context, id, lightId);
                    break;
                }
            }),

            props.Children.Subscribe(e => {
                var children = context.Require<GraphNodeAttachments>(id).Children;
                switch (e.Operation) {
                case ReactiveSetOperation.Add:
                    children[e.Value] = ResourceLibrary.Reference(context, id, e.Value);
                    break;
                case ReactiveSetOperation.Remove:
                    if (!children.Remove(e.Value, out var childId)) {
                        Console.WriteLine("Internal error: GraphNode child not found");
                        break;
                    }
                    ResourceLibrary.Unreference(context, id, childId);
                    break;
                }
            }),

            props.Metadata.Subscribe(e => {
                var metadata = context.Require<GraphNodeAttachments>(id).Metadata;
                e.ApplyTo(metadata);
            })
        );
    }

    protected override void Uninitialize(IContext context, Guid id, GraphNode resource)
    {
        if (context.Remove<GraphNodeAttachments>(id, out var attachments)) {
            Console.WriteLine("Error: failed to remove graph node attachments.");
            return;
        }
        ClearGraphNode(context, id, in attachments);
    }

    private void ClearGraphNode(IContext context, Guid id, in GraphNodeAttachments attachments)
    {
        ResourceLibrary.UnreferenceAll(context, id);

        foreach (var lightId in attachments.Lights.Values) {
            context.Destroy(lightId);
        }
        foreach (var childId in attachments.Children.Values) {
            context.Destroy(childId);
        }

        attachments.Lights.Clear();
        attachments.Children.Clear();
    }
}