namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

using GLTextureWrapMode = global::OpenTK.Graphics.OpenGL.TextureWrapMode;
using GLTextureMinFilter = global::OpenTK.Graphics.OpenGL.TextureMinFilter;
using GLTextureMagFilter = global::OpenTK.Graphics.OpenGL.TextureMagFilter;

internal static class TextureHelper
{
    public static int Cast(TextureWrapMode mode)
        => (int)(mode switch {
            TextureWrapMode.ClampToBorder => GLTextureWrapMode.ClampToBorder,
            TextureWrapMode.ClampToEdge => GLTextureWrapMode.ClampToEdge,
            TextureWrapMode.MirroredRepeat => GLTextureWrapMode.MirroredRepeat,
            TextureWrapMode.Repeat => GLTextureWrapMode.Repeat,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    public static int Cast(TextureMinFilter filter)
        => (int)(filter switch {
            TextureMinFilter.Linear => GLTextureMinFilter.Linear,
            TextureMinFilter.LinearMipmapLinear => GLTextureMinFilter.LinearMipmapLinear,
            TextureMinFilter.LinearMipmapNearest => GLTextureMinFilter.LinearMipmapNearest,
            TextureMinFilter.Nearest => GLTextureMinFilter.Nearest,
            TextureMinFilter.NearestMipmapLinear => GLTextureMinFilter.NearestMipmapLinear,
            TextureMinFilter.NearestMipmapNearest => GLTextureMinFilter.NearestMipmapNearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    public static int Cast(TextureMagFilter filter)
        => (int)(filter switch {
            TextureMagFilter.Linear => GLTextureMagFilter.Linear,
            TextureMagFilter.Nearest => GLTextureMagFilter.Nearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });
    
    public static TextureTarget Cast(CubemapTextureTarget target)
        => target switch {
            CubemapTextureTarget.PositiveX => TextureTarget.TextureCubeMapPositiveX,
            CubemapTextureTarget.NegativeX => TextureTarget.TextureCubeMapNegativeX,
            CubemapTextureTarget.PositiveY => TextureTarget.TextureCubeMapPositiveY,
            CubemapTextureTarget.NegativeY => TextureTarget.TextureCubeMapNegativeY,
            CubemapTextureTarget.PositiveZ => TextureTarget.TextureCubeMapPositiveZ,
            CubemapTextureTarget.NegativeZ => TextureTarget.TextureCubeMapNegativeZ,
            _ => throw new NotSupportedException("Invalid cubemap texture target")
        };
}