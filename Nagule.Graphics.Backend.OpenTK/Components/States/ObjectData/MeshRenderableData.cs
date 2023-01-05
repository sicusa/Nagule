namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct MeshRenderableData : IPooledComponent
{
    public Dictionary<Guid, int> Entries = new();
    public BufferHandle VariantBufferHandle = BufferHandle.Zero;
    public IntPtr VariantBufferPointer = IntPtr.Zero;

    public MeshRenderableData() {}
}