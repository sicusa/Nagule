namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class OpenTKWindowInitializeSystem()
    : SystemBase(
        matcher: Matchers.Of<
            Window,
            OpenTKWindow,
            GraphicsContext,
            SimulationContext>(),
        trigger: EventUnion.Of<WorldEvents.Add>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            var window = new OpenTKNativeWindow(world, entity);
            entity.Get<OpenTKWindow>().Native = window;
        }
    }
}

public class OpenTKWindowModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<OpenTKWindowInitializeSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<OpenTKStyleUpdator>(world);
    }
}