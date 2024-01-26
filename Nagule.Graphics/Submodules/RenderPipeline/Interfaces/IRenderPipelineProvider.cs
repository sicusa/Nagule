namespace Nagule.Graphics;

using Sia;

public interface IRenderPipelineProvider
{
    RenderPassChain TransformPipeline(in EntityRef entity, RenderPassChain chain);
}