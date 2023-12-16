namespace Nagule.Graphics.Backend.OpenTK;

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

public sealed class Mesh3DBufferState(object key, BufferHandle handle)
{
    public object Key = key;
    public BufferHandle Handle = handle;
    public int RefCount = 1;
}

public sealed class Mesh3DDataState(Mesh3DData data, BufferHandle handle)
{
    public bool Visible;
    public Mesh3DData Key = data;
    public BufferHandle UniformBufferHandle = handle;

    public GLPrimitiveType PrimitiveType = GLUtils.Cast(data.PrimitiveType);
    public int IndexCount = data.Indices.Length;
    public Rectangle BoundingBox = data.BoundingBox ?? ModelUtils.CalculateBoundingBox(data.Vertices.AsSpan());
    public readonly EnumDictionary<Mesh3DBufferType, Mesh3DBufferState> BufferEntries = new();

    public int RefCount = 1;

    public uint EnableVertexAttribArrays()
    {
        var buffer = BufferEntries[Mesh3DBufferType.Vertices].Handle;
        if (buffer != BufferHandle.Zero) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = BufferEntries[Mesh3DBufferType.TexCoords].Handle;
        if (buffer != BufferHandle.Zero) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = BufferEntries[Mesh3DBufferType.Normals].Handle;
        if (buffer != BufferHandle.Zero) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = BufferEntries[Mesh3DBufferType.Tangents].Handle;
        if (buffer != BufferHandle.Zero) {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer.Handle);
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 0, 0);
        }

        buffer = BufferEntries[Mesh3DBufferType.Indices].Handle;
        if (buffer != BufferHandle.Zero) {
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffer.Handle);
        }

        return 4;
    }
}

public record struct Mesh3DState
{
    public EntityRef MaterialEntity;
    public Mesh3DDataState DataEntry;
}