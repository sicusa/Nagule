namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshManager : ResourceManagerBase<Mesh>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid MeshId;
        public Mesh? Resource;
        public Guid MaterialId;
        public RenderMode RenderMode;

        public override Guid? Id => MeshId;

        public unsafe override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<MeshData>(MeshId, out bool exists);
            var buffers = data.BufferHandles;

            data.PrimitiveType = MeshHelper.Cast(Resource!.PrimitiveType);
            data.IndexCount = Resource.Indices!.Length;
            data.IsOccluder = Resource.IsOccluder;

            data.MaterialId = MaterialId;
            data.RenderMode = RenderMode;

            data.UniformBufferHandle = GL.GenBuffer();
            data.VertexArrayHandle = GL.GenVertexArray();
            data.CullingVertexArrayHandle = GL.GenVertexArray();
            data.CulledQueryHandle = GL.GenQuery();

            GL.GenBuffers(buffers.Raw);

            MeshHelper.InitializeUniformBuffer(in data, Resource!);

            // instancing

            GL.BindVertexArray(data.VertexArrayHandle);
            MeshHelper.InitializeDrawVertexArray(in data, Resource!);

            if (context.TryGet<MeshRenderState>(MeshId, out var state)
                    && state.InstanceCount != 0) {
                var capacity = state.InstanceCount;
                data.InstanceCapacity = capacity;
                MeshHelper.InitializeInstanceBuffer(ref data);

                var srcSpan = state.Instances.AsSpan(0, state.InstanceCount);
                var dstSpan = new Span<MeshInstance>((void*)data.InstanceBufferPointer, capacity);
                srcSpan.CopyTo(dstSpan);
            }
            else {
                MeshHelper.InitializeInstanceBuffer(ref data);
            }
            MeshHelper.InitializeInstanceCulling(in data);

            GL.BindVertexArray(VertexArrayHandle.Zero);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid MeshId;

        public override void Execute(ICommandContext context)
        {
            if (!context.Remove<MeshData>(MeshId, out var data)) {
                return;
            }
            GL.DeleteBuffer(data.UniformBufferHandle);
            GL.DeleteBuffers(data.BufferHandles.Raw);
            GL.DeleteVertexArray(data.VertexArrayHandle);
            GL.DeleteQuery(data.CulledQueryHandle);
        }
    }

    private class SetIsOccluderCommand : Command<SetIsOccluderCommand, RenderTarget>
    {
        public Guid MeshId;
        public bool IsOccluder;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Require<MeshData>(MeshId);
            data.IsOccluder = IsOccluder;

            if (IsOccluder) {
                context.Acquire<Occluder>(MeshId);
            }
            else {
                context.Remove<Occluder>(MeshId);
            }
        }
    }

    private Query<Modified<Occluder>, Occluder, Resource<Mesh>> _modifiedOccluderQuery = new();
    private Query<Removed<Occluder>, Resource<Mesh>> _removedOccluderQuery = new();

    public override void OnUpdate(IContext context)
    {
        base.OnUpdate(context);

        foreach (var id in _modifiedOccluderQuery.Query(context)) {
            var cmd = SetIsOccluderCommand.Create();
            cmd.MeshId = id;
            cmd.IsOccluder = true;
            context.SendCommandBatched(cmd);
        }
        
        foreach (var id in _removedOccluderQuery.Query(context)) {
            if (context.Contains<Occluder>(id)) {
                continue;
            }
            var cmd = SetIsOccluderCommand.Create();
            cmd.MeshId = id;
            cmd.IsOccluder = false;
            context.SendCommandBatched(cmd);
        }
    }

    protected unsafe override void Initialize(
        IContext context, Guid id, Mesh resource, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource);
        }

        if (resource.IsOccluder) {
            context.Acquire<Occluder>(id);
        }

        var cmd = InitializeCommand.Create();
        cmd.MeshId = id;
        cmd.Resource = resource;

        var material = resource.Material ?? Material.Default;
        cmd.MaterialId = ResourceLibrary<Material>.Reference(context, id, material);
        cmd.RenderMode = material.RenderMode;

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Mesh resource)
    {
        ResourceLibrary<Material>.UnreferenceAll(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.MeshId = id;
        context.SendCommandBatched(cmd);
    }
}