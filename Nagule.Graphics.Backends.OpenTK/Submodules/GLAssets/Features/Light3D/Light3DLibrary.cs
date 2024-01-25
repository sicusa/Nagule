namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using Sia;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Light3DParameters
{
    public static readonly int MemorySize = Marshal.SizeOf<Light3DParameters>();

    public float Type;

    public Vector4 Color;
    public Vector3 Position;
    public float Range;
    public Vector3 Direction;

    public float InnerConeAngle;
    public float OuterConeAngle;

    public float ShadowMapIndex;
    public float ShadowMapStrength;
}

public class Light3DLibrary : IAddon
{
    public const int InitialCapacity = 32;

    public IReadOnlyList<EntityRef> Entities => _entities;

    public int Count { get; private set; }
    public int Capacity { get; private set; } = InitialCapacity;

    public BufferHandle Handle { get; private set; }
    public TextureHandle TextureHandle { get; private set; }
    public IntPtr Pointer { get; private set; }

    public Light3DParameters[] Parameters { get; private set; } = new Light3DParameters[InitialCapacity];

    private readonly List<EntityRef> _entities = [];

    public void OnInitialize(World world)
        => world.GetAddon<RenderFramer>().Start(Load);
    
    public void OnUninitialize(World world)
        => world.GetAddon<RenderFramer>().Start(Unload);

    private bool Load()
    {
        Handle = new(GL.GenBuffer());
        GL.BindBuffer(BufferTargetARB.TextureBuffer, Handle.Handle);

        Pointer = GLUtils.InitializeBuffer(BufferTargetARB.TextureBuffer, Capacity * Light3DParameters.MemorySize);
        TextureHandle = new(GL.GenTexture());

        GL.BindTexture(TextureTarget.TextureBuffer, TextureHandle.Handle);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, Handle.Handle);

        GL.BindBuffer(BufferTargetARB.TextureBuffer, 0);
        GL.BindTexture(TextureTarget.TextureBuffer, 0);
        return true;
    }

    private bool Unload()
    {
        GL.DeleteBuffer(Handle.Handle);
        GL.DeleteTexture(TextureHandle.Handle);
        return true;
    }

    public int Add(EntityRef entity, in Light3DParameters pars)
    {
        int index = Count;
        Count++;
        EnsureCapacity(Count);

        Parameters[index] = pars;
        GetBufferData(index) = pars;

        _entities.Add(entity);
        return index;
    }

    public void Remove(int index)
    {
        Count--;
        ref var lastPars = ref Parameters[Count];
        Parameters[index] = lastPars;
        GetBufferData(index) = lastPars;
        _entities[index] = _entities[Count];
        _entities.RemoveAt(Count);
    }

    public unsafe ref Light3DParameters GetBufferData(int lightIndex)
        => ref ((Light3DParameters*)Pointer)[lightIndex];

    public unsafe void EnsureCapacity(int capacity)
    {
        int prevCapacity = Capacity;
        if (prevCapacity >= capacity) { return; }

        int newCapacity = prevCapacity * 2;
        while (newCapacity < capacity) newCapacity *= 2;

        var prevPars = Parameters;
        Parameters = new Light3DParameters[newCapacity];
        Array.Copy(prevPars, Parameters, Capacity);

        var newBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.TextureBuffer, newBuffer);
        var pointer = GLUtils.InitializeBuffer(BufferTargetARB.TextureBuffer, newCapacity * Light3DParameters.MemorySize);

        prevPars.CopyTo(new Span<Light3DParameters>((void*)pointer, Capacity));

        var newTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, newTex);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, newBuffer);

        GL.DeleteTexture(TextureHandle.Handle);
        GL.DeleteBuffer(Handle.Handle);

        Capacity = newCapacity;
        Handle = new(newBuffer);
        TextureHandle = new(newTex);
        Pointer = pointer;

        GL.BindTexture(TextureTarget.TextureBuffer, 0);
        GL.BindBuffer(BufferTargetARB.TextureBuffer, 0);
        GL.BindBuffer(BufferTargetARB.CopyReadBuffer, 0);
    }
}