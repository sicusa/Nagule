namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class SettingsModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<RenderSettingsModule>());