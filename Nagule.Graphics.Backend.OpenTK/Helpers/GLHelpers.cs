namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;

using global::OpenTK.Graphics.OpenGL;

public static class GLHelper
{
    public static unsafe IntPtr InitializeBuffer(BufferTargetARB target, int length)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            GL.BufferData(target, length, IntPtr.Zero, BufferUsageARB.DynamicDraw);
        }
        else {
            GL.BufferStorage((BufferStorageTarget)target, length, IntPtr.Zero,
                BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
        }
        return (IntPtr)GL.MapBuffer(target, BufferAccessARB.WriteOnly);
    }
}