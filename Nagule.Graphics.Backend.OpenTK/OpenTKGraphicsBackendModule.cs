namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[AfterSystem<GraphicsModule>]
public class OpenTKGraphicsBackendModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<OpenTKWindowModule>()
            .Add<GLAssetsModule>()
            .Add<GLInstancedModule>()
            .Add<GLPipelineModule>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        world.GetAddon<RenderFrame>().OnTaskExecuted += (task, argument) => {
            var error = GL.GetError();
            if (error != GLErrorCode.NoError) {
                Console.WriteLine(error);
            }
        };
    }
}