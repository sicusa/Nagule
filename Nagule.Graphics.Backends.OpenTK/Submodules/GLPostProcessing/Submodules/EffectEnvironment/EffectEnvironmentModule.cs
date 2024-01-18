namespace Nagule.Graphics.Backends.OpenTK;

using Nagule.Graphics.PostProcessing;
using Sia;

[NaAssetModule<REffectEnvironment, Tuple<EffectEnvironmentState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManager<,,>))]
internal partial class EffectEnvironmentModule;