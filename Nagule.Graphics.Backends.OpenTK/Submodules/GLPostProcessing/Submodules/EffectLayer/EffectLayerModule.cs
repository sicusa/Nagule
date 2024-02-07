namespace Nagule.Graphics.Backends.OpenTK;

using Nagule.Graphics.PostProcessing;
using Sia;

[NaAssetModule<REffectLayer, Bundle<EffectLayerState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManagerBase<,>))]
internal partial class EffectLayerModule;