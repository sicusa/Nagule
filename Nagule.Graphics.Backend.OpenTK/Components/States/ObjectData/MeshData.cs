namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

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
    public VertexArrayHandle VertexArrayHandle = new VertexArrayHandle(-1);
    public VertexArrayHandle CullingVertexArrayHandle = new VertexArrayHandle(-1);
    public QueryHandle CulledQueryHandle = new QueryHandle(-1);
    public readonly EnumArray<MeshBufferType, BufferHandle> BufferHandles = new();
    public IntPtr InstanceBufferPointer = IntPtr.Zero;
    public Guid MaterialId = Guid.Empty;
    
    public int InstanceCapacity = 1;

    public MeshData() {}
}