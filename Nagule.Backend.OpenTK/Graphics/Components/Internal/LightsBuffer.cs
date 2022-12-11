namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics;

public struct LightsBuffer : ISingletonComponent
{
    public const int InitialCapacity = 512;

    public int Capacity;

    public BufferHandle Handle;
    public TextureHandle TexHandle;
    public IntPtr Pointer;

    public LightParameters[] Parameters;
}