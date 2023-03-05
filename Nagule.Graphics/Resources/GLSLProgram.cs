namespace Nagule.Graphics;

using System.Collections.Immutable;

public record GLSLProgram : ResourceBase
{
    public static GLSLProgram Standard { get; } =
        new GLSLProgram { Name = "nagule.blinn_phong" }
            .WithShaders(
                new(ShaderType.Vertex, GraphicsHelper.LoadEmbededShader("blinn_phong.vert.glsl")),
                new(ShaderType.Fragment, GraphicsHelper.LoadEmbededShader("blinn_phong.frag.glsl")))
            .WithParameters(
                new(MaterialKeys.Tiling),
                new(MaterialKeys.Offset),
                new(MaterialKeys.Diffuse),
                new(MaterialKeys.DiffuseTex),
                new(MaterialKeys.Specular),
                new(MaterialKeys.SpecularTex),
                new(MaterialKeys.RoughnessTex),
                new(MaterialKeys.Ambient),
                new(MaterialKeys.AmbientTex),
                new(MaterialKeys.AmbientOcclusionTex),
                new(MaterialKeys.AmbientOcclusionMultiplier),
                new(MaterialKeys.Emission),
                new(MaterialKeys.EmissionTex),
                new(MaterialKeys.Shininess),
                new(MaterialKeys.Reflectivity),
                new(MaterialKeys.ReflectionTex),
                new(MaterialKeys.OpacityTex),
                new(MaterialKeys.Threshold),
                new(MaterialKeys.NormalTex),
                new(MaterialKeys.HeightTex),
                new(MaterialKeys.ParallaxScale),
                new(MaterialKeys.EnableParallaxEdgeClip),
                new(MaterialKeys.EnableParallaxShadow));
        
    public static GLSLProgram Depth { get; } =
        new GLSLProgram { Name = "nagule.depth" }
            .WithShaders(
                new(ShaderType.Vertex, GraphicsHelper.LoadEmbededShader("nagule.common.simple.vert.glsl")),
                new(ShaderType.Fragment, GraphicsHelper.EmptyFragmentShader));

    public ImmutableDictionary<ShaderType, string> Shaders { get; init; }
        = ImmutableDictionary<ShaderType, string>.Empty;
    public ImmutableHashSet<string> Macros { get; init; }
        = ImmutableHashSet<string>.Empty;
    public ImmutableDictionary<string, ShaderParameterType> Parameters { get; init; }
        = ImmutableDictionary<string, ShaderParameterType>.Empty;
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
    public GLSLProgram WithParameter(ShaderParameter parameter)
        => this with { Parameters = Parameters.SetItem(parameter.Name, parameter.Type) };
    public GLSLProgram WithParameters(params ShaderParameter[] parameters)
        => this with { Parameters = Parameters.SetItems(parameters.Select(ShaderParameter.ToPair)) };
    public GLSLProgram WithParameters(IEnumerable<ShaderParameter> parameters)
        => this with { Parameters = Parameters.SetItems(parameters.Select(ShaderParameter.ToPair)) };

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