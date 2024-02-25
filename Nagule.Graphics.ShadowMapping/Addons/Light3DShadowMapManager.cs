namespace Nagule.Graphics.ShadowMapping;

using Sia;

public class Light3DShadowMapManager : AssetManagerBase<Light3D, Light3DShadowMapState>
{
    private static readonly RRenderSettings s_directionalLightRenderSettings = new() {
        PipelineProvider = ShadowMapPipelineProvider.Instance,
        Resolution = (1024, 1024)
    };

    private static readonly RCamera3D s_directionalLightCamera = new() {
        ProjectionMode = ProjectionMode.Orthographic,
        ClearFlags = ClearFlags.Depth,

        OrthographicWidth = 10f,
        FarPlaneDistance = 1000f,

        Settings = s_directionalLightRenderSettings,
        Priority = RenderPriority.DepthSampler,
        Target = RenderTarget.None
    };

    private ShadowMapLibrary _lib = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _lib = world.GetAddon<ShadowMapLibrary>();
    }

    public override void LoadAsset(in EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        if (!asset.IsShadowEnabled) {
            return;
        }

        ref var state = ref stateEntity.Get<Light3DShadowMapState>();

        if (asset.Type == LightType.Directional) {
            var nodeEntity = entity.GetFeatureNode();
            var handle = _lib.Allocate(entity);
            var cameraRecord = s_directionalLightCamera with {
                Target = new RenderTarget.Tileset2D(_lib.TilesetRecord, handle.Index)
            };
            state.ShadowMapHandle = handle;
            state.ShadowSamplerCamera =
                FeatureUtils.CreateEntity(World, cameraRecord, nodeEntity)!.Value;
        }
    }
}