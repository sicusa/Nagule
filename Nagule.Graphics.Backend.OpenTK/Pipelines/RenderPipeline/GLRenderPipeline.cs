namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;

using Aeco;
using Aeco.Local;

public class GLRenderPipeline : PolyHashStorage<IComponent>, IRenderPipeline
{
    public IReadOnlyList<IRenderPass> Passes => _passes;
    public Guid RenderSettingsId { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public FramebufferHandle FramebufferHandle { get; private set; }
    public BufferHandle UniformBufferHandle { get; private set; }

    public TextureHandle? ColorTextureHandle => _colorTexHandle;
    public TextureHandle? DepthTextureHandle => _depthTexHandle;

    public event Action<ICommandHost, IRenderPipeline>? OnResize;

    private TextureHandle? _colorTexHandle;
    private TextureHandle? _depthTexHandle;
    private List<IRenderPass> _passes;

    private bool _initialized;
    private string _profileKey;
    private static int s_pipelineCounter;

    public GLRenderPipeline(Guid renderSettingsId, int width, int height, IEnumerable<IRenderPass> passes)
    {
        RenderSettingsId = renderSettingsId;
        Width = width;
        Height = height;

        _passes = new(passes);
        _profileKey = "RenderPipeline_" + s_pipelineCounter++;
    }

    public void LoadResources(IContext context)
    {
        foreach (var pass in _passes) {
            pass.LoadResources(context);
        }
    }

    public void UnloadResources(IContext context)
    {
        foreach (var pass in _passes) {
            pass.UnloadResources(context);
        }
    }

    public void Initialize(ICommandHost host)
    {
        if (_initialized) {
            throw new InvalidOperationException("Render pipeline has been initialized");
        }
        _initialized = true;

        UniformBufferHandle = GL.GenBuffer();
        FramebufferHandle = GL.GenFramebuffer();

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 12, IntPtr.Zero, BufferUsageARB.DynamicDraw);

        foreach (var pass in CollectionsMarshal.AsSpan(_passes)) {
            try {
                pass.Initialize(host, this);
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to initialize pass '{pass}': " + e);
            }
        }
    }

    public void Uninitialize(ICommandHost host)
    {
        if (!_initialized) {
            throw new InvalidOperationException("Render pipeline has not been initialized");
        }
        _initialized = false;

        foreach (var pass in CollectionsMarshal.AsSpan(_passes)) {
            try {
                pass.Uninitialize(host, this);
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to uninitialize pass '{pass}': " + e);
            }
        }

        if (_colorTexHandle.HasValue) {
            GL.DeleteTexture(_colorTexHandle.Value);
            _colorTexHandle = null;
        }
        if (_depthTexHandle.HasValue) {
            GL.DeleteTexture(_depthTexHandle.Value);
            _depthTexHandle = null;
        }
        GL.DeleteFramebuffer(FramebufferHandle);
        GL.DeleteBuffer(UniformBufferHandle);

        FramebufferHandle = FramebufferHandle.Zero;
        UniformBufferHandle = BufferHandle.Zero;
    }

    public void Execute(ICommandHost host, MeshGroup meshGroup)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle);
        BindUniformBuffer();

        foreach (var pass in CollectionsMarshal.AsSpan(_passes)) {
            try {
                using (host.Profile(_profileKey, pass)) {
                    pass.Execute(host, this, meshGroup);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"[{_profileKey}] Failed to execute pass '{pass}': " + e);
            }
        }
    }

    public void Resize(ICommandHost host, int width, int height)
    {
        Width = width;
        Height = height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 4, width);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 4, 4, height);

        if (_colorTexHandle.HasValue) {
            GL.DeleteTexture(_colorTexHandle.Value);
            CreateColorTexture();
        }
        if (_depthTexHandle.HasValue) {
            GL.DeleteTexture(_depthTexHandle.Value);
            CreateDepthTexture();
        }
        OnResize?.Invoke(host, this);
    }

    public unsafe TextureHandle AcquireColorTexture()
        => _colorTexHandle ?? CreateColorTexture();

    private unsafe TextureHandle CreateColorTexture()
    {
        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, Width, Height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Linear);

        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, handle, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, (FramebufferHandle)currentFramebuffer);

        _colorTexHandle = handle;
        return handle;
    }

    public unsafe TextureHandle AcquireDepthTexture()
        => _depthTexHandle ?? CreateDepthTexture();

    private unsafe TextureHandle CreateDepthTexture()
    {
        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.NearestMipmapNearest);

        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, handle, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, (FramebufferHandle)currentFramebuffer);

        _depthTexHandle = handle;
        return handle;
    }

    public void BindUniformBuffer()
    {
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, UniformBufferHandle);
    }
}