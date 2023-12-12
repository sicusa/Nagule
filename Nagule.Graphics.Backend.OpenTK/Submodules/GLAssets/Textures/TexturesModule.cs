namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class TexturesModule : SystemBase
{
    public TexturesModule()
    {
        Children = SystemChain.Empty
            .Add<Texture2DModule>()
            .Add<CubemapModule>()
            .Add<RenderTexture2DModule>();
    }
}