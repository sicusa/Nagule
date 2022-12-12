namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics;

public struct VariantUniformBuffer : IPooledComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
}