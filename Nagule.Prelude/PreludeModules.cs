namespace Nagule.Prelude;

using Sia;

public class PreludeModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EventsModule>()
            .Add<UpdatorModule>()
            .Add<Spawner3DModule>()
            .Add<FirstPersonControllerModule>());