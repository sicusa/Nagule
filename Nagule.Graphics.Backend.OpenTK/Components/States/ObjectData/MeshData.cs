namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

public enum MeshBufferType
{
    Index,
    Vertex,
    TexCoord,
    Normal,
    Tangent,
    Instance,
    CulledInstance
}

public struct MeshData : IPooledComponent
{
    public RenderMode RenderMode = RenderMode.Opaque;
    public int IndexCount = 0;
    public BufferHandle UniformBufferHandle = BufferHandle.Zero;
    public VertexArrayHandle VertexArrayHandle = VertexArrayHandle.Zero;
    public VertexArrayHandle CullingVertexArrayHandle = VertexArrayHandle.Zero;
    public QueryHandle CulledQueryHandle = QueryHandle.Zero;
    public readonly EnumArray<MeshBufferType, BufferHandle> BufferHandles = new();
    public IntPtr InstanceBufferPointer = IntPtr.Zero;
    public Guid MaterialId = Guid.Empty;
    public PrimitiveType PrimitiveType = PrimitiveType.Triangles;
    
    public int InstanceCapacity = 1;

    public MeshData() {}
}