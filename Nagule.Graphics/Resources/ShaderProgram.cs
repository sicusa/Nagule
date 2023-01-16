namespace Nagule.Graphics;

using System.Collections.Immutable;

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

public record ShaderProgram : ResourceBase
{
    public static ShaderProgram NonMaterial { get; }
        = new ShaderProgram { IsMaterialTexturesEnabled = false };
    
    public ImmutableDictionary<ShaderType, string> Shaders { get; init; }
        = ImmutableDictionary<ShaderType, string>.Empty;
    public ImmutableHashSet<string> TransformFeedbackVaryings { get; init; }
        = ImmutableHashSet<string>.Empty;
    public bool IsMaterialTexturesEnabled { get; init; } = true;
    public ImmutableDictionary<string, ShaderParameterType> CustomParameters { get; init; }
        = ImmutableDictionary<string, ShaderParameterType>.Empty;
    public ImmutableDictionary<ShaderType, ImmutableArray<string>> Subroutines { get; init; }
        = ImmutableDictionary<ShaderType, ImmutableArray<string>>.Empty;

    public ShaderProgram WithShader(ShaderType shaderType, string source)
        => this with { Shaders = Shaders.SetItem(shaderType, source) };
    public ShaderProgram WithShaders(params KeyValuePair<ShaderType, string>[] shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };
    public ShaderProgram WithParameters(IEnumerable<KeyValuePair<ShaderType, string>> shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };

    public ShaderProgram WithTransformFeedbackVarying(string varying)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Add(varying) };
    public ShaderProgram WithTransformFeedbackVaryings(params string[] varyings)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Union(varyings) };
    public ShaderProgram WithTransformFeedbackVaryings(IEnumerable<string> varyings)
        => this with { TransformFeedbackVaryings = TransformFeedbackVaryings.Union(varyings) };

    public ShaderProgram WithParameter(string name, ShaderParameterType parameterType)
        => this with { CustomParameters = CustomParameters.SetItem(name, parameterType) };
    public ShaderProgram WithParameters(params KeyValuePair<string, ShaderParameterType>[] parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };
    public ShaderProgram WithParameters(IEnumerable<KeyValuePair<string, ShaderParameterType>> parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };

    public ShaderProgram WithSubroutine(ShaderType shaderType, ImmutableArray<string> names)
        => this with { Subroutines = Subroutines.SetItem(shaderType, names) };
    public ShaderProgram WithSubroutines(params KeyValuePair<ShaderType, ImmutableArray<string>>[] subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
    public ShaderProgram WithSubroutines(IEnumerable<KeyValuePair<ShaderType, ImmutableArray<string>>> subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
}