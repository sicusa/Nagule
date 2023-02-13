namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Reactive;

using Nagule.Graphics;

public class GraphNodeManager : ResourceManagerBase<GraphNode>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid GraphNodeId;
        public readonly List<Guid> LightIds = new();
        public readonly List<Guid> ChildrenIds = new();

        public override Guid? Id => GraphNodeId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<GraphNodeData>(GraphNodeId, out bool exists);
            if (exists) {
                data.LightIds.Clear();
                data.ChildrenIds.Clear();
            }
            data.LightIds.AddRange(LightIds);
            data.ChildrenIds.AddRange(ChildrenIds);
        }

        public override void Dispose()
        {
            LightIds.Clear();
            ChildrenIds.Clear();
            base.Dispose();
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid GraphNodeId;

        public override void Execute(ICommandHost host)
        {
            host.Remove<GraphNodeData>(GraphNodeId, out var data);
        }
    }

    protected override void Initialize(IContext context, Guid id, GraphNode resource, GraphNode? prevResource)
    {
        if (prevResource != null) {
            ResourceLibrary.UnreferenceAll(context, id);
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

        var meshRenderable = resource.MeshRenderable;
        if (meshRenderable != null) {
            context.SetResource(id, meshRenderable);
        }

        var cmd = InitializeCommand.Create();
        cmd.GraphNodeId = id;

        var lights = resource.Lights;
        if (lights != null) {
            cmd.LightIds.AddRange(
                lights.Select(light =>
                    ResourceLibrary.Reference(context, id, light)));
            SetParent(context, id, cmd.LightIds);
        }

        var children = resource.Children;
        if (children != null) {
            cmd.ChildrenIds.AddRange(
                children.Select(child =>
                    ResourceLibrary.Reference(context, id, child)));
            SetParent(context, id, cmd.ChildrenIds);
        }

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, GraphNode resource)
    {
        ResourceLibrary.UnreferenceAll(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.GraphNodeId = id;
        context.SendCommandBatched(cmd);
    }
}