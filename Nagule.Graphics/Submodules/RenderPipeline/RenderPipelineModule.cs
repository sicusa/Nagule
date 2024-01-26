namespace Nagule.Graphics;

using Sia;

[NaAssetModule<RRenderPipeline, RenderPipelineState>(typeof(GraphicsAssetManager<,,>))]
public partial class RenderPipelineModule
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<PipelineRenderer>(world);
    }
}