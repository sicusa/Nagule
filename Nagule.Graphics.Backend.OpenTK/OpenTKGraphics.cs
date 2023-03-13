namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Local;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new DefaultMaterialLoader(),

            new GraphNodeManager(),
            new LightManager(),
            new LightingEnvironmentManager(),
            new CameraManager(),
            new RenderSettingsManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new CubemapManager(),
            new TextureManager(),
            new RenderTextureManager(),
            new GLSLProgramManager(),

            new LightsBufferUpdator(),
            new LightClustersBufferUpdator(),
            new MeshRenderableBufferUpdator(),

            new CameraRenderer(),
            new ImGuiRenderer(),

            new GraphicsCommandExecutor())
    {
    }
}