namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public static class RenderPipelineUtils
{
    public static RenderPassChain ConstructChain(
        EntityRef nodeEntity, in RenderSettings settings, RenderPassChain? initialChain = null)
    {
        var chain = initialChain ?? RenderPassChain.Empty;

        foreach (var featureEntity in nodeEntity.Get<NodeFeatures>()) {
            ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
            if (Unsafe.IsNullRef(ref provider)) {
                continue;
            }
            if (provider.Instance != null) {
                chain = provider.Instance.TransformPipeline(chain, settings);
            }
        }

        return chain;
    }
}