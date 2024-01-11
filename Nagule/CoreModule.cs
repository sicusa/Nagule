namespace Nagule;

using Sia;

public class CoreModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<GuidModule>()
            .Add<NameModule>()
            .Add<HangableModule>()
            .Add<AssetSystemModule>()
            .Add<TransformModule>()
            .Add<NodeModule>()
            .Add<PeripheralModule>()
            .Add<ApplicationModule>()
            .Add<ProfilerModule>());