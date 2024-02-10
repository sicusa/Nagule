namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class StandardPipelineFramebuffer : PipelineFramebufferBase
{
    public override FramebufferHandle Handle => _frontHandle;
    public override TextureHandle ColorAttachmentHandle => _frontColorHandle;
    public override TextureHandle DepthAttachmentHandle => _depthHandle;

    private FramebufferHandle _frontHandle;
    private TextureHandle _frontColorHandle;

    private FramebufferHandle _backHandle;
    private TextureHandle _backColorHandle;

    private TextureHandle _depthHandle;

    private Light3DLibrary _lightLib = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _frontHandle = new(GL.GenFramebuffer());
        _backHandle = new(GL.GenFramebuffer());

        GenerateDepthBuffer();
        InitializeFramebuffer(_frontHandle, out _frontColorHandle);
        InitializeFramebuffer(_backHandle, out _backColorHandle);

        _lightLib = PipelineInfo.MainWorld.GetAddon<Light3DLibrary>();
    }

    public override void OnUninitialize(World world)
    {
        base.OnUninitialize(world);

        GL.DeleteFramebuffer(_frontHandle.Handle);
        GL.DeleteFramebuffer(_backHandle.Handle);

        GL.DeleteTexture(_frontColorHandle.Handle);
        GL.DeleteTexture(_backColorHandle.Handle);
        GL.DeleteTexture(_depthHandle.Handle);
    }

    public override void Update(float time)
    {
        base.Update(time);

        ref var settingsState = ref PipelineInfo.CameraState
            .Get<Camera3DState>().SettingsState
            .Get<RenderSettingsState>();
        
        if (settingsState.Loaded) {
            var sunLightIndex = settingsState
                .SunLightState?.Get<Light3DState>().Index;
            if (sunLightIndex != null) {
                ref var sunPars = ref _lightLib.Parameters[sunLightIndex.Value];
                Uniform.SunLightDirection = sunPars.Direction;
            }
        }
    }

    public override void Resize(int width, int height)
    {
        base.Resize(width, height);

        GL.DeleteTexture(_frontColorHandle.Handle);
        GL.DeleteTexture(_backColorHandle.Handle);
        GL.DeleteTexture(_depthHandle.Handle);

        GenerateDepthBuffer();
        InitializeFramebuffer(_frontHandle, out _frontColorHandle);
        InitializeFramebuffer(_backHandle, out _backColorHandle);
    }

    public override void SwapColorAttachments()
    {
        static void DoSwap<T>(ref T a, ref T b)
            => (b, a) = (a, b);
        
        DoSwap(ref _frontHandle, ref _backHandle);
        DoSwap(ref _frontColorHandle, ref _backColorHandle);
    }

    private unsafe void InitializeFramebuffer(FramebufferHandle handle, out TextureHandle colorTex)
    {
        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle.Handle);

        colorTex = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, colorTex.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.Rgba16f, Width, Height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, colorTex.Handle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depthHandle.Handle, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFramebuffer);
        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    private void GenerateDepthBuffer()
    {
        _depthHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, _depthHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);
    }
}