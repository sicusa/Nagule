namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

using PixelFormat = Nagule.Graphics.PixelFormat;
using GLPixelFormat = global::OpenTK.Graphics.OpenGL.PixelFormat;

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

    public static void TexImage2D(TextureType type, ImageBase image)
        => TexImage2D(TextureTarget.Texture2d, type, image);

    public static void TexImage2D(TextureTarget target, TextureType type, ImageBase image)
    {
        var pixelFormat = image.PixelFormat;
        int width = image.Width;
        int height = image.Height;

        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            switch (image) {
            case Image byteImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R8,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16f,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32f,
                    width, height, 0, GLPixelFormat.Red,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R16ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.R32ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreen:
            switch (image) {
            case Image byteImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg8,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16f,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32f,
                    width, height, 0, GLPixelFormat.Rg,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg16ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rg32ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlue:
            switch (image) {
            case Image byteImage:
                format = type switch {
                    TextureType.Diffuse => InternalFormat.Srgb8,
                    TextureType.UI => InternalFormat.Srgb8,
                    _ => InternalFormat.Rgb8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16f,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32f,
                    width, height, 0, GLPixelFormat.Rgb,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb16ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgb32ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlueAlpha:
            switch (image) {
            case Image byteImage:
                format = type switch {
                    TextureType.Diffuse => InternalFormat.Srgb8Alpha8,
                    TextureType.UI => InternalFormat.Srgb8Alpha8,
                    _ => InternalFormat.Rgba8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case Image<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16f,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case Image<float> float32Image:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32f,
                    width, height, 0, GLPixelFormat.Rgba,
                    PixelType.Float, float32Image.Data.AsSpan());
                break;
            case Image<short> shortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.Short, shortImage.Data.AsSpan());
                break;
            case Image<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba16ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case Image<int> intImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.Int, intImage.Data.AsSpan());
                break;
            case Image<uint> uintImage:
                GL.TexImage2D(
                    target, 0, InternalFormat.Rgba32ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    PixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        }
        throw new NotSupportedException("Pixel format not supported: " + pixelFormat);
    }

    public static void TexImage2D(TextureType type, PixelFormat pixelFormat, int width, int height)
    {
        InternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.R8,
                width, height, 0, GLPixelFormat.Red,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreen:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.Rg8,
                width, height, 0, GLPixelFormat.Rg,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlue:
            format = type switch {
                TextureType.Diffuse => InternalFormat.Srgb8,
                TextureType.UI => InternalFormat.Srgb8,
                _ => InternalFormat.Rgb8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgb,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = type switch {
                TextureType.Diffuse => InternalFormat.Srgb8Alpha8,
                TextureType.UI => InternalFormat.Srgb8Alpha8,
                _ => InternalFormat.Rgba8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgba,
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