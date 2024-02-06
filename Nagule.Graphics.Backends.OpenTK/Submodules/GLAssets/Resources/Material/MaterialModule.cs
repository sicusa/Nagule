namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

[NaAssetModule<RMaterial, Bundle<MaterialState, MaterialReferences>>(
    typeof(GraphicsAssetManager<,,>))]
internal partial class MaterialModule;