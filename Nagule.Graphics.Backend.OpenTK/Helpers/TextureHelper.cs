namespace Nagule.Graphics.Backend.OpenTK;

using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

internal static class TextureHelper
{
    public static int Cast(TextureWrapMode mode)
        => (int)(mode switch {
            TextureWrapMode.ClampToBorder => global::OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToBorder,
            TextureWrapMode.ClampToEdge => global::OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge,
            TextureWrapMode.MirroredRepeat => global::OpenTK.Graphics.OpenGL.TextureWrapMode.MirroredRepeat,
            TextureWrapMode.Repeat => global::OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    public static int Cast(TextureMinFilter filter)
        => (int)(filter switch {
            TextureMinFilter.Linear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.Linear,
            TextureMinFilter.LinearMipmapLinear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapLinear,
            TextureMinFilter.LinearMipmapNearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapNearest,
            TextureMinFilter.Nearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.Nearest,
            TextureMinFilter.NearestMipmapLinear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.NearestMipmapLinear,
            TextureMinFilter.NearestMipmapNearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.NearestMipmapNearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    public static int Cast(TextureMagFilter filter)
        => (int)(filter switch {
            TextureMagFilter.Linear => global::OpenTK.Graphics.OpenGL.TextureMagFilter.Linear,
            TextureMagFilter.Nearest => global::OpenTK.Graphics.OpenGL.TextureMagFilter.Nearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });
}