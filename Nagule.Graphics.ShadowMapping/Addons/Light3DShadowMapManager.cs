namespace Nagule.Graphics.ShadowMapping;

using Sia;

public class Light3DShadowMapManager : AssetManagerBase<Light3D, Light3DShadowMapState>
{
    private readonly RCamera3D s_shadowSamplerCamera = new() {
        ProjectionMode = ProjectionMode.Orthographic,
        ClearFlags = ClearFlags.Depth,
        OrthographicWidth = 10f,
        FarPlaneDistance = 100f,

        Settings = new RRenderSettings {
            PipelineProvider = ShadowMapPipelineProvider.Instance,
            Resolution = (1024, 1024)
        },
        Priority = RenderPriority.DepthSampler,
        Target = RenderTarget.None
    };

    public override void LoadAsset(in EntityRef entity, ref Light3D asset, EntityRef stateEntity)
    {
        if (!asset.IsShadowEnabled) {
            return;
        }

        var nodeEntity = entity.GetFeatureNode();
        var cameraEntity = FeatureUtils.CreateEntity(World, s_shadowSamplerCamera, nodeEntity)!.Value;

        ref var state = ref stateEntity.Get<Light3DShadowMapState>();
        state.ShadowSamplerCamera = cameraEntity;
    }
}