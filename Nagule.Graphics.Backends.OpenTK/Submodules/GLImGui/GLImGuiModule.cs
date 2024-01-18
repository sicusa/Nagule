namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

internal class GLImGuiModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<ImGuiLayerModule>());