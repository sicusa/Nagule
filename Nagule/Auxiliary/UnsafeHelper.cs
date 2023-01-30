namespace Nagule;

using System.Reactive.Disposables;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Buffers;

public unsafe static class UnsafeHelper
{
    public static readonly int IntPtrSize = Marshal.SizeOf(typeof(IntPtr));

    public static IDisposable CreateRawStringArray(IReadOnlyCollection<string> c, out byte** result)
        => CreateRawStringArray(c, c.Count, out result);

    public static IDisposable CreateRawStringArray(IEnumerable<string> e, int count, out byte** result)
    {
        var arrPtr = Marshal.AllocHGlobal(IntPtrSize * count);
        var elementPtrs = MemoryOwner<IntPtr>.Allocate(count);
        var elementPtrsSpan = elementPtrs.Span;

        int n = 0;
        foreach (var str in e) {
            var byteArrPtr = Marshal.StringToHGlobalAnsi(str);
            Marshal.WriteIntPtr(arrPtr, n * IntPtrSize, byteArrPtr);
            elementPtrsSpan[n] = byteArrPtr;
            ++n;
        }

        result = (byte**)arrPtr;
        return CreateRawArrayDisposable(arrPtr, elementPtrs);
    }

    public static IDisposable CreateRawStringArray(ReadOnlySpan<string> span, out byte** result)
    {
        var arrPtr = Marshal.AllocHGlobal(IntPtrSize * span.Length);
        var elementPtrs = MemoryOwner<IntPtr>.Allocate(span.Length);
        var elementPtrsSpan = elementPtrs.Span;

        int n = 0;
        foreach (var str in span) {
            var byteArrPtr = Marshal.StringToHGlobalAnsi(str);
            Marshal.WriteIntPtr(arrPtr, n * IntPtrSize, byteArrPtr);
            elementPtrsSpan[n] = byteArrPtr;
            ++n;
        }

        result = (byte**)arrPtr;
        return CreateRawArrayDisposable(arrPtr, elementPtrs);
    }

    private static IDisposable CreateRawArrayDisposable(IntPtr arrPtr, MemoryOwner<IntPtr> elementPtrs)
        => Disposable.Create(() => {
            Marshal.FreeHGlobal(arrPtr);
            foreach (var ptr in elementPtrs.Span) {
                Marshal.FreeHGlobal(ptr);
            }
            elementPtrs.Dispose();
        });

    // From https://stackoverflow.com/questions/6889614/how-to-pin-a-pointer-to-managed-object-in-c
    public static ref byte GetRawObjectData(object o)
        => ref new PinnableUnion(o).Pinnable.Data;

    [StructLayout(LayoutKind.Sequential)]
    private sealed class Pinnable
    {
        public byte Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PinnableUnion
    {
        [FieldOffset(0)]
        public object Object;

        [FieldOffset(0)]
        public Pinnable Pinnable;

        public PinnableUnion(object o)
        {
            System.Runtime.CompilerServices.Unsafe.SkipInit(out this);
            Object = o;
        }
    }
}