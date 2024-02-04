namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class PipelineFramebuffer : IAddon
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PipelineUniform
    {
        public int ViewportWidth;
        public int ViewportHeight;
        public int Frame;
        public float Time;
        public Vector3 SunLightDirection;

        public static readonly int MemorySize = Unsafe.SizeOf<PipelineUniform>();
    }

    public BufferHandle UniformBufferHandle { get; private set; }

    public FramebufferHandle Handle => _frontHandle;
    public TextureHandle ColorHandle => _frontColorHandle;
    public TextureHandle DepthHandle => _depthHandle;

    public VertexArrayHandle EmptyVertexArray { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    private IntPtr _uniformPointer;

    private FramebufferHandle _frontHandle;
    private TextureHandle _frontColorHandle;

    private FramebufferHandle _backHandle;
    private TextureHandle _backColorHandle;

    private TextureHandle _depthHandle;

    [AllowNull] private RenderPipelineInfo _info;
    [AllowNull] private Light3DLibrary _lightLib;

    public unsafe void OnInitialize(World world)
    {
        Width = 512;
        Height = 512;

        UniformBufferHandle = new(GL.GenBuffer());
        EmptyVertexArray = new(GL.GenVertexArray());

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle.Handle);
        _uniformPointer = GLUtils.InitializeBuffer(BufferTargetARB.UniformBuffer, PipelineUniform.MemorySize);

        var uniform = (PipelineUniform*)_uniformPointer;
        uniform->ViewportWidth = Width;
        uniform->ViewportHeight = Height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        _frontHandle = new(GL.GenFramebuffer());
        _backHandle = new(GL.GenFramebuffer());

        GenerateDepthBuffer();
        InitializeFramebuffer(_frontHandle, out _frontColorHandle);
        InitializeFramebuffer(_backHandle, out _backColorHandle);

        _info = world.GetAddon<RenderPipelineInfo>();
        _lightLib = _info.MainWorld.GetAddon<Light3DLibrary>();
    }

    public void OnUninitialize(World world)
    {
        GL.DeleteBuffer(UniformBufferHandle.Handle);

        GL.DeleteFramebuffer(_frontHandle.Handle);
        GL.DeleteFramebuffer(_backHandle.Handle);

        GL.DeleteTexture(_frontColorHandle.Handle);
        GL.DeleteTexture(_backColorHandle.Handle);
        GL.DeleteTexture(_depthHandle.Handle);
    }

    public unsafe void Update(float time)
    {
        var uniform = (PipelineUniform*)_uniformPointer;
        uniform->Time = time;

        var renderSettingsState = _info
            .CameraState.Get<Camera3DState>()
            .RenderSettingsState.Get<RenderSettingsState>();
        
        if (renderSettingsState.Loaded) {
            var sunLightIndex = renderSettingsState
                .SunLightState?.Get<Light3DState>().Index;
            if (sunLightIndex != null) {
                ref var sunPars = ref _lightLib.Parameters[sunLightIndex.Value];
                uniform->SunLightDirection = sunPars.Direction;
            }
        }
    }

    public unsafe void Resize(int width, int height)
    {
        Width = width;
        Height = height;

        var uniform = (PipelineUniform*)_uniformPointer;
        uniform->ViewportWidth = width;
        uniform->ViewportHeight = height;

        GL.DeleteTexture(_frontColorHandle.Handle);
        GL.DeleteTexture(_backColorHandle.Handle);
        GL.DeleteTexture(DepthHandle.Handle);

        GenerateDepthBuffer();
        InitializeFramebuffer(_frontHandle, out _frontColorHandle);
        InitializeFramebuffer(_backHandle, out _backColorHandle);
    }

    public void Swap()
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