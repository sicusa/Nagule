using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public class Tileset2DRenderTarget(RTileset2D tileset, int index) : RenderTargetBase
{
    public override (int, int) ViewportSize {
        get {
            ref var state = ref _textureStateEntity.Get<Tileset2DState>();
            return (state.TileWidth, state.TileHeight);
        }
    }
    
    private EntityRef _textureStateEntity;
    private FramebufferHandle _framebuffer;
    private TextureHandle _texHandle;
    private bool _mipmapEnabled;

    public override void OnInitialize(World world, EntityRef cameraEntity)
    {
        base.OnInitialize(world, cameraEntity);

        _textureStateEntity = world.AcquireAsset(tileset, cameraEntity)
            .GetStateEntity();
    }

    public override void OnUninitialize(World world, EntityRef cameraEntity)
    {
        base.OnUninitialize(world, cameraEntity);
        cameraEntity.Unrefer(world.GetAsset(tileset));
        RenderFramer.Start(() => GL.DeleteFramebuffer(_framebuffer.Handle));
    }

    protected override bool PrepareBlit()
    {
        ref var texState = ref _textureStateEntity.Get<Tileset2DState>();
        if (!texState.Loaded) { return false; }

        _texHandle = texState.Handle;
        _mipmapEnabled = texState.IsMipmapEnabled;

        if (_framebuffer == FramebufferHandle.Zero) {
            _framebuffer = new(GL.GenFramebuffer());
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.Handle);
            GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0, texState.Handle.Handle, 0, index);
        }
        else {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.Handle);
        }

        GL.Viewport(0, 0, texState.TileWidth, texState.TileHeight);
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