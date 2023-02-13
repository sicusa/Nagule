namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Local;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new EmbededShaderProgramsLoader(),
            new DefaultMaterialLoader(),

            new GraphNodeManager(),
            new CameraManager(),
            new RenderSettingsManager(),
            new LightManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new CubemapManager(),
            new TextureManager(),
            new RenderTextureManager(),
            new GLSLProgramManager(),

            new LightsBufferUpdator(),
            new LightingEnvUniformBufferUpdator(),
            new MeshRenderableBufferUpdator(),

            new CameraRenderer(),
            new ImGuiRenderer(),

            new GraphicsCommandExecutor())
    {
    }
}