namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class OpenTKWindowInitializeSystem : SystemBase
{
    public OpenTKWindowInitializeSystem()
    {
        Matcher = Matchers.Of<
            Window,
            OpenTKWindow,
            GraphicsContext,
            SimulationContext,
            Keyboard,
            Mouse
        >();
        Trigger = EventUnion.Of<WorldEvents.Add>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(world, (world, entity) => {
            var window = new OpenTKNativeWindow(world, entity);
            entity.Get<OpenTKWindow>().Native = window;
        });
    }
}

public class OpenTKWindowModule : SystemBase
{
    public OpenTKWindowModule()
    {
        Children = SystemChain.Empty
            .Add<OpenTKWindowInitializeSystem>();
    }
}