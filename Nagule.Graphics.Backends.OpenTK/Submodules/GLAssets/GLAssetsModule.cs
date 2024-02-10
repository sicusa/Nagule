namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class GLAssetsModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<ResourcesModule>()
            .Add<FeaturesModule>()
            .Add<SettingsModule>());