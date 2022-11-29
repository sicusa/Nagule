namespace Nagule.Backend.OpenTK.Graphics;

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

public struct MeshData : Nagule.IPooledComponent
{
    public bool IsTransparent = false;
    public int IndexCount = 0;
    public int VertexArrayHandle = -1;
    public int CullingVertexArrayHandle = -1;
    public int CulledQueryHandle = -1;
    public readonly EnumArray<MeshBufferType, int> BufferHandles = new();
    public IntPtr InstanceBufferPointer = IntPtr.Zero;
    public Guid MaterialId = Guid.Empty;
    public int InstanceCapacity = 1;

    public MeshData() {}
}