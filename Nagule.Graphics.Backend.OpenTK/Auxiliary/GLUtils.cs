namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;

internal unsafe static class GLUtils
{
    public const int BuiltInBufferCount = 4;

    private static readonly float[] s_transparencyClearColor = {0, 0, 0, 1};
    private static readonly InvalidateFramebufferAttachment[] s_colorAttachmentToInvalidate =
        new[] { InvalidateFramebufferAttachment.ColorAttachment0 };
    private static readonly InvalidateFramebufferAttachment[] s_depthAttachmentToInvalidate =
        new[] { InvalidateFramebufferAttachment.DepthAttachment };

    public static IntPtr InitializeBuffer(BufferTargetARB target, int length)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            GL.BufferData(target, length, IntPtr.Zero, BufferUsageARB.DynamicDraw);
            return (IntPtr)GL.MapBuffer(target, BufferAccessARB.WriteOnly);
        }
        else {
            GL.BufferStorage((BufferStorageTarget)target, length, IntPtr.Zero,
                BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
            return (IntPtr)GL.MapBufferRange(target, 0, length,
                MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit);
        }
    }

    public static void TexImage2D(TextureType type, ImageAssetBase image)
        => TexImage2D(TextureTarget.Texture2d, type, image);

    public static void TexImage2D(TextureTarget target, TextureType type, ImageAssetBase image)
    {
        var pixelFormat = image.PixelFormat;
        int width = image.Width;
        int height = image.Height;

        GLInternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            switch (image) {
            case ImageAsset byteImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R8,
                    width, height, 0, GLPixelFormat.Red,
                    GLPixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case ImageAsset<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R16f,
                    width, height, 0, GLPixelFormat.Red,
                    GLPixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case ImageAsset<float> float32Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R32f,
                    width, height, 0, GLPixelFormat.Red,
                    GLPixelType.Float, float32Image.Data.AsSpan());
                break;
            case ImageAsset<short> shortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R16i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    GLPixelType.Short, shortImage.Data.AsSpan());
                break;
            case ImageAsset<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R16ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    GLPixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case ImageAsset<int> intImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R32i,
                    width, height, 0, GLPixelFormat.RedInteger,
                    GLPixelType.Int, intImage.Data.AsSpan());
                break;
            case ImageAsset<uint> uintImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.R32ui,
                    width, height, 0, GLPixelFormat.RedInteger,
                    GLPixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreen:
            switch (image) {
            case ImageAsset byteImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg8,
                    width, height, 0, GLPixelFormat.Rg,
                    GLPixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case ImageAsset<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg16f,
                    width, height, 0, GLPixelFormat.Rg,
                    GLPixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case ImageAsset<float> float32Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg32f,
                    width, height, 0, GLPixelFormat.Rg,
                    GLPixelType.Float, float32Image.Data.AsSpan());
                break;
            case ImageAsset<short> shortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg16i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    GLPixelType.Short, shortImage.Data.AsSpan());
                break;
            case ImageAsset<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg16ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    GLPixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case ImageAsset<int> intImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg32i,
                    width, height, 0, GLPixelFormat.RgInteger,
                    GLPixelType.Int, intImage.Data.AsSpan());
                break;
            case ImageAsset<uint> uintImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rg32ui,
                    width, height, 0, GLPixelFormat.RgInteger,
                    GLPixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlue:
            switch (image) {
            case ImageAsset byteImage:
                format = type switch {
                    TextureType.Color => GLInternalFormat.Srgb8,
                    TextureType.UI => GLInternalFormat.Srgb8,
                    _ => GLInternalFormat.Rgb8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgb,
                    GLPixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case ImageAsset<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb16f,
                    width, height, 0, GLPixelFormat.Rgb,
                    GLPixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case ImageAsset<float> float32Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb32f,
                    width, height, 0, GLPixelFormat.Rgb,
                    GLPixelType.Float, float32Image.Data.AsSpan());
                break;
            case ImageAsset<short> shortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb16i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    GLPixelType.Short, shortImage.Data.AsSpan());
                break;
            case ImageAsset<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb16ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    GLPixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case ImageAsset<int> intImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb32i,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    GLPixelType.Int, intImage.Data.AsSpan());
                break;
            case ImageAsset<uint> uintImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgb32ui,
                    width, height, 0, GLPixelFormat.RgbInteger,
                    GLPixelType.UnsignedInt, uintImage.Data.AsSpan());
                break;
            default:
                throw new NotSupportedException("Pixel type not supported: " + image.GetType());
            }
            return;
        case PixelFormat.RedGreenBlueAlpha:
            switch (image) {
            case ImageAsset byteImage:
                format = type switch {
                    TextureType.Color => GLInternalFormat.Srgb8Alpha8,
                    TextureType.UI => GLInternalFormat.Srgb8Alpha8,
                    _ => GLInternalFormat.Rgba8
                };
                GL.TexImage2D(
                    target, 0, format,
                    width, height, 0, GLPixelFormat.Rgba,
                    GLPixelType.UnsignedByte, byteImage.Data.AsSpan());
                break;
            case ImageAsset<Half> flaot16Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba16f,
                    width, height, 0, GLPixelFormat.Rgba,
                    GLPixelType.HalfFloat, flaot16Image.Data.AsSpan());
                break;
            case ImageAsset<float> float32Image:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba32f,
                    width, height, 0, GLPixelFormat.Rgba,
                    GLPixelType.Float, float32Image.Data.AsSpan());
                break;
            case ImageAsset<short> shortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba16i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    GLPixelType.Short, shortImage.Data.AsSpan());
                break;
            case ImageAsset<ushort> ushortImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba16ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    GLPixelType.UnsignedShort, ushortImage.Data.AsSpan());
                break;
            case ImageAsset<int> intImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba32i,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    GLPixelType.Int, intImage.Data.AsSpan());
                break;
            case ImageAsset<uint> uintImage:
                GL.TexImage2D(
                    target, 0, GLInternalFormat.Rgba32ui,
                    width, height, 0, GLPixelFormat.RgbaInteger,
                    GLPixelType.UnsignedInt, uintImage.Data.AsSpan());
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
        GLInternalFormat format;

        switch (pixelFormat) {
        case PixelFormat.Red:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, GLInternalFormat.R8,
                width, height, 0, GLPixelFormat.Red,
                GLPixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreen:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, GLInternalFormat.Rg8,
                width, height, 0, GLPixelFormat.Rg,
                GLPixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlue:
            format = type switch {
                TextureType.Color => GLInternalFormat.Srgb8,
                TextureType.UI => GLInternalFormat.Srgb8,
                _ => GLInternalFormat.Rgb8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgb,
                GLPixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = type switch {
                TextureType.Color => GLInternalFormat.Srgb8Alpha8,
                TextureType.UI => GLInternalFormat.Srgb8Alpha8,
                _ => GLInternalFormat.Rgba8
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, GLPixelFormat.Rgba,
                GLPixelType.UnsignedByte, IntPtr.Zero);
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
    }

    public static void Clear(ClearFlags flags)
    {
        switch (flags) {
        case ClearFlags.Color | ClearFlags.Depth:
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            break;
        case ClearFlags.Color:
            GL.Clear(ClearBufferMask.ColorBufferBit);
            break;
        case ClearFlags.Depth:
            GL.Clear(ClearBufferMask.DepthBufferBit);
            break;
        }
    }

    public static void InvalidateColorBuffer()
    {
        GL.InvalidateFramebuffer(FramebufferTarget.Framebuffer, s_colorAttachmentToInvalidate.AsSpan());
    }

    public static void InvalidateDepthBuffer()
    {
        GL.InvalidateFramebuffer(FramebufferTarget.Framebuffer, s_depthAttachmentToInvalidate.AsSpan());
    }

    public static void DrawQuad()
    {
        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
    }

    public static GLPrimitiveType Cast(PrimitiveType type)
        => type switch {
            PrimitiveType.Point => GLPrimitiveType.Points,
            PrimitiveType.Line => GLPrimitiveType.Lines,
            PrimitiveType.Triangle => GLPrimitiveType.Triangles,
            PrimitiveType.Polygon => (GLPrimitiveType)9u, // GL_POLYGON
            _ => GLPrimitiveType.Triangles
        };
}