namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
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
}

public class Light3DLibrary : IAddon
{
    public const int InitialCapacity = 32;

    public IReadOnlyList<EntityRef> States => _states;

    public int Count => _states.Count;
    public int Capacity => ParametersBuffer.Capacity;

    [AllowNull] public GLArrayBuffer<Light3DParameters> ParametersBuffer { get; private set; }
    public Light3DParameters[] Parameters { get; private set; } = new Light3DParameters[InitialCapacity];
    public TextureHandle TextureHandle { get; private set; }

    private readonly List<EntityRef> _states = [];

    public void OnInitialize(World world)
        => world.GetAddon<RenderFramer>().Start(Load);
    
    public void OnUninitialize(World world)
        => world.GetAddon<RenderFramer>().Start(Unload);

    private bool Load()
    {
        ParametersBuffer = new(InitialCapacity);
        TextureHandle = new(GL.GenTexture());

        GL.BindTexture(TextureTarget.TextureBuffer, TextureHandle.Handle);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, ParametersBuffer.Handle.Handle);

        GL.BindBuffer(BufferTargetARB.TextureBuffer, 0);
        GL.BindTexture(TextureTarget.TextureBuffer, 0);

        return true;
    }

    private bool Unload()
    {
        ParametersBuffer.Dispose();
        GL.DeleteTexture(TextureHandle.Handle);
        return true;
    }

    public int Add(EntityRef stateEntity, in Light3DParameters pars)
    {
        int newIndex = Count;
        _states.Add(stateEntity);

        EnsureCapacity(newIndex + 1);
        Parameters[newIndex] = pars;
        ParametersBuffer[newIndex] = pars;
        return newIndex;
    }

    public void Remove(int index)
    {
        var lastIndex = Count - 1;
        if (index == lastIndex) {
            _states.RemoveAt(lastIndex);
            return;
        }

        ref var lastPars = ref Parameters[lastIndex];
        Parameters[index] = lastPars;
        ParametersBuffer[index] = lastPars;

        var lastState = _states[lastIndex];
        _states[index] = lastState;
        _states.RemoveAt(lastIndex);

        lastState.Get<Light3DState>().Index = index;
    }

    private unsafe void EnsureCapacity(int capacity)
    {
        ParametersBuffer.EnsureCapacity(capacity, out bool modified);
        if (!modified) {
            return;
        }

        var prevPars = Parameters;
        Parameters = new Light3DParameters[ParametersBuffer.Capacity];
        Array.Copy(prevPars, Parameters, prevPars.Length);
        prevPars.CopyTo(ParametersBuffer.AsSpan());

        var newTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, newTex);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, ParametersBuffer.Handle.Handle);

        GL.DeleteTexture(TextureHandle.Handle);
        TextureHandle = new(newTex);

        GL.BindTexture(TextureTarget.TextureBuffer, 0);
    }
}