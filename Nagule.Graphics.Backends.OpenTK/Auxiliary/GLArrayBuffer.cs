namespace Nagule.Graphics.Backends.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class GLArrayBuffer<T> : IDisposable
    where T : unmanaged
{
    public BufferHandle Handle { get; private set; }

    public int Capacity { get; private set; }
    public int ElementSize { get; } = Unsafe.SizeOf<T>();

    public unsafe ref T this[int index]
        => ref ((T*)_pointer)[index];

    private nint _pointer;

    public GLArrayBuffer(int capacity)
        => EnsureCapacity(capacity, out bool _);
    
    public unsafe Span<T> AsSpan()
        => new((void*)_pointer, Capacity);

    public void EnsureCapacity(int capacity, out bool modified, int copyCount = 0)
    {
        if (capacity == 0) {
            throw new ArgumentException("Capacity of array buffer cannot be 0");
        }

        var prevHandle = Handle;
        int prevCapacity = Capacity;
        var prevPointer = _pointer;

        if (prevCapacity == 0) {
            Capacity = capacity;
        }
        else if (prevCapacity >= capacity) {
            modified = false;
            return;
        }
        else {
            int newCapacity = Math.Max(prevCapacity * 2, 6);
            while (newCapacity < capacity) { newCapacity *= 2; }
            Capacity = newCapacity;
        }

        int prevBoundHandle = 0;
        GL.GetInteger(GetPName.ArrayBufferBinding, ref prevBoundHandle);

        Handle = new(GL.GenBuffer());
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, Handle.Handle);
        _pointer = GLUtils.InitializeBuffer(BufferTargetARB.ArrayBuffer, Capacity * ElementSize);

        if (prevHandle != BufferHandle.Zero && copyCount != 0) {
            unsafe {
                new Span<T>((void*)prevPointer, Math.Min(copyCount, prevCapacity)).CopyTo(AsSpan());
            }
            GL.DeleteBuffer(prevHandle.Handle);
        }

        GL.BindBuffer(BufferTargetARB.ArrayBuffer, prevBoundHandle);
        modified = true;
    }

    ~GLArrayBuffer()
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