namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

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

            MeshHelper.InitializeUniformBuffer(in data, Resource!);

            GL.BindVertexArray(data.VertexArrayHandle);
            GL.GenBuffers(buffers.Raw);

            MeshHelper.InitializeDrawVertexArray(in data, Resource!);

            // instancing

            if (context.TryGet<MeshRenderState>(MeshId, out var state)) {
                int instanceCount = state.InstanceCount;
                if (instanceCount != 0) {
                    var capacity = state.Instances.Length;
                    data.InstanceCapacity = capacity;
                    MeshHelper.InitializeInstanceBuffer(ref data);

                    var srcSpan = state.Instances.AsSpan();
                    var dstSpan = new Span<MeshInstance>((void*)data.InstanceBufferPointer, capacity);
                    srcSpan.CopyTo(dstSpan);
                }
                else {
                    MeshHelper.InitializeInstanceBuffer(ref data);
                }
            }
            else {
                MeshHelper.InitializeInstanceBuffer(ref data);
            }

            MeshHelper.InitializeInstanceCulling(in data);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance]);
            GLHelper.EnableMatrix4x4Attributes(4, 1);

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

        var cmd = Command<InitializeCommand>.Create();
        cmd.MeshId = id;
        cmd.Resource = resource;
        context.SendCommand<RenderTarget>(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Mesh resource, in MeshData data)
    {
        ResourceLibrary<Material>.Unreference(context, data.MaterialId, id);
        var cmd = Command<UninitializeCommand>.Create();
        cmd.MeshId = id;
        context.SendCommand<RenderTarget>(cmd);
    }
}