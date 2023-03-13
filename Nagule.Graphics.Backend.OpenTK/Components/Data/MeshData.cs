namespace Nagule.Graphics.Backend.OpenTK;

using GLPrimitiveType = global::OpenTK.Graphics.OpenGL.PrimitiveType;

using Aeco;

public enum MeshBufferType
{
    Index,
    Vertex,
    TexCoord,
    Normal,
    Tangent,
    Bitangent,
    Instance,
    CulledInstance
}

public struct MeshData : IHashComponent
{
    public bool IsOccluder = false;
    public int IndexCount = 0;
    public BufferHandle UniformBufferHandle = BufferHandle.Zero;
    public VertexArrayHandle VertexArrayHandle = VertexArrayHandle.Zero;
    public VertexArrayHandle CullingVertexArrayHandle = VertexArrayHandle.Zero;
    public QueryHandle CulledQueryHandle = QueryHandle.Zero;
    public readonly EnumArray<MeshBufferType, BufferHandle> BufferHandles = new();
    public IntPtr InstanceBufferPointer = IntPtr.Zero;
    public uint MaterialId = 0;
    public GLPrimitiveType PrimitiveType = GLPrimitiveType.Triangles;
    
    public int InstanceCapacity = 1;

    public MeshData() {}
}