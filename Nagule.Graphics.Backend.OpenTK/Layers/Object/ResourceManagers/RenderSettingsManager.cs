namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class RenderSettingsManager : ResourceManagerBase<RenderSettings>,
    ILoadListener, IWindowResizeListener
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;
        public RenderSettings? Resource;
        public GLRenderPipeline? RenderPipeline;
        public GLCompositionPipeline? CompositionPipeline;
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

            if (CompositionPipeline != null) {
                data.CompositionPipeline = CompositionPipeline;
                data.CompositionPipeline.Initialize(host);
            }

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
            data.CompositionPipeline?.Resize(host, Width, Height);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            if (!host.Remove<RenderSettingsData>(RenderSettingsId, out var data)) {
                return;
            }
            data.RenderPipeline.Uninitialize(host);

            var cmd = UnloadResourcesCommand.Create();
            cmd.RenderPipeline = data.RenderPipeline;
            cmd.CompositionPipeline = data.CompositionPipeline;
            host.SendCommandBatched(cmd);
        }
    }

    private class UnloadResourcesCommand : Command<UnloadResourcesCommand, ContextTarget>
    {
        public IRenderPipeline? RenderPipeline;
        public ICompositionPipeline? CompositionPipeline;

        public override void Execute(ICommandHost host)
        {
            var context = (IContext)host;
            RenderPipeline!.UnloadResources(context);
            CompositionPipeline?.UnloadResources(context);
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

        cmd.RenderPipeline = new GLRenderPipeline(
            id, cmd.Width, cmd.Height,
            CreateRenderPasses(context, resource.RenderPipeline));
        cmd.RenderPipeline.LoadResources(context);

        if (resource.CompositionPipeline != null) {
            cmd.CompositionPipeline = new GLCompositionPipeline(
                id, CreateCompositionPasses(context, resource.CompositionPipeline));
            cmd.CompositionPipeline.LoadResources(context);
        }

        if (resource.Skybox != null) {
            cmd.SkyboxId = ResourceLibrary.Reference(context, id, resource.Skybox);
        }

        context.SendCommandBatched(cmd);
    }

    private IEnumerable<IRenderPass> CreateRenderPasses(IContext context, RenderPipeline pipeline)
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
                Console.WriteLine("[RenderSettings] Unsupported render pass: " + pass);
                continue;
            }
            yield return result;
        }
    }

    private IEnumerable<ICompositionPass> CreateCompositionPasses(IContext context, CompositionPipeline pipeline)
    {
        foreach (var pass in pipeline.Passes) {
            ICompositionPass? result = pass switch {
                CompositionPass.BlitColor p => new BlitColorPass(),
                CompositionPass.BlitDepth p => new BlitDepthPass(),

                CompositionPass.ACESToneMapping p => new ACESToneMappingPass(),
                CompositionPass.GammaCorrection p => new GammaCorrectionPass(p.Gamma),

                CompositionPass.Bloom p => new BloomPass(
                    threshold: p.Threshold,
                    intensity: p.Intensity,
                    radius: p.Radius,
                    dirtTexture: p.DirtTexture,
                    dirtIntensity: p.DirtIntensity),

                _ => null
            };

            if (result == null) {
                Console.WriteLine("[RenderSettings] Unsupported composition pass: " + pass);
                continue;
            }
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