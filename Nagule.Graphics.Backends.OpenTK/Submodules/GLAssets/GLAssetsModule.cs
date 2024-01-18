namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class GLAssetsModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<GLSLProgramModule>()
            .Add<TexturesModule>()
            .Add<MaterialModule>()
            .Add<RenderSettingsModule>()
            .Add<RenderFeaturesModule>()
            .Add<GLPostProcessingModule>());