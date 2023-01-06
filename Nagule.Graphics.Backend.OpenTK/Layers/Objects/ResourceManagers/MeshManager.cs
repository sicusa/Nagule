namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshManager : ResourceManagerBase<Mesh, MeshData>
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public Guid MeshId;
        public Mesh? Resource;

        public unsafe override void Execute(IContext context)
        {
            ref var data = ref context.Require<MeshData>(MeshId);
            var buffers = data.BufferHandles;

            data.UniformBufferHandle = GL.GenBuffer();
            data.VertexArrayHandle = GL.GenVertexArray();
            data.CullingVertexArrayHandle = GL.GenVertexArray();
            data.CulledQueryHandle = GL.GenQuery();

            GL.GenBuffers(buffers.Raw);

            MeshHelper.InitializeUniformBuffer(in data, Resource!);

            // instancing

            GL.BindVertexArray(data.VertexArrayHandle);
            MeshHelper.InitializeDrawVertexArray(in data, Resource!);

            if (context.TryGet<MeshRenderState>(MeshId, out var state) && state.InstanceCount != 0) {
                var capacity = state.Instances.Length;
                data.InstanceCapacity = capacity;
                MeshHelper.InitializeInstanceBuffer(ref data);

                var srcSpan = state.Instances.AsSpan(state.InstanceCount);
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

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public Guid MeshId;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<MeshData>(MeshId);
            GL.DeleteBuffer(data.UniformBufferHandle);
            GL.DeleteBuffers(data.BufferHandles.Raw);
            GL.DeleteVertexArray(data.VertexArrayHandle);
            GL.DeleteQuery(data.CulledQueryHandle);
        }
    }

    private class SetIsOccluderCommand : Command<SetIsOccluderCommand>
    {
        public Guid MeshId;
        public bool IsOccluder;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<MeshData>(MeshId);
            data.IsOccluder = IsOccluder;
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
            context.SendCommandBatched<RenderTarget>(cmd);
        }
        
        foreach (var id in _removedOccluderQuery.Query(context)) {
            if (context.Contains<Occluder>(id)) {
                continue;
            }
            var cmd = SetIsOccluderCommand.Create();
            cmd.MeshId = id;
            cmd.IsOccluder = false;
            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }

    protected unsafe override void Initialize(
        IContext context, Guid id, Mesh resource, ref MeshData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource, in data);
        }

        var material = resource.Material ?? Material.Default;

        data.PrimitiveType = MeshHelper.Cast(resource.PrimitiveType);
        data.MaterialId = ResourceLibrary<Material>.Reference(context, material, id);
        data.IndexCount = resource.Indices!.Length;
        data.RenderMode = material.RenderMode;

        if (resource.IsOccluder) {
            context.Acquire<Occluder>(id);
        }

        if (context.TryGet<MeshRenderState>(id, out var state) && state.InstanceCount != 0) {
            data.InstanceCapacity = state.InstanceCount;
        }

        var cmd = InitializeCommand.Create();
        cmd.MeshId = id;
        cmd.Resource = resource;
        context.SendCommand<RenderTarget>(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Mesh resource, in MeshData data)
    {
        ResourceLibrary<Material>.Unreference(context, data.MaterialId, id);
        var cmd = UninitializeCommand.Create();
        cmd.MeshId = id;
        context.SendCommand<RenderTarget>(cmd);
    }
}