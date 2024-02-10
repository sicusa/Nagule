namespace Nagule.Graphics;

public static class RenderPipelineProviders
{
    public sealed class Const(RenderPassChain chain)
        : IRenderPipelineProvider
    {
        public RenderPassChain TransformPipeline(RenderPassChain otherChain)
            => otherChain.Concat(chain);
    }
}