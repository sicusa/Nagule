namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[NaAssetModule<RMesh3D, Mesh3DState>(typeof(GraphicsAssetManager<,,>))]
internal partial class Mesh3DModule : AssetModuleBase;