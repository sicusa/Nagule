namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Local;

public class RenderSettingsManager : ResourceManagerBase<RenderSettings>,
    ILoadListener, IWindowResizeListener
{
    private class GLRenderPipeline : PolyHashStorage<IComponent>, IRenderPipeline
    {
        public Guid RenderSettingsId { get; init; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public FramebufferHandle FramebufferHandle { get; private set; }
        public BufferHandle UniformBufferHandle { get; private set; }

        public unsafe TextureHandle ColorTextureHandle {
            get {
                if (_colorTexHandle.HasValue) {
                    return _colorTexHandle.Value;
                }

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
        }

        public unsafe TextureHandle DepthTextureHandle {
            get {
                if (_depthTexHandle.HasValue) {
                    return _depthTexHandle.Value;
                }

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
        }

        public IReadOnlyList<IRenderPass> RenderPasses => _renderPasses;

        public event Action<ICommandHost, IRenderPipeline>? OnResize;

        private TextureHandle? _colorTexHandle;
        private TextureHandle? _depthTexHandle;
        private List<IRenderPass> _renderPasses;

        private bool _initialized;
        private string _profileKey;
        private static int s_pipelineCounter;

        public GLRenderPipeline(Guid renderSettingsId, int width, int height, IEnumerable<IRenderPass> renderPasses)
        {
            RenderSettingsId = renderSettingsId;
            Width = width;
            Height = height;

            _renderPasses = new(renderPasses);
            _profileKey = "RenderPipeline_" + s_pipelineCounter++;
        }

        public void Initialize(ICommandHost host)
        {
            if (_initialized) {
                throw new InvalidOperationException("Render pipeline has been initialized");
            }

            UniformBufferHandle = GL.GenBuffer();
            FramebufferHandle = GL.GenFramebuffer();

            GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle);
            GL.BufferData(BufferTargetARB.UniformBuffer, 12, IntPtr.Zero, BufferUsageARB.DynamicDraw);

            foreach (var pass in CollectionsMarshal.AsSpan(_renderPasses)) {
                try {
                    pass.Initialize(host, this);
                }
                catch (Exception e) {
                    Console.WriteLine($"[{_profileKey}] Failed to initialize render pass '{pass}': " + e);
                }
            }

            _initialized = true;
        }

        public void Uninitialize(ICommandHost host)
        {
            if (!_initialized) {
                throw new InvalidOperationException("Render pipeline has been uninitialized");
            }
            _initialized = false;

            foreach (var pass in CollectionsMarshal.AsSpan(_renderPasses)) {
                try {
                    pass.Uninitialize(host, this);
                }
                catch (Exception e) {
                    Console.WriteLine($"[{_profileKey}] Failed to uninitialize render pass '{pass}': " + e);
                }
            }

            DeleteTextures();
            GL.DeleteFramebuffer(FramebufferHandle);
            GL.DeleteBuffer(UniformBufferHandle);

            FramebufferHandle = FramebufferHandle.Zero;
            UniformBufferHandle = BufferHandle.Zero;
        }

        public void Render(ICommandHost host, MeshGroup meshGroup)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, UniformBufferHandle);

            foreach (var pass in CollectionsMarshal.AsSpan(_renderPasses)) {
                try {
                    using (host.Profile(_profileKey, pass)) {
                        pass.Render(host, this, meshGroup);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"[{_profileKey}] Failed to execute render pass '{pass}': " + e);
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

            DeleteTextures();
            OnResize?.Invoke(host, this);
        }

        private void DeleteTextures()
        {
            if (_colorTexHandle.HasValue) {
                GL.DeleteTexture(_colorTexHandle.Value);
                _colorTexHandle = null;
            }
            if (_depthTexHandle.HasValue) {
                GL.DeleteTexture(_depthTexHandle.Value);
                _depthTexHandle = null;
            }
        }
    }
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;
        public RenderSettings? Resource;
        public GLRenderPipeline? RenderPipeline;
        public Guid? SkyboxId;

        public int Width;
        public int Height;

        public override Guid? Id => RenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<RenderSettingsData>(RenderSettingsId, out bool exists);
            if (exists) {
                data.RenderPipeline.Uninitialize(host);
            }

            data.RenderPipeline = RenderPipeline!;
            data.RenderPipeline.Initialize(host);

            data.IsCompositionEnabled = Resource!.IsCompositionEnabled;
            data.SkyboxId = SkyboxId;
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;
        public int Width;
        public int Height;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.RequireOrNullRef<RenderSettingsData>(RenderSettingsId);
            if (Unsafe.IsNullRef(ref data)) { return; }
            data.RenderPipeline.Resize(host, Width, Height);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<RenderSettingsData>(RenderSettingsId, out var data)) {
                data.RenderPipeline.Uninitialize(host);
            }
        }
    }

    private int _width;
    private int _height;

    public void OnLoad(IContext context)
    {
        ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
        _width = spec.Width;
        _height = spec.Height;
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();

        if (spec.IsResizable) {
            _width = width;
            _height = height;
        }
        else if (_height == spec.Width && _height == spec.Height) {
            return;
        }
        else {
            _width = spec.Width;
            _height = spec.Height;
        }

        foreach (var id in context.Query<Resource<RenderSettings>>()) {
            var resource = context.Inspect<Resource<RenderSettings>>(id).Value;
            if (resource == null || !resource.AutoResizeByWindow) {
                continue;
            }
            var cmd = ResizeCommand.Create();
            cmd.RenderSettingsId = id;
            cmd.Width = _width;
            cmd.Height = _height;
            context.SendCommandBatched(cmd);
        }
    }

    protected override void Initialize(IContext context, Guid id, RenderSettings resource, RenderSettings? prevResource)
    {
        if (prevResource != null) {
            ResourceLibrary.UnreferenceAll(context, id);
        }

        var cmd = InitializeCommand.Create();
        cmd.RenderSettingsId = id;
        cmd.Resource = resource;

        if (resource.AutoResizeByWindow) {
            cmd.Width = _width;
            cmd.Height = _height;
        }
        else {
            cmd.Width = resource.Width;
            cmd.Height = resource.Height;
        }

        var passes = CreatePasses(context, resource.RenderPipeline);
        cmd.RenderPipeline = new GLRenderPipeline(id, cmd.Width, cmd.Height, passes);

        if (resource.Skybox != null) {
            cmd.SkyboxId = ResourceLibrary.Reference(context, id, resource.Skybox);
        }

        context.SendCommandBatched(cmd);
    }

    private IEnumerable<IRenderPass> CreatePasses(IContext context, RenderPipeline pipeline)
    {
        foreach (var pass in pipeline.Passes) {
            IRenderPass? result = pass switch {
                RenderPass.ActivateMaterialBuiltInBuffers p => new ActivateMaterialBuiltInBuffersPass(),
                RenderPass.GenerateHiZBuffer p => new GenerateHiZBufferPass(),

                RenderPass.CullMeshesByFrustum p => new CullMeshesByFrustumPass { MeshFilter = p.MeshFilter },
                RenderPass.CullMeshesByHiZ p => new CullMeshesByFrustumPass { MeshFilter = p.MeshFilter },
                
                RenderPass.RenderDepth p => new RenderDepthPass { MeshFilter = p.MeshFilter },
                RenderPass.RenderOpaque p => new RenderOpaquePass { MeshFilter = p.MeshFilter },
                RenderPass.RenderTransparent p => new RenderTransparentPass { MeshFilter = p.MeshFilter },
                RenderPass.RenderBlending p => new RenderBlendingPass { MeshFilter = p.MeshFilter },

                RenderPass.RenderSkyboxCubemap p => new RenderSkyboxCubemapPass(),

                _ => null
            };

            if (result == null) {
                Console.WriteLine("[Camera] Unsupported render pass: " + pass);
                continue;
            }

            result.LoadResources(context);
            yield return result;
        }
    }

    protected override void Uninitialize(IContext context, Guid id, RenderSettings resource)
    {
        ResourceLibrary.UnreferenceAll(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderSettingsId = id;
        context.SendCommandBatched(cmd);
    }
}