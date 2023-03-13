namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

public class LightingEnvironmentManager : ResourceManagerBase<LightingEnvironment>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public uint LightingEnvironmentId;
        public LightingEnvironment? Resource;
        public uint ShadowMapRenderSettingsId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<LightingEnvironmentData>(LightingEnvironmentId, out bool exists);

            data.ShadowMapTexturePool = new(
                GLInternalFormat.DepthComponent16, GLPixelFormat.DepthComponent, GLPixelType.UnsignedShort,
                Resource!.ShadowMapWidth, Resource.ShadowMapHeight);
            
            data.ShadowMapCubemapPool = new(
                GLInternalFormat.DepthComponent16, GLPixelFormat.DepthComponent, GLPixelType.UnsignedShort,
                Resource.ShadowMapWidth, Resource.ShadowMapHeight);
            
            data.ShadowMapRenderSettingsId = ShadowMapRenderSettingsId;
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint LightingEnvironmentId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<LightingEnvironmentData>(LightingEnvironmentId, out var data)) {
                data.ShadowMapTexturePool.Dispose();
                data.ShadowMapCubemapPool.Dispose();
            }
        }
    }

    private CompositionPipeline s_shadowMapCompositionPipeline = new() {
    };

    protected override void Initialize(IContext context, uint id, LightingEnvironment resource, LightingEnvironment? prevResource)
    {
        var cmd = InitializeCommand.Create();
        cmd.LightingEnvironmentId = id;
        cmd.Resource = resource;
        cmd.ShadowMapRenderSettingsId = context.GetResourceLibrary().Reference(id, new RenderSettings() {
            Width = resource.ShadowMapWidth,
            Height = resource.ShadowMapHeight,
            RenderPipeline = RenderPipeline.OpaqueShadowmap,
            CompositionPipeline = new() {
            }
        });
        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, uint id, LightingEnvironment resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.LightingEnvironmentId = id;
        context.SendCommandBatched(cmd);
    }
}