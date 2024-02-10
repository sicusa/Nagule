namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public static class RenderPipelineUtils
{
    public static RenderPassChain ConstructFeaturePasses(EntityRef node, RenderPassChain? initialChain = null)
    {
        var chain = initialChain ?? RenderPassChain.Empty;

        foreach (var featureEntity in node.Get<NodeFeatures>()) {
            ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
            if (Unsafe.IsNullRef(ref provider)) {
                continue;
            }
            if (provider.Instance != null) {
                chain = provider.Instance.TransformPipeline(chain);
            }
        }

        return chain;
    }
}