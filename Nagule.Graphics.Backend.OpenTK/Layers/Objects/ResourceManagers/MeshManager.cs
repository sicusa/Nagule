namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshManager : ResourceManagerBase<Mesh, MeshData, MeshResource>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid)> _commandQueue = new();

    protected unsafe override void Initialize(
        IContext context, Guid id, ref Mesh mesh, ref MeshData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in mesh, in data);
        }

        var resource = mesh.Resource;
        var material = resource.Material ?? MaterialResource.Default;

        data.MaterialId = ResourceLibrary<MaterialResource>.Reference<Material>(context, material, id);
        data.IndexCount = resource.Indeces!.Length;
        data.RenderMode = material.RenderMode;

        _commandQueue.Enqueue((true, id));
    }

    protected override void Uninitialize(IContext context, Guid id, in Mesh mesh, in MeshData data)
    {
        ResourceLibrary<MaterialResource>.Unreference(context, data.MaterialId, id);
        _commandQueue.Enqueue((false, id));
    }


    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            ref var data = ref context.Require<MeshData>(id);

            if (commandType) {
                var buffers = data.BufferHandles;
                var resource = context.Inspect<Mesh>(id).Resource;

                data.VertexArrayHandle = GL.GenVertexArray();
                data.CullingVertexArrayHandle = GL.GenVertexArray();
                data.CulledQueryHandle = GL.GenQuery();

                GL.BindVertexArray(data.VertexArrayHandle);
                GL.GenBuffers(buffers.Raw);

                if (resource.Vertices != null) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Vertex]);
                    GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Vertices, BufferUsageARB.StaticDraw);
                    GL.EnableVertexAttribArray(0);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                }
                if (resource.TexCoords != null) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.TexCoord]);
                    GL.BufferData(BufferTargetARB.ArrayBuffer, resource.TexCoords, BufferUsageARB.StaticDraw);
                    GL.EnableVertexAttribArray(1);
                    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
                }
                if (resource.Normals != null) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Normal]);
                    GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Normals, BufferUsageARB.StaticDraw);
                    GL.EnableVertexAttribArray(2);
                    GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
                }
                if (resource.Tangents != null) {
                    GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Tangent]);
                    GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Tangents, BufferUsageARB.StaticDraw);
                    GL.EnableVertexAttribArray(3);
                    GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 0, 0);
                }

                if (resource.Indeces != null) {
                    GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffers[MeshBufferType.Index]);
                    GL.BufferData(BufferTargetARB.ElementArrayBuffer, resource.Indeces, BufferUsageARB.StaticDraw);
                }

                // instancing

                if (context.TryGet<MeshRenderingState>(id, out var state) && state.InstanceCount != 0) {
                    data.InstanceCapacity = state.InstanceCount;
                    InitializeInstanceBuffer(ref data);

                    fixed (MeshInstance* ptr = state.Instances) {
                        var length = state.InstanceCount * MeshInstance.MemorySize;
                        System.Buffer.MemoryCopy(ptr, (void*)data.InstanceBufferPointer, length, length);
                    }
                }
                else {
                    InitializeInstanceBuffer(ref data);
                }

                InitializeInstanceCulling(ref data);

                GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance]);
                GLHelper.EnableMatrix4x4Attributes(4, 1);

                GL.BindVertexArray(VertexArrayHandle.Zero);
            }
            else {
                GL.DeleteBuffers(data.BufferHandles.Raw);
                GL.DeleteVertexArray(data.VertexArrayHandle);
                GL.DeleteQuery(data.CulledQueryHandle);
            }
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

    public static void InitializeInstanceCulling(ref MeshData data)
    {
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.CulledInstance]);
        GL.BufferData(BufferTargetARB.ArrayBuffer, data.InstanceCapacity * MeshInstance.MemorySize, IntPtr.Zero, BufferUsageARB.StreamCopy);
        GLHelper.EnableMatrix4x4Attributes(4, 1);

        GL.BindVertexArray(data.CullingVertexArrayHandle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance]);
        GLHelper.EnableMatrix4x4Attributes(0);
    }
}