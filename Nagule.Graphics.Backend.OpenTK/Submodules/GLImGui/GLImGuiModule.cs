namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class GLImGuiModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<ImGuiLayerModule>());