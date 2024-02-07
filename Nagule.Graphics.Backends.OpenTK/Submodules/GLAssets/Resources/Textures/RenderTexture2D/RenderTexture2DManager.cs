namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public partial class RenderTexture2DManager
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

        RegisterParameterListener((ref RenderTexture2DState state, in RenderTexture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((ref RenderTexture2DState state, in RenderTexture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        Listen((in EntityRef entity, in RenderTexture2D.SetUsage cmd) => RegenerateRenderTexture(entity));
        Listen((in EntityRef entity, in RenderTexture2D.SetImage cmd) => RegenerateRenderTexture(entity));
        Listen((in EntityRef entity, in RenderTexture2D.SetAutoResizeByWindow cmd) => RegenerateRenderTexture(entity));
    }
        
    internal void RegenerateRenderTexture(in EntityRef entity)
    {
        ref var tex = ref entity.Get<RenderTexture2D>();
        var usage = tex.Usage;
        var image = tex.Image ?? RImage.Hint;

        if (tex.AutoResizeByWindow) {
            image = image with {
                Width = WindowSize.Item1,
                Height = WindowSize.Item2
            };
        }

        RegenerateTexture(entity, (ref RenderTexture2DState state) => {
            GLUtils.TexImage2D(usage, image);
            state.Width = image.Width;
            state.Height = image.Height;
        });
    }

    public override void LoadAsset(in EntityRef entity, ref RenderTexture2D asset, EntityRef stateEntity)
    {
        var image = asset.Image ?? RImage.Hint;
        if (asset.AutoResizeByWindow) {
            image = image with {
                Width = WindowSize.Item1,
                Height = WindowSize.Item2
            };
        }

        var usage = asset.Usage;
        var pixelFormat = image.PixelFormat;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderTexture2DState>();
            state = new RenderTexture2DState {
                Handle = new(GL.GenTexture()),
                MinFilter = minFilter,
                MagFilter = magFilter,
                MipmapEnabled = mipmapEnabled,
                Width = image.Width,
                Height = image.Height,
                FramebufferHandle = new(GL.GenFramebuffer())
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            GLUtils.TexImage2D(usage, image);

            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));

            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);

            int prevFramebuffer = 0;
            GL.GetInteger(GetPName.DrawFramebufferBinding, ref prevFramebuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, state.FramebufferHandle.Handle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, state.Handle.Handle, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFramebuffer);
        });
    }
}