namespace Nagule.Graphics.Backend.OpenTK;

using Aeco.Local;

using Nagule.Graphics;

using Nagule.Graphics.Backend.OpenTK;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new OpenGLSynchronizer(),

            new DefaultRenderTargetInitializer(),
            new DefaultTextureLoader(),
            new EmbededShaderProgramsLoader(),

            new RenderTargetManager(),
            new GraphNodeManager(),
            new LightManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new TextureManager(),
            new RenderTextureManager(),
            new ShaderProgramManager(),

            new CameraMatricesUpdator(),

            new MeshUniformBufferUpdator(),
            new LightingEnvUniformBufferUpdator(),
            new CameraUniformBufferUpdator(),

            new LightsBufferUpdator(),
            new MeshRenderableBufferUpdator(),

            new ForwardRenderPipeline())
    {
    }
}