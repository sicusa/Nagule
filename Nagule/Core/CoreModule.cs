namespace Nagule;

using Sia;

public class CoreModule : SystemBase
{
    public CoreModule()
    {
        Children = SystemChain.Empty
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
            .Add<ProfilerModule>();
    }
}