namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

[AfterSystem<GraphicsModule>]
public class OpenTKGraphicsBackendsModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<OpenTKWindowModule>()
            .Add<GLAssetsModule>()
            .Add<GLImGuiModule>()
            .Add<GLInstancedModule>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var renderFramer = world.GetAddon<RenderFramer>();
        
        renderFramer.OnTaskExecuted += entry => {
            var error = GL.GetError();
            if (error != GLErrorCode.NoError) {
                Console.WriteLine(error);
            }
        };
    }
}