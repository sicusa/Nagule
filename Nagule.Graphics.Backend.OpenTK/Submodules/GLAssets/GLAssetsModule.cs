namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class GLAssetsModule : SystemBase
{
    public GLAssetsModule()
    {
        Children = SystemChain.Empty
            .Add<GLSLProgramModule>()
            .Add<TexturesModule>()
            .Add<MaterialModule>()
            .Add<RenderSettingsModule>()
            .Add<RenderFeaturesModule>();
    }
}