namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class OpenTKWindowInitializeSystem()
    : SystemBase(
        matcher: Matchers.Of<
            Window,
            OpenTKWindow,
            GraphicsContext,
            SimulationContext,
            Keyboard,
            Mouse>(),
        trigger: EventUnion.Of<WorldEvents.Add>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(world, (world, entity) => {
            var window = new OpenTKNativeWindow(world, entity);
            entity.Get<OpenTKWindow>().Native = window;
        });
    }
}

public class OpenTKWindowModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<OpenTKWindowInitializeSystem>());