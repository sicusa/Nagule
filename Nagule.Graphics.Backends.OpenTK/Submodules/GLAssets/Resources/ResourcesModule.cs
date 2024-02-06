namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class ResourcesModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<GLSLProgramModule>()
            .Add<TexturesModule>()
            .Add<MaterialModule>());