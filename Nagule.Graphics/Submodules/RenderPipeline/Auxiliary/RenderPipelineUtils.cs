namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public static class RenderPipelineUtils
{
    public static RenderPassChain ConstructPasses(EntityRef node)
    {
        var passes = RenderPassChain.Empty;

        foreach (var featureEntity in node.GetFeatures()) {
            ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
            if (Unsafe.IsNullRef(ref provider)) {
                continue;
            }
            if (provider.Instance != null) {
                passes = provider.Instance.TransformPipeline(featureEntity, passes);
            }
        }

        return passes;
    }
}