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

            InitializeUniformBuffer(in data, Resource!);

            GL.BindVertexArray(data.VertexArrayHandle);
            GL.GenBuffers(buffers.Raw);

            InitializeDrawVertexArray(in data, Resource!);

            // instancing

            if (context.TryGet<MeshRenderState>(MeshId, out var state)) {
                int instanceCount = state.InstanceCount;
                if (instanceCount != 0) {
                    var capacity = state.Instances.Length;
                    data.InstanceCapacity = capacity;
                    InitializeInstanceBuffer(ref data);

                    var srcSpan = state.Instances.AsSpan();
                    var dstSpan = new Span<MeshInstance>((void*)data.InstanceBufferPointer, capacity);
                    srcSpan.CopyTo(dstSpan);
                }
                else {
                    InitializeInstanceBuffer(ref data);
                }
            }
            else {
                InitializeInstanceBuffer(ref data);
            }

            InitializeInstanceCulling(in data);

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

        data.MaterialId = ResourceLibrary<Material>.Reference(context, material, id);
        data.IndexCount = resource.Indeces!.Length;
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

    private static void InitializeUniformBuffer(in MeshData data, Mesh resource)
    {
        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 2 * 16, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, data.UniformBufferHandle);

        var boundingBox = resource.BoundingBox;
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 12, boundingBox.Min);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 16, 12, boundingBox.Max);
    }

    private static void InitializeDrawVertexArray(in MeshData data, Mesh resource)
    {
        var buffers = data.BufferHandles;

        if (resource.Vertices.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Vertex]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Vertices.AsSpan(), BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        }
        if (resource.TexCoords.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.TexCoord]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, resource.TexCoords.AsSpan(), BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
        }
        if (resource.Normals.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Normal]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Normals.AsSpan(), BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
        }
        if (resource.Tangents.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Tangent]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Tangents.AsSpan(), BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        if (resource.Indeces.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffers[MeshBufferType.Index]);
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, resource.Indeces.AsSpan(), BufferUsageARB.StaticDraw);
        }
    }

    public static void InitializeInstanceBuffer(ref MeshData data)
        => InitializeInstanceBuffer(
            BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance], ref data);

    public static void InitializeInstanceBuffer(BufferTargetARB target, BufferHandle handle, ref MeshData data)
    {
        GL.BindBuffer(target, handle);
        data.InstanceBufferPointer = GLHelper.InitializeBuffer(
            target, data.InstanceCapacity * MeshInstance.MemorySize);
    }

    public static void InitializeInstanceCulling(in MeshData data)
    {
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BufferData(BufferTargetARB.ArrayBuffer, data.InstanceCapacity * MeshInstance.MemorySize, IntPtr.Zero, BufferUsageARB.StreamCopy);
        GLHelper.EnableMatrix4x4Attributes(4, 1);

        GL.BindVertexArray(data.CullingVertexArrayHandle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance]);
        GLHelper.EnableMatrix4x4Attributes(0);
    }
}