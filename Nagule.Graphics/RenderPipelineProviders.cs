namespace Nagule.Graphics;

public static class RenderPipelineProviders
{
    public sealed record Const(RenderPassChain Chain) : IRenderPipelineProvider
    {
        public RenderPassChain TransformPipeline(
            RenderPassChain otherChain, in RenderSettings settings)
            => otherChain.Concat(Chain);
    }
}