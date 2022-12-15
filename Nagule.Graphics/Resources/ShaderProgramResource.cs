namespace Nagule.Graphics;

using System.Collections.Immutable;

using Aeco;

public enum ShaderType
{
    Fragment,
    Vertex,
    Geometry,
    TessellationEvaluation,
    TessellationControl,
    Compute,
    Unknown
}

public record ShaderProgramResource : ResourceBase
{
    public readonly static ShaderProgramResource NonMaterial
        = new ShaderProgramResource { IsMaterialTexturesEnabled = false };
    
    public ImmutableDictionary<ShaderType, string> Shaders { get; init; }
        = ImmutableDictionary<ShaderType, string>.Empty;
    public ImmutableHashSet<string> TransformFeedbackVaryings { get; init; }
        = ImmutableHashSet<string>.Empty;
    public bool IsMaterialTexturesEnabled { get; init; } = true;
    public ImmutableDictionary<string, ShaderParameterType> CustomParameters { get; init; }
        = ImmutableDictionary<string, ShaderParameterType>.Empty;
    public ImmutableDictionary<ShaderType, ImmutableArray<string>> Subroutines { get; init; }
        = ImmutableDictionary<ShaderType, ImmutableArray<string>>.Empty;

    public ShaderProgramResource WithShader(ShaderType shaderType, string source)
        => this with { Shaders = Shaders.SetItem(shaderType, source) };
    public ShaderProgramResource WithShaders(params KeyValuePair<ShaderType, string>[] shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };
    public ShaderProgramResource WithParameters(IEnumerable<KeyValuePair<ShaderType, string>> shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };

    public ShaderProgramResource WithTransformFeedbackVarying(string varying)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Add(varying) };
    public ShaderProgramResource WithTransformFeedbackVaryings(params string[] varyings)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Union(varyings) };
    public ShaderProgramResource WithTransformFeedbackVaryings(IEnumerable<string> varyings)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Union(varyings) };

    public ShaderProgramResource WithParameter(string name, ShaderParameterType parameterType)
        => this with { CustomParameters = CustomParameters.SetItem(name, parameterType) };
    public ShaderProgramResource WithParameters(params KeyValuePair<string, ShaderParameterType>[] parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };
    public ShaderProgramResource WithParameters(IEnumerable<KeyValuePair<string, ShaderParameterType>> parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };

    public ShaderProgramResource WithSubroutine(ShaderType shaderType, ImmutableArray<string> names)
        => this with { Subroutines = Subroutines.SetItem(shaderType, names) };
    public ShaderProgramResource WithSubroutines(params KeyValuePair<ShaderType, ImmutableArray<string>>[] subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
    public ShaderProgramResource WithSubroutines(IEnumerable<KeyValuePair<ShaderType, ImmutableArray<string>>> subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
}