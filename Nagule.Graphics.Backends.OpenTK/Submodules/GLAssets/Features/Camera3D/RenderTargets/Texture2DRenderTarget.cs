using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public class Texture2DRenderTarget(RTexture2D texture) : RenderTargetBase
{
    public override (int, int) ViewportSize {
        get {
            ref var state = ref _textureStateEntity.Get<Texture2DState>();
            return (state.Width, state.Height);
        }
    }
    
    private EntityRef _textureStateEntity;
    private FramebufferHandle _framebuffer;
    private TextureHandle _texHandle;
    private bool _mipmapEnabled;

    public override void OnInitialize(World world, EntityRef cameraEntity)
    {
        base.OnInitialize(world, cameraEntity);

        _textureStateEntity = world.AcquireAsset(texture, cameraEntity)
            .GetStateEntity();
    }

    public override void OnUninitailize(World world, EntityRef cameraEntity)
    {
        base.OnUninitailize(world, cameraEntity);
        cameraEntity.Unrefer(world.GetAsset(texture));
        RenderFramer.Start(() => GL.DeleteFramebuffer(_framebuffer.Handle));
    }

    protected override bool PrepareBlit()
    {
        ref var texState = ref _textureStateEntity.Get<Texture2DState>();
        if (!texState.Loaded) { return false; }

        _texHandle = texState.Handle;
        _mipmapEnabled = texState.IsMipmapEnabled;

        if (_framebuffer == FramebufferHandle.Zero) {
            _framebuffer = new(GL.GenFramebuffer());
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.Handle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, texState.Handle.Handle, 0);
        }
        else {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.Handle);
        }

        GL.Viewport(0, 0, texState.Width, texState.Height);
        return true;
    }

    protected override void FinishBlit()
    {
        if (_mipmapEnabled) {
            GL.BindTexture(TextureTarget.Texture2d, _texHandle.Handle);
            GL.GenerateMipmap(TextureTarget.Texture2d);
            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
    }
}