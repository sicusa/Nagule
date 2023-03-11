namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Reactive.Disposables;
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

        public Guid LightingEnvironmentId;
        public Guid? SkyboxId;

        public int Width;
        public int Height;

        public override Guid? Id => RenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<RenderSettingsData>(RenderSettingsId, out bool exists);
            if (exists) {
                data.RenderPipeline.Uninitialize(host);
                data.CompositionPipeline?.Uninitialize(host);

                var cmd = UnloadResourcesCommand.Create();
                cmd.RenderPipeline = data.RenderPipeline;
                cmd.CompositionPipeline = data.CompositionPipeline;
                host.SendCommandBatched(cmd);
            }

            data.RenderPipeline = RenderPipeline!;
            data.RenderPipeline.Initialize(host);
            data.RenderPipeline.Resize(host, Width, Height);

            if (CompositionPipeline != null) {
                data.CompositionPipeline = CompositionPipeline;
                data.CompositionPipeline.Initialize(host);
                data.CompositionPipeline.Resize(host, Width, Height);
            }

            data.LightingEnvironmentId = LightingEnvironmentId;
            data.SkyboxId = SkyboxId;
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public Guid RenderSettingsId;
        public int Width;
        public int Height;

        public override Guid? Id => RenderSettingsId;

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
            data.CompositionPipeline?.Uninitialize(host);

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
        ref readonly var spec = ref context.Inspect<GraphicsSpecification>();
        _width = spec.Width;
        _height = spec.Height;
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        ref readonly var spec = ref context.Inspect<GraphicsSpecification>();

        if (!spec.IsResizable) {
            return;
        }
        if (_height == width && _height == height) {
            return;
        }

        _width = width;
        _height = height;

        foreach (var id in context.Query<RenderSettingsProps>()) {
            ref readonly var props = ref context.Inspect<RenderSettingsProps>(id);
            if (!props.AutoResizeByWindow) {
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
        var resLib = context.GetResourceLibrary();
        
        if (prevResource != null) {
            resLib.UnreferenceAll(id);
        }

        RenderSettings.GetProps(context, id).Set(resource);

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

        cmd.LightingEnvironmentId = resLib.Reference(id, resource.LightingEnvironment);

        if (resource.Skybox != null) {
            cmd.SkyboxId = resLib.Reference(id, resource.Skybox);
        }

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable? Subscribe(IContext context, Guid id, RenderSettings resource)
    {
        var props = RenderSettings.GetProps(context, id);
        var resLib = context.GetResourceLibrary();

        return new CompositeDisposable(
            props.Width.SubscribeCommand<int, RenderTarget>(
                context, (host, width) => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    int height = data.RenderPipeline.Height;
                    data.RenderPipeline.Resize(host, width, height);
                    data.CompositionPipeline?.Resize(host, width, height);
                }),
            
            props.Height.SubscribeCommand<int, RenderTarget>(
                context, (host, height) => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    int width = data.RenderPipeline.Width;
                    data.RenderPipeline.Resize(host, width, height);
                    data.CompositionPipeline?.Resize(host, width, height);
                }),
            
            props.Skybox.Modified.Subscribe(tuple => {
                var (prevSkybox, skybox) = tuple;

                if (prevSkybox != null) {
                    resLib.Unreference(id, prevSkybox);
                }

                Guid? skyboxId = skybox != null
                    ? resLib.Reference(id, skybox)
                    : null;

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    data.SkyboxId = skyboxId;
                }));
            }),
            
            props.RenderPipeline.Subscribe(pipeline => {
                int width;
                int height;

                if (props.AutoResizeByWindow) {
                    width = _width;
                    height = _height;
                }
                else {
                    width = props.Width;
                    height = props.Height;
                }

                var glPipeline = new GLRenderPipeline(
                    id, width, height, CreateRenderPasses(context, pipeline));
                glPipeline.LoadResources(context);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<RenderSettingsData>(id);

                    var prevGLPipeline = data.RenderPipeline;
                    prevGLPipeline.Uninitialize(host);

                    context.SendCommandBatched<ContextTarget>(Command.Do(context => {
                        prevGLPipeline.UnloadResources((IContext)context);
                    }));

                    data.RenderPipeline = glPipeline;
                    data.RenderPipeline.Initialize(host);
                }));
            }),
            
            props.CompositionPipeline.Subscribe(pipeline => {
                var glPipeline = pipeline != null
                    ? new GLCompositionPipeline(id, CreateCompositionPasses(context, pipeline))
                    : null;
                glPipeline?.LoadResources(context);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<RenderSettingsData>(id);

                    var prevGLPipeline = data.CompositionPipeline;
                    if (prevGLPipeline != null) {
                        prevGLPipeline.Uninitialize(host);

                        context.SendCommandBatched<ContextTarget>(Command.Do(context => {
                            prevGLPipeline.UnloadResources((IContext)context);
                        }));
                    }

                    data.CompositionPipeline = glPipeline;
                    data.CompositionPipeline?.Initialize(host);
                }));
            })
        );
    }

    private IEnumerable<IRenderPass> CreateRenderPasses(IContext context, RenderPipeline pipeline)
    {
        foreach (var pass in pipeline.Passes) {
            IRenderPass? result = pass switch {
                RenderPass.ActivateMaterialBuiltInBuffers p => new ActivateMaterialBuiltInBuffersPass(),
                RenderPass.GenerateHiZBuffer p => new GenerateHiZBufferPass(),

                RenderPass.CullMeshesByFrustum p => new CullMeshesByFrustumPass { MeshFilter = p.MeshFilter },
                RenderPass.CullMeshesByHiZ p => new CullMeshesByHiZPass { MeshFilter = p.MeshFilter },
                
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

                CompositionPass.Brightness p => new BrightnessPass(p.Value),

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
        context.GetResourceLibrary().UnreferenceAll(id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderSettingsId = id;
        context.SendCommandBatched(cmd);
    }
}