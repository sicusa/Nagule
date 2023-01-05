namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Local;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new DefaultTextureLoader(),
            new EmbededShaderProgramsLoader(),

            new GraphNodeManager(),
            new CameraManager(),
            new RenderPipelineManager(),
            new LightManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new CubemapManager(),
            new TextureManager(),
            new RenderTextureManager(),
            new ShaderProgramManager(),

            new LightingEnvUniformBufferUpdator(),
            new LightsBufferUpdator(),
            new MeshRenderableBufferUpdator(),

            new RenderCommandExecutor(),
            new ForwardRenderPipeline(),
            new ImGuiRenderer(),
            new RenderThreadSynchronizer())
    {
    }
}