namespace Nagule.Graphics.Backend.OpenTK;

public struct LightsBuffer : ISingletonComponent
{
    public const int InitialCapacity = 32;

    public int Capacity;

    public BufferHandle Handle;
    public TextureHandle TexHandle;
    public IntPtr Pointer;

    public LightParameters[] Parameters;
}