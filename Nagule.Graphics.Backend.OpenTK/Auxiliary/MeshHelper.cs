namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using PrimitiveType = Nagule.Graphics.PrimitiveType;
using GLPrimitiveType = global::OpenTK.Graphics.OpenGL.PrimitiveType;

public static class MeshHelper
{
    public static void InitializeUniformBuffer(in MeshData data, Mesh resource)
    {
        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 2 * 16, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, data.UniformBufferHandle);

        var boundingBox = resource.BoundingBox;
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 12, boundingBox.Min);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 16, 12, boundingBox.Max);
    }

    public static void InitializeDrawVertexArray(in MeshData data, Mesh resource)
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
        if (resource.Bitangents.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffers[MeshBufferType.Bitangent]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, resource.Tangents.AsSpan(), BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 0, 0);
        }
        if (resource.Indices.Length != 0) {
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffers[MeshBufferType.Index]);
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, resource.Indices.AsSpan(), BufferUsageARB.StaticDraw);
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
        GLHelper.EnableMatrix4x4Attributes(5, 1);

        GL.BindVertexArray(data.CullingVertexArrayHandle);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, data.BufferHandles[MeshBufferType.Instance]);
        GLHelper.EnableMatrix4x4Attributes(5);
    }

    public static void EnsureBufferCapacity(ref MeshData meshData, int requiredCapacity)
    {
        int prevCapacity = meshData.InstanceCapacity;
        if (prevCapacity >= requiredCapacity) { return; }

        int newCapacity = prevCapacity * 2;
        while (newCapacity < requiredCapacity) { newCapacity *= 2; }
        meshData.InstanceCapacity = newCapacity;

        var newBuffer = GL.GenBuffer();
        MeshHelper.InitializeInstanceBuffer(BufferTargetARB.ArrayBuffer, newBuffer, ref meshData);

        var instanceBufferHandle = meshData.BufferHandles[MeshBufferType.Instance];
        GL.BindBuffer(BufferTargetARB.CopyReadBuffer, instanceBufferHandle);
        GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.ArrayBuffer,
            IntPtr.Zero, IntPtr.Zero, prevCapacity * MeshInstance.MemorySize);

        GL.BindBuffer(BufferTargetARB.CopyReadBuffer, BufferHandle.Zero);
        GL.DeleteBuffer(instanceBufferHandle);

        GL.BindVertexArray(meshData.VertexArrayHandle);
        MeshHelper.InitializeInstanceCulling(in meshData);
        GL.BindVertexArray(VertexArrayHandle.Zero);
    }

    public static GLPrimitiveType Cast(PrimitiveType type)
        => type switch {
            PrimitiveType.Point => GLPrimitiveType.Points,
            PrimitiveType.Line => GLPrimitiveType.Lines,
            PrimitiveType.Triangle => GLPrimitiveType.Triangles,
            PrimitiveType.Polygon => GLPrimitiveType.Polygon,
            _ => GLPrimitiveType.Triangles
        };
}