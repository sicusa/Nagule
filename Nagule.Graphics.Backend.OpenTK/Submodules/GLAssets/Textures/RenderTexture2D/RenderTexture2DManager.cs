namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class RenderTexture2DManager : TextureManagerBase<RenderTexture2D, RenderTexture2DAsset, RenderTexture2DState>
{
    protected override TextureTarget TextureTarget => TextureTarget.Texture2d;

    internal (int, int) WindowSize { get; set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        RegisterCommonListeners(
            (RenderTexture2D.SetMinFilter cmd) => cmd.Value,
            (RenderTexture2D.SetMagFilter cmd) => cmd.Value,
            (RenderTexture2D.SetBorderColor cmd) => cmd.Value,
            (RenderTexture2D.SetMipmapEnabled cmd) => cmd.Value);

        RegisterParameterListener((in RenderTexture2DState state, in RenderTexture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in RenderTexture2DState state, in RenderTexture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));
    }

    protected override void LoadAsset(EntityRef entity, ref RenderTexture2D asset)
    {
        var (width, height) = asset.AutoResizeByWindow ? WindowSize : (asset.Width, asset.Height);

        var type = asset.Type;
        var pixelFormat = asset.PixelFormat;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFrame.Enqueue(entity, () => {
            var state = new RenderTexture2DState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled,
                Width = width,
                Height = height,
                FramebufferHandle = new(GL.GenFramebuffer())
            };

            GL.BindTexture(TextureTarget.Texture2d, state.Handle.Handle);
            GLUtils.TexImage2D(type, pixelFormat, width, height);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));

            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, state.FramebufferHandle.Handle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, state.Handle.Handle, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero.Handle);

            RenderStates.Set(entity, state);
            Handles.Set(entity, state.Handle);
            return true;
        });
    }
}