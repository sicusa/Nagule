namespace Nagule.Graphics;

public interface IRenderPipelineProvider
{
    RenderPassChain TransformPipeline(RenderPassChain chain);
}