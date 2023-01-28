namespace Nagule.Graphics;

using System.Collections.Immutable;

public record GLSLProgram : ResourceBase
{
    public ImmutableDictionary<ShaderType, string> Shaders { get; init; }
        = ImmutableDictionary<ShaderType, string>.Empty;
    public ImmutableHashSet<string> Macros { get; init; }
        = ImmutableHashSet<string>.Empty;
    public ImmutableDictionary<string, ShaderParameterType> Parameters { get; init; }
        = ImmutableDictionary<string, ShaderParameterType>.Empty;
    public ImmutableHashSet<string> TextureSlots { get; init; }
        = ImmutableHashSet<string>.Empty;
    public ImmutableHashSet<string> Feedbacks { get; init; }
        = ImmutableHashSet<string>.Empty;
    public ImmutableDictionary<ShaderType, ImmutableArray<string>> Subroutines { get; init; }
        = ImmutableDictionary<ShaderType, ImmutableArray<string>>.Empty;

    public GLSLProgram WithShader(ShaderType shaderType, string source)
        => this with { Shaders = Shaders.SetItem(shaderType, source) };
    public GLSLProgram WithShaders(params KeyValuePair<ShaderType, string>[] shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };
    public GLSLProgram WithShaders(IEnumerable<KeyValuePair<ShaderType, string>> shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };

    public GLSLProgram WithMacro(string macro)
        => this with { Macros = Macros.Add(macro) };
    public GLSLProgram WithMacros(params string[] macros)
        => this with { Macros = Macros.Union(macros) };
    public GLSLProgram WithMacros(IEnumerable<string> macros)
        => this with { Macros = Macros.Union(macros) };

    public GLSLProgram WithParameter(string name, ShaderParameterType parameterType)
        => this with { Parameters = Parameters.SetItem(name, parameterType) };
    public GLSLProgram WithParameters(params KeyValuePair<string, ShaderParameterType>[] parameters)
        => this with { Parameters = Parameters.SetItems(parameters) };
    public GLSLProgram WithParameters(IEnumerable<KeyValuePair<string, ShaderParameterType>> parameters)
        => this with { Parameters = Parameters.SetItems(parameters) };

    public GLSLProgram WithTextureSlot(string slot)
        => this with { TextureSlots = TextureSlots.Add(slot) };
    public GLSLProgram WithTextureSlots(params string[] slots)
        => this with { TextureSlots = TextureSlots.Union(slots) };
    public GLSLProgram WithTextureSlots(IEnumerable<string> slots)
        => this with { TextureSlots = TextureSlots.Union(slots) };

    public GLSLProgram WithFeedback(string feedback)
        => this with { Feedbacks = Feedbacks.Add(feedback) };
    public GLSLProgram WithFeedbacks(params string[] feedbacks)
        => this with { Feedbacks = Feedbacks.Union(feedbacks) };
    public GLSLProgram WithFeedbacks(IEnumerable<string> feedbacks)
        => this with { Feedbacks = Feedbacks.Union(feedbacks) };

    public GLSLProgram WithSubroutine(ShaderType shaderType, ImmutableArray<string> names)
        => this with { Subroutines = Subroutines.SetItem(shaderType, names) };
    public GLSLProgram WithSubroutines(params KeyValuePair<ShaderType, ImmutableArray<string>>[] subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
    public GLSLProgram WithSubroutines(IEnumerable<KeyValuePair<ShaderType, ImmutableArray<string>>> subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
}