namespace Nagule.Graphics.Backends.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class GLPersistentArrayBuffer<T> : IDisposable
    where T : unmanaged
{
    public BufferHandle Handle { get; private set; }

    public int Capacity { get; private set; }
    public int ElementSize { get; } = Unsafe.SizeOf<T>();

    public unsafe ref T this[int index]
        => ref ((T*)_pointer)[index];

    private nint _pointer;

    public GLPersistentArrayBuffer(int capacity)
        => EnsureCapacity(capacity, out bool _);
    
    public unsafe Span<T> AsSpan()
        => new((void*)_pointer, Capacity);

    public void EnsureCapacity(int capacity, out bool modified)
    {
        if (capacity == 0) {
            throw new ArgumentException("Capacity of array buffer cannot be 0");
        }
        if (Capacity == 0) {
            Capacity = capacity;
        }
        else {
            int prevCapacity = Capacity;
            if (prevCapacity >= capacity) {
                modified = false;
                return;
            }

            int newCapacity = Math.Max(prevCapacity * 2, 6);
            while (newCapacity < capacity) { newCapacity *= 2; }
            Capacity = newCapacity;
        }

        if (Handle != BufferHandle.Zero) {
            GL.DeleteBuffer(Handle.Handle);
        }

        Handle = new(GL.GenBuffer());

        int prevHandle = 0;
        GL.GetInteger(GetPName.ArrayBufferBinding, ref prevHandle);

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, Handle.Handle);
        _pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * ElementSize);

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, prevHandle);
        modified = true;
    }

    ~GLPersistentArrayBuffer()
    {
        if (_pointer == 0) {
            return;
        }
        var world = Context<World>.Current;
        world?.GetAddon<RenderFramer>().Start(DoDispose);
    }

    public void Dispose()
    {
        if (_pointer == 0) {
            return;
        }
        DoDispose();
        GC.SuppressFinalize(this);
    }

    private void DoDispose()
    {
        GL.DeleteBuffer(Handle.Handle);
        Handle = BufferHandle.Zero;
        _pointer = 0;
    }
}