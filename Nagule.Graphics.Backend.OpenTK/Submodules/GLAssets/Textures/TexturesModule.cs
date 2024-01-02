namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class TexturesModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<Texture2DModule>()
            .Add<CubemapModule>()
            .Add<RenderTexture2DModule>());