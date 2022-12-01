namespace Nagule.Backend.OpenTK;

using Aeco.Local;

using Nagule.Graphics;

using Nagule.Backend.OpenTK.Graphics;

public class OpenTKGraphics : CompositeLayer
{
    public OpenTKGraphics()
        : base(
            new DefaultRenderTargetLoader(),
            new DefaultTextureLoader(),
            new EmbededShaderProgramsLoader(),

            new RenderTargetManager(),
            new GraphNodeManager(),
            new LightManager(),
            new MeshRenderableManager(),
            new MeshManager(),
            new MaterialManager(),
            new TextureManager(),
            new ShaderProgramManager(),

            new CameraMatricesUpdator(),

            new MeshUniformBufferUpdator(),
            new LightingEnvUniformBufferUpdator(),
            new CameraUniformBufferUpdator(),

            new LightsBufferUpdator(),
            new MeshRenderableUpdator(),

            new ForwardRenderPipeline())
    {
    }
}