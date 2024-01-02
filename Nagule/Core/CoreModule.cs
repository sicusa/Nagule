namespace Nagule;

using Sia;

public class CoreModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<LogModule>()
            .Add<GuidModule>()
            .Add<NameModule>()
            .Add<ObjectModule>()
            .Add<HangableModule>()
            .Add<AssetModule>()
            .Add<TransformModule>()
            .Add<NodeModule>()
            .Add<PeripheralModule>()
            .Add<ApplicationModule>()
            .Add<ProfilerModule>());