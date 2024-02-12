using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public class TextureRenderTarget(RRenderTexture2D texture) : RenderTargetBase
{
    public override (int, int) ViewportSize {
        get {
            ref var state = ref _textureStateEntity.Get<RenderTexture2DState>();
            return (state.Width, state.Height);
        }
    }
    
    private EntityRef _textureStateEntity;
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
    }

    protected override bool PrepareBlit()
    {
        ref var texState = ref _textureStateEntity.Get<RenderTexture2DState>();
        if (!texState.Loaded) { return false; }

        _texHandle = texState.Handle;
        _mipmapEnabled = texState.MipmapEnabled;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, texState.FramebufferHandle.Handle);
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