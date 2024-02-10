namespace Nagule.Graphics.PostProcessing;

using Sia;

[NaAssetModule<REffectLayer, Bundle<EffectLayerState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManagerBase<,>))]
internal partial class EffectLayerModule;