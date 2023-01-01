namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Local;

using Nagule.Graphics;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new RenderThreadSynchronizer(),

            new DefaultRenderPipelineInitializer(),
            new DefaultTextureLoader(),
            new EmbededShaderProgramsLoader(),

            new RenderPipelineManager(),
            new GraphNodeManager(),
            new CameraManager(),
            new LightManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new TextureManager(),
            new RenderTextureManager(),
            new ShaderProgramManager(),

            new MeshUniformBufferUpdator(),
            new LightingEnvUniformBufferUpdator(),

            new LightsBufferUpdator(),
            new MeshRenderableBufferUpdator(),

            new ForwardRenderPipeline(),
            new ImGuiRenderer())
    {
    }
}