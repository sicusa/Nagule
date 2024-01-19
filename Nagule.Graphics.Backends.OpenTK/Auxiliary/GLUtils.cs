namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using Sia;

internal unsafe static class GLUtils
{
    public const int BuiltInBufferCount = 4;

    public static readonly FrozenSet<GLInternalFormat> IntegerInternalFormats =
        new HashSet<GLInternalFormat> {
            GLInternalFormat.R8i,
            GLInternalFormat.R8ui,
            GLInternalFormat.R16i,
            GLInternalFormat.R16ui,
            GLInternalFormat.R32i,
            GLInternalFormat.R32ui,

            GLInternalFormat.Rg8i,
            GLInternalFormat.Rg8ui,
            GLInternalFormat.Rg16i,
            GLInternalFormat.Rg16ui,
            GLInternalFormat.Rg32i,
            GLInternalFormat.Rg32ui,

            GLInternalFormat.Rgb8i,
            GLInternalFormat.Rgb8ui,
            GLInternalFormat.Rgb16i,
            GLInternalFormat.Rgb16ui,
            GLInternalFormat.Rgb32i,
            GLInternalFormat.Rgb32ui,

            GLInternalFormat.Rgba8i,
            GLInternalFormat.Rgba8ui,
            GLInternalFormat.Rgba16i,
            GLInternalFormat.Rgba16ui,
            GLInternalFormat.Rgba32i,
            GLInternalFormat.Rgba32ui
        }.ToFrozenSet();

    public readonly record struct GLTexPixelInfo(
        GLInternalFormat InternalFormat, GLPixelType PixelType);

    public static readonly FrozenDictionary<(Type, PixelFormat), GLTexPixelInfo> ImagePixelInfoMap =
        new Dictionary<(Type, PixelFormat), GLTexPixelInfo>() {
            [(typeof(byte), PixelFormat.Grey)] = new(GLInternalFormat.R8, GLPixelType.UnsignedByte),
            [(typeof(byte), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg8, GLPixelType.UnsignedByte),
            [(typeof(byte), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb8, GLPixelType.UnsignedByte),
            [(typeof(byte), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba8, GLPixelType.UnsignedByte),

            [(typeof(short), PixelFormat.Grey)] = new(GLInternalFormat.R16i, GLPixelType.Short),
            [(typeof(short), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg16i, GLPixelType.Short),
            [(typeof(short), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb16i, GLPixelType.Short),
            [(typeof(short), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba16i, GLPixelType.Short),

            [(typeof(ushort), PixelFormat.Grey)] = new(GLInternalFormat.R16ui, GLPixelType.UnsignedShort),
            [(typeof(ushort), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg16ui, GLPixelType.UnsignedShort),
            [(typeof(ushort), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb16ui, GLPixelType.UnsignedShort),
            [(typeof(ushort), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba16ui, GLPixelType.UnsignedShort),

            [(typeof(int), PixelFormat.Grey)] = new(GLInternalFormat.R32i, GLPixelType.Int),
            [(typeof(int), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg32i, GLPixelType.Int),
            [(typeof(int), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb32i, GLPixelType.Int),
            [(typeof(int), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba32i, GLPixelType.Int),

            [(typeof(uint), PixelFormat.Grey)] = new(GLInternalFormat.R32ui, GLPixelType.UnsignedInt),
            [(typeof(uint), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg32ui, GLPixelType.UnsignedInt),
            [(typeof(uint), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb32ui, GLPixelType.UnsignedInt),
            [(typeof(uint), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba32ui, GLPixelType.UnsignedInt),

            [(typeof(Half), PixelFormat.Grey)] = new(GLInternalFormat.R16, GLPixelType.HalfFloat),
            [(typeof(Half), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg16, GLPixelType.HalfFloat),
            [(typeof(Half), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb16, GLPixelType.HalfFloat),
            [(typeof(Half), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba16, GLPixelType.HalfFloat),

            [(typeof(float), PixelFormat.Grey)] = new(GLInternalFormat.R32f, GLPixelType.Float),
            [(typeof(float), PixelFormat.GreyAlpha)] = new(GLInternalFormat.Rg32f, GLPixelType.Float),
            [(typeof(float), PixelFormat.RedGreenBlue)] = new(GLInternalFormat.Rgb32f, GLPixelType.Float),
            [(typeof(float), PixelFormat.RedGreenBlueAlpha)] = new(GLInternalFormat.Rgba32f, GLPixelType.Float),
        }.ToFrozenDictionary();

    private static readonly InvalidateFramebufferAttachment[] s_colorAttachmentToInvalidate =
        [InvalidateFramebufferAttachment.ColorAttachment0];
    private static readonly InvalidateFramebufferAttachment[] s_depthAttachmentToInvalidate =
        [InvalidateFramebufferAttachment.DepthAttachment];

    public static IntPtr InitializeBuffer(BufferTargetARB target, int length)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            GL.BufferData(target, length, IntPtr.Zero, BufferUsageARB.DynamicDraw);
            return (IntPtr)GL.MapBuffer(target, BufferAccessARB.ReadWrite);
        }
        else {
            GL.BufferStorage((BufferStorageTarget)target, length, IntPtr.Zero,
                BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit | BufferStorageMask.MapCoherentBit);
            return (IntPtr)GL.MapBufferRange(target, 0, length,
                MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit);
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

    public static GLPrimitiveType Convert(PrimitiveType type)
        => type switch {
            PrimitiveType.Point => GLPrimitiveType.Points,
            PrimitiveType.Line => GLPrimitiveType.Lines,
            PrimitiveType.Triangle => GLPrimitiveType.Triangles,
            PrimitiveType.Polygon => (GLPrimitiveType)9u, // GL_POLYGON
            _ => GLPrimitiveType.Triangles
        };
    
    public static void CheckError()
    {
        var error = GL.GetError();
        if (error != GLErrorCode.NoError) {
            Console.WriteLine(error);
        }
    }

    public static void TexImage2D(TextureUsage usage, RImageBase image)
        => TexImage2D(TextureTarget.Texture2d, usage, image);

    public static void TexImage2D(TextureTarget target, TextureUsage usage, RImageBase image)
    {
        var pixelFormat = image.PixelFormat;
        var (internalFormat, pixelType) = GetTexPixelInfo(image);
        var glPixelFormat = SetPixelFormat(target, pixelFormat, internalFormat, pixelType);

        if (IsSRGBTexture(usage)) {
            internalFormat = ToSRGBColorSpace(internalFormat);
        }

        if (image.Length == 0) {
            GL.TexImage2D(target, 0, internalFormat,
                image.Width, image.Height, 0, glPixelFormat, pixelType, (void*)0);
        }
        else {
            GL.TexImage2D(target, 0, internalFormat,
                image.Width, image.Height, 0, glPixelFormat, pixelType, image.AsByteSpan());
        }
    }

    public static GLTexPixelInfo GetTexPixelInfo(RImageBase image)
    {
        if (!ImagePixelInfoMap.TryGetValue((image.ChannelType, image.PixelFormat), out var info)) {
            throw new NotSupportedException("Image not supported: " + image.GetType());
        }
        return info;
    }

    public static GLPixelFormat SetPixelFormat(
        TextureTarget target, PixelFormat pixelFormat, GLInternalFormat internalFormat, GLPixelType pixelType)
    {
        bool isInteger = IntegerInternalFormats.Contains(internalFormat);
        switch (pixelFormat) {
        case PixelFormat.Grey:
            var format = isInteger ? GLPixelFormat.RedInteger : GLPixelFormat.Red;
            GL.TexParameteri(target, TextureParameterName.TextureSwizzleG, (int)format);
            GL.TexParameteri(target, TextureParameterName.TextureSwizzleB, (int)format);
            return format;
        case PixelFormat.GreyAlpha:
            if (isInteger) {
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleG, (int)GLPixelFormat.RedInteger);
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleB, (int)GLPixelFormat.RedInteger);
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleA, (int)GLPixelFormat.GreenInteger);
                return GLPixelFormat.RgInteger;
            }
            else {
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleG, (int)GLPixelFormat.Red);
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleB, (int)GLPixelFormat.Red);
                GL.TexParameteri(target, TextureParameterName.TextureSwizzleA, (int)GLPixelFormat.Green);
                return GLPixelFormat.Rg;
            }
        case PixelFormat.RedGreenBlue:
            return isInteger ? GLPixelFormat.RgbInteger : GLPixelFormat.Rgb;
        case PixelFormat.RedGreenBlueAlpha:
            return isInteger ? GLPixelFormat.RgbaInteger : GLPixelFormat.Rgba;
        default:
            throw new NaguleInternalException("Invalid pixel format: " + pixelFormat);
        }
    }

    public static bool IsSRGBTexture(TextureUsage usage)
        => usage == TextureUsage.Color || usage == TextureUsage.UI;

    public static GLInternalFormat ToSRGBColorSpace(GLInternalFormat format)
        => format switch {
            GLInternalFormat.R8 => GLInternalFormat.Sr8Ext,
            GLInternalFormat.Rg8 => GLInternalFormat.Srg8Ext,
            GLInternalFormat.Rgb8 => GLInternalFormat.Srgb8,
            GLInternalFormat.Rgba8 => GLInternalFormat.Srgb8Alpha8,
            _ => format
        };
}