namespace Nagule.Graphics.Backends.OpenTK;

using Nagule.Graphics.PostProcessing;
using Sia;

[NaAssetModule<REffectLayer, Tuple<EffectLayerState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManager<,,>))]
internal partial class EffectLayerModule;