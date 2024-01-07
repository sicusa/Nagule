namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[NaAssetModule<RMaterial, Tuple<MaterialState, MaterialReferences>>(
    typeof(GraphicsAssetManager<,,>))]
internal partial class MaterialModule;