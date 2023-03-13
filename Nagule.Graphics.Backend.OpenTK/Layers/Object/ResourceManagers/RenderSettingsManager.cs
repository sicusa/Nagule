namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Generic;
using System.Reactive.Disposables;

public class RenderSettingsManager : ResourceManagerBase<RenderSettings>,
    ILoadListener, IWindowResizeListener
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public uint RenderSettingsId;
        public RenderSettings? Resource;
        public RenderPipelineImpl? RenderPipeline;
        public CompositionPipelineImpl? CompositionPipeline;

        public uint? SkyboxId;

        public int Width;
        public int Height;

        public int WindowWidth;
        public int WindowHeight;

        public override uint? Id => RenderSettingsId;

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

            data.AutoResizeByWindow = Resource!.AutoResizeByWindow;

            data.RenderPipeline = RenderPipeline!;
            data.RenderPipeline.Initialize(host);
            data.RenderPipeline.Resize(host, Width, Height);

            if (CompositionPipeline != null) {
                data.CompositionPipeline = CompositionPipeline;
                data.CompositionPipeline.Initialize(host);
                data.CompositionPipeline.SetViewportSize(WindowWidth, WindowHeight);
            }

            data.SkyboxId = SkyboxId;
        }
    }

    private class UpdateWindowSizeCommand : Command<UpdateWindowSizeCommand, RenderTarget>
    {
        public int Width;
        public int Height;

        public override uint? Id { get; } = 0;

        public override void Execute(ICommandHost host)
        {
            foreach (var id in host.Query<RenderSettingsData>()) {
                ref var data = ref host.Require<RenderSettingsData>(id);
                data.CompositionPipeline?.SetViewportSize(Width, Height);
            }
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public int Width;
        public int Height;

        public override uint? Id { get; } = 0;

        public override void Execute(ICommandHost host)
        {
            foreach (var id in host.Query<RenderSettingsData>()) {
                ref var data = ref host.Require<RenderSettingsData>(id);
                if (!data.AutoResizeByWindow) {
                    continue;
                }
                data.RenderPipeline.Resize(host, Width, Height);
            }
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint RenderSettingsId;

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
        _width = width;
        _height = height;

        var updateWindowSizeCmd = UpdateWindowSizeCommand.Create();
        updateWindowSizeCmd.Width = width;
        updateWindowSizeCmd.Height = height;
        context.SendCommandBatched(updateWindowSizeCmd);

        ref readonly var spec = ref context.Inspect<GraphicsSpecification>();
        if (spec.IsResizable) {
            var resizeCmd = ResizeCommand.Create();
            resizeCmd.Width = _width;
            resizeCmd.Height = _height;
            context.SendCommandBatched(resizeCmd);
        }
    }

    protected override void Initialize(IContext context, uint id, RenderSettings resource, RenderSettings? prevResource)
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

        cmd.RenderPipeline = new RenderPipelineImpl(
            id, cmd.Width, cmd.Height,
            CreateRenderPasses(context, resource.RenderPipeline));
        cmd.RenderPipeline.LoadResources(context);

        if (resource.CompositionPipeline != null) {
            cmd.WindowWidth = _width;
            cmd.WindowHeight = _height;

            cmd.CompositionPipeline = new CompositionPipelineImpl(
                id, CreateCompositionPasses(context, resource.CompositionPipeline));
            cmd.CompositionPipeline.LoadResources(context);
        }

        if (resource.Skybox != null) {
            cmd.SkyboxId = resLib.Reference(id, resource.Skybox);
        }

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable? Subscribe(IContext context, uint id, RenderSettings resource)
    {
        var props = RenderSettings.GetProps(context, id);
        var resLib = context.GetResourceLibrary();

        return new CompositeDisposable(
            props.Width.SubscribeCommand<int, RenderTarget>(
                context, (host, width) => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    int height = data.RenderPipeline.Height;
                    data.RenderPipeline.Resize(host, width, height);
                }),
            
            props.Height.SubscribeCommand<int, RenderTarget>(
                context, (host, height) => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    int width = data.RenderPipeline.Width;
                    data.RenderPipeline.Resize(host, width, height);
                }),
            
            props.AutoResizeByWindow.SubscribeCommand<bool, RenderTarget>(
                context, (host, autoResize) => {
                    ref var data = ref host.Require<RenderSettingsData>(id);
                    data.AutoResizeByWindow = autoResize;

                    if (autoResize) {
                        data.RenderPipeline.Resize(host, _width, _height);
                    }
                    else {
                        data.RenderPipeline.Resize(host, props.Width, props.Height);
                    }
                }),
            
            props.Skybox.Modified.Subscribe(tuple => {
                var (prevSkybox, skybox) = tuple;

                if (prevSkybox != null) {
                    resLib.Unreference(id, prevSkybox);
                }

                uint? skyboxId = skybox != null
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

                var pipelineImpl = new RenderPipelineImpl(
                    id, width, height, CreateRenderPasses(context, pipeline));
                pipelineImpl.LoadResources(context);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<RenderSettingsData>(id);

                    var prevPipelineImpl = data.RenderPipeline;
                    prevPipelineImpl.Uninitialize(host);

                    context.SendCommandBatched<ContextTarget>(Command.Do(context => {
                        prevPipelineImpl.UnloadResources((IContext)context);
                    }));

                    data.RenderPipeline = pipelineImpl;
                    data.RenderPipeline.Initialize(host);
                }));
            }),
            
            props.CompositionPipeline.Subscribe(pipeline => {
                var pipelineImpl = pipeline != null
                    ? new CompositionPipelineImpl(id, CreateCompositionPasses(context, pipeline))
                    : null;
                pipelineImpl?.LoadResources(context);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<RenderSettingsData>(id);

                    var prevPipelineImpl = data.CompositionPipeline;
                    if (prevPipelineImpl != null) {
                        prevPipelineImpl.Uninitialize(host);

                        context.SendCommandBatched<ContextTarget>(Command.Do(context => {
                            prevPipelineImpl.UnloadResources((IContext)context);
                        }));
                    }

                    data.CompositionPipeline = pipelineImpl;
                    data.CompositionPipeline?.Initialize(host);
                }));
            })
        );
    }

    private IEnumerable<IRenderPass> CreateRenderPasses(IContext context, RenderPipeline pipeline)
    {
        foreach (var pass in pipeline.Passes) {
            IRenderPass? result = pass switch {
                RenderPass.ActivateMaterialBuiltInBuffers => new ActivateMaterialBuiltInBuffersPassImpl(),
                RenderPass.GenerateHiZBuffer => new GenerateHiZBufferPassImpl(),

                RenderPass.CullMeshesByFrustum p => new CullMeshesByFrustumPassImpl { MeshFilter = p.MeshFilter },
                RenderPass.CullMeshesByHiZ p => new CullMeshesByHiZPassImpl { MeshFilter = p.MeshFilter },
                
                RenderPass.RenderDepth p => new RenderDepthPassImpl { MeshFilter = p.MeshFilter },
                RenderPass.RenderOpaque p => new RenderOpaquePassImpl { MeshFilter = p.MeshFilter },
                RenderPass.RenderTransparent p => new RenderTransparentPassImpl { MeshFilter = p.MeshFilter },
                RenderPass.RenderBlending p => new RenderBlendingPassImpl { MeshFilter = p.MeshFilter },

                RenderPass.RenderSkyboxCubemap => new RenderSkyboxCubemapPassImpl(),

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
                CompositionPass.SampleColor => new SampleColorPassImpl(),
                CompositionPass.SampleDepth => new SampleDepthPassImpl(),

                CompositionPass.BlitToDisplay => new BlitToDisplayPassImpl(),
                CompositionPass.BlitToRenderTexture p => new BlitToRenderTexturePassImpl(p.RenderTexture),

                CompositionPass.ACESToneMapping => new ACESToneMappingPassImpl(),
                CompositionPass.GammaCorrection p => new GammaCorrectionPassImpl(p.Gamma),

                CompositionPass.Brightness p => new BrightnessPassImpl(p.Value),

                CompositionPass.Bloom p => new BloomPassImpl(
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

    protected override void Uninitialize(IContext context, uint id, RenderSettings resource)
    {
        context.GetResourceLibrary().UnreferenceAll(id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderSettingsId = id;
        context.SendCommandBatched(cmd);
    }
}