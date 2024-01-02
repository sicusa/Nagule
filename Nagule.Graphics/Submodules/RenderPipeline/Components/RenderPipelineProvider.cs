namespace Nagule.Graphics;

using Sia;

public interface IRenderPipelineProvider
{
    SystemChain TransformPipeline(in EntityRef entity, SystemChain chain);
}

public record struct RenderPipelineProvider(IRenderPipelineProvider? Instance);