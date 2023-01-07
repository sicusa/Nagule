namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

using PixelFormat = Nagule.Graphics.PixelFormat;

internal unsafe static class GLHelper
{
    private static readonly EnumArray<ShaderParameterType, Action<int, object>> s_uniformSetters = new() {
        [ShaderParameterType.Int] = (location, value) => GL.Uniform1i(location, (int)value),
        [ShaderParameterType.IntArray] = (location, value) => {
            var arr = (int[])value;
            GL.Uniform1i(location, arr.Length, arr);
        },
        [ShaderParameterType.UnsignedInt] = (location, value) => GL.Uniform1ui(location, (uint)value),
        [ShaderParameterType.UnsignedIntArray] = (location, value) => {
            var arr = (uint[])value;
            GL.Uniform1uiv(location, arr.Length, (uint*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.Float] = (location, value) => GL.Uniform1f(location, (float)value),
        [ShaderParameterType.FloatArray] = (location, value) => {
            var arr = (float[])value;
            GL.Uniform1f(location, arr.Length, arr);
        },
        [ShaderParameterType.Double] = (location, value) => GL.Uniform1d(location, (double)value),
        [ShaderParameterType.DoubleArray] = (location, value) => {
            var arr = (double[])value;
            GL.Uniform1d(location, arr.Length, arr);
        },

        [ShaderParameterType.Vector2] = (location, value) => {
            var vec = (Vector2)value;
            GL.Uniform2f(location, vec.X, vec.Y);
        },
        [ShaderParameterType.Vector2Array] = (location, value) => {
            var arr = (Vector2[])value;
            GL.Uniform2fv(location, arr.Length, (float*)Unsafe.AsPointer(ref arr[0].X));
        },
        [ShaderParameterType.Vector3] = (location, value) => {
            var vec = (Vector3)value;
            GL.Uniform3f(location, vec.X, vec.Y, vec.Z);
        },
        [ShaderParameterType.Vector3Array] = (location, value) => {
            var arr = (Vector3[])value;
            GL.Uniform3fv(location, arr.Length, (float*)Unsafe.AsPointer(ref arr[0].X));
        },
        [ShaderParameterType.Vector4] = (location, value) => {
            var vec = (Vector4)value;
            GL.Uniform4f(location, vec.X, vec.Y, vec.Z, vec.W);
        },
        [ShaderParameterType.Vector4Array] = (location, value) => {
            var arr = (Vector4[])value;
            GL.Uniform4fv(location, arr.Length, (float*)Unsafe.AsPointer(ref arr[0].X));
        },

        [ShaderParameterType.DoubleVector2] = (location, value) => {
            var vec = (double[])value;
            GL.Uniform2d(location, vec[0], vec[1]);
        },
        [ShaderParameterType.DoubleVector2Array] = (location, value) => {
            var arr = (double[])value;
            GL.Uniform2dv(location, arr.Length / 2, (double*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.DoubleVector3] = (location, value) => {
            var vec = (double[])value;
            GL.Uniform3d(location, vec[0], vec[1], vec[2]);
        },
        [ShaderParameterType.DoubleVector3Array] = (location, value) => {
            var arr = (double[])value;
            GL.Uniform3dv(location, arr.Length / 3, (double*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.DoubleVector4] = (location, value) => {
            var vec = (double[])value;
            GL.Uniform4d(location, vec[0], vec[1], vec[2], vec[3]);
        },
        [ShaderParameterType.DoubleVector4Array] = (location, value) => {
            var arr = (double[])value;
            GL.Uniform4dv(location, arr.Length / 4, (double*)Unsafe.AsPointer(ref arr[0]));
        },

        [ShaderParameterType.IntVector2] = (location, value) => {
            var vec = (int[])value;
            GL.Uniform2i(location, vec[0], vec[1]);
        },
        [ShaderParameterType.IntVector2Array] = (location, value) => {
            var arr = (int[])value;
            GL.Uniform2iv(location, arr.Length / 2, (int*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.IntVector3] = (location, value) => {
            var vec = (int[])value;
            GL.Uniform3i(location, vec[0], vec[1], vec[2]);
        },
        [ShaderParameterType.IntVector3Array] = (location, value) => {
            var arr = (int[])value;
            GL.Uniform3iv(location, arr.Length / 3, (int*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.IntVector4] = (location, value) => {
            var vec = (int[])value;
            GL.Uniform4i(location, vec[0], vec[1], vec[2], vec[3]);
        },
        [ShaderParameterType.IntVector4Array] = (location, value) => {
            var arr = (int[])value;
            GL.Uniform4iv(location, arr.Length / 4, (int*)Unsafe.AsPointer(ref arr[0]));
        },

        [ShaderParameterType.UnsignedIntVector2] = (location, value) => {
            var vec = (uint[])value;
            GL.Uniform2ui(location, vec[0], vec[1]);
        },
        [ShaderParameterType.UnsignedIntVector2Array] = (location, value) => {
            var arr = (uint[])value;
            GL.Uniform2uiv(location, arr.Length / 2, (uint*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.UnsignedIntVector3] = (location, value) => {
            var vec = (uint[])value;
            GL.Uniform3ui(location, vec[0], vec[1], vec[2]);
        },
        [ShaderParameterType.UnsignedIntVector3Array] = (location, value) => {
            var arr = (uint[])value;
            GL.Uniform3uiv(location, arr.Length / 3, (uint*)Unsafe.AsPointer(ref arr[0]));
        },
        [ShaderParameterType.UnsignedIntVector4] = (location, value) => {
            var vec = (uint[])value;
            GL.Uniform4ui(location, vec[0], vec[1], vec[2], vec[3]);
        },
        [ShaderParameterType.UnsignedIntVector4Array] = (location, value) => {
            var arr = (uint[])value;
            GL.Uniform4uiv(location, arr.Length / 4, (uint*)Unsafe.AsPointer(ref arr[0]));
        },
        
        [ShaderParameterType.Matrix3x2] = (location, value) => {
            var mat = (Matrix3x2)value;
            GL.UniformMatrix2x3fv(location, 1, true, (float*)Unsafe.AsPointer(ref mat.M11));
        },
        [ShaderParameterType.Matrix3x2Array] = (location, value) => {
            var arr = (Matrix3x2[])value;
            GL.UniformMatrix3x2fv(location, arr.Length, true, (float*)Unsafe.AsPointer(ref arr[0].M11));
        },
        [ShaderParameterType.Matrix4x4] = (location, value) => {
            var mat = (Matrix4x4)value;
            GL.UniformMatrix4fv(location, 1, true, (float*)Unsafe.AsPointer(ref mat.M11));
        },
        [ShaderParameterType.Matrix4x4Array] = (location, value) => {
            var arr = (Matrix4x4[])value;
            GL.UniformMatrix4fv(location, arr.Length, true, (float*)Unsafe.AsPointer(ref arr[0].M11));
        }
    };

    public static void SetUniform(ShaderParameterType type, int location, object value)
    {
        var setter = s_uniformSetters[type];
        if (setter == null) {
            throw new NotSupportedException("Shader parameter type not supported: " + type);
        }
        setter(location, value);
    }

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

    public static void EnableMatrix4x4Attributes(uint startIndex, uint divisor = 0)
    {
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 0);
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 2 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);

        ++startIndex;
        GL.EnableVertexAttribArray(startIndex);
        GL.VertexAttribPointer(startIndex, 4, VertexAttribPointerType.Float, false, MeshInstance.MemorySize, 3 * 4 * sizeof(float));
        GL.VertexAttribDivisor(startIndex, divisor);
    }

    public static void TexImage2D(TextureType type, PixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> pixels)
        => TexImage2D(TextureTarget.Texture2d, type, pixelFormat, width, height, pixels);

    public static void TexImage2D(TextureTarget target, TextureType type, PixelFormat pixelFormat, int width, int height, ReadOnlySpan<byte> pixels)
    {
        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Grey:
            GL.TexImage2D(
                target, 0, InternalFormat.R8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red,
                PixelType.UnsignedByte, pixels);
            break;
        case PixelFormat.GreyAlpha:
            GL.TexImage2D(
                target, 0, InternalFormat.Rg8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rg,
                PixelType.UnsignedByte, pixels);
            break;
        case PixelFormat.RedGreenBlue:
            format = type switch {
                TextureType.Diffuse => InternalFormat.Srgb,
                TextureType.UI => InternalFormat.Srgb,
                _ => InternalFormat.Rgb
            };
            GL.TexImage2D(
                target, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
                PixelType.UnsignedByte, pixels);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = type switch {
                TextureType.Diffuse => InternalFormat.SrgbAlpha,
                TextureType.UI => InternalFormat.SrgbAlpha,
                _ => InternalFormat.Rgba
            };
            GL.TexImage2D(
                target, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte, pixels);
            break;
        }
    }

    public static void TexImage2D(TextureType type, PixelFormat pixelFormat, int width, int height)
    {
        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Grey:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.R8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.GreyAlpha:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.Rg8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rg,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlue:
            format = type switch {
                TextureType.Diffuse => InternalFormat.Srgb,
                TextureType.UI => InternalFormat.Srgb,
                _ => InternalFormat.Rgb
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = type switch {
                TextureType.Diffuse => InternalFormat.SrgbAlpha,
                TextureType.UI => InternalFormat.SrgbAlpha,
                _ => InternalFormat.Rgba
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        }
    }
    
    public static void WaitSync(GLSync sync)
    {
        SyncStatus status;
        do {
            status = GL.ClientWaitSync(sync, SyncObjectMask.SyncFlushCommandsBit, 1);
        }
        while (status != SyncStatus.AlreadySignaled && status != SyncStatus.ConditionSatisfied);
    }

    public static void FenceSync(ref GLSync sync)
    {
        GL.DeleteSync(sync);
        sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
        GL.Flush();
    }
}