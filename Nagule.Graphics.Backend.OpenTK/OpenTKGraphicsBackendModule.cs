namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[AfterSystem<GraphicsModule>]
public class OpenTKGraphicsBackendModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<OpenTKWindowModule>()
            .Add<GLAssetsModule>()
            .Add<GLImGuiModule>()
            .Add<GLInstancedModule>()
            .Add<GLPipelineModule>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var renderFrame = world.GetAddon<RenderFrame>();
        
        renderFrame.StackTraceEnabled = false;
        renderFrame.OnTaskExecuted += entry => {
            var error = GL.GetError();
            if (error != GLErrorCode.NoError) {
                Console.WriteLine(error);
                if (entry.StackTrace != null) {
                    Console.WriteLine(entry.StackTrace);
                }
            }
        };
    }
}