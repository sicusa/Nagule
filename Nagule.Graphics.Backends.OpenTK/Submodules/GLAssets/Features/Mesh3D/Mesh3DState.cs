namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public enum Mesh3DBufferType : int
{
    Indices,
    Vertices,
    TexCoords,
    Normals,
    Tangents,
    Bitangents
}

public sealed class Mesh3DSubBuffer(object key, BufferHandle handle)
{
    public object Key = key;
    public BufferHandle Handle = handle;
    public int RefCount = 1;
}

public sealed class Mesh3DDataBuffer(Mesh3DData data, BufferHandle handle)
{
    public bool Visible;
    public Mesh3DData Key { get; } = data;
    public BufferHandle Handle { get; } = handle;

    public GLPrimitiveType PrimitiveType { get; }  = GLUtils.Convert(data.PrimitiveType);
    public int IndexCount { get; } = data.Indices.Length;
    public AABB BoundingBox { get; } = data.BoundingBox ?? ModelUtils.CalculateBoundingBox(data.Vertices.AsSpan());
    public EnumDictionary<Mesh3DBufferType, Mesh3DSubBuffer> SubBuffers { get; } = new();

    internal int RefCount = 1;

    public uint EnableVertexAttribArrays()
    {
        var buffer = SubBuffers[Mesh3DBufferType.Vertices];
        if (buffer != null) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle.Handle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = SubBuffers[Mesh3DBufferType.TexCoords];
        if (buffer != null) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle.Handle);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = SubBuffers[Mesh3DBufferType.Normals];
        if (buffer != null) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle.Handle);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = SubBuffers[Mesh3DBufferType.Tangents];
        if (buffer != null) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle.Handle);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = SubBuffers[Mesh3DBufferType.Indices];
        if (buffer != null) {
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffer.Handle.Handle);
        }

        return 4;
    }
}

public record struct Mesh3DState : IAssetState
{
    public readonly bool Loaded => DataBuffer != null;

    public Mesh3DDataBuffer DataBuffer;
    public EntityRef MaterialEntity;
}