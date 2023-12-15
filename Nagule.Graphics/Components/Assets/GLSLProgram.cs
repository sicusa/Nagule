namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(GLSLProgram), Immutable = true)]
[NaguleAsset<GLSLProgram>]
public record GLSLProgramAsset : AssetBase
{
    public static GLSLProgramAsset Standard { get; } =
        new GLSLProgramAsset() { Name = "Standard" }
            .WithShaders(
                new(ShaderType.Vertex, ShaderUtils.LoadEmbedded("blinn_phong.vert.glsl")),
                new(ShaderType.Fragment, ShaderUtils.LoadEmbedded("blinn_phong.frag.glsl")))
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

    public static GLSLProgramAsset White { get; } =
        new GLSLProgramAsset()
            .WithShaders(
                new(ShaderType.Vertex, ShaderUtils.LoadEmbedded("nagule.common.simple.vert.glsl")),
                new(ShaderType.Fragment, ShaderUtils.LoadEmbedded("nagule.common.white.frag.glsl")));
        
    public static GLSLProgramAsset Depth { get; } =
        new GLSLProgramAsset()
            .WithShaders(
                new(ShaderType.Vertex, ShaderUtils.LoadEmbedded("nagule.common.simple.vert.glsl")),
                new(ShaderType.Fragment, ShaderUtils.EmptyFragmentShader));

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

    public GLSLProgramAsset WithShader(ShaderType shaderType, string source)
        => this with { Shaders = Shaders.SetItem(shaderType, source) };
    public GLSLProgramAsset WithShaders(params KeyValuePair<ShaderType, string>[] shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };
    public GLSLProgramAsset WithShaders(IEnumerable<KeyValuePair<ShaderType, string>> shaders)
        => this with { Shaders = Shaders.SetItems(shaders) };

    public GLSLProgramAsset WithMacro(string macro)
        => this with { Macros = Macros.Add(macro) };
    public GLSLProgramAsset WithMacros(params string[] macros)
        => this with { Macros = Macros.Union(macros) };
    public GLSLProgramAsset WithMacros(IEnumerable<string> macros)
        => this with { Macros = Macros.Union(macros) };

    public GLSLProgramAsset WithParameter(string name, ShaderParameterType parameterType)
        => this with { Parameters = Parameters.SetItem(name, parameterType) };
    public GLSLProgramAsset WithParameter(ShaderParameter parameter)
        => this with { Parameters = Parameters.SetItem(parameter.Name, parameter.Type) };
    public GLSLProgramAsset WithParameters(params ShaderParameter[] parameters)
        => this with { Parameters = Parameters.SetItems(parameters.Select(ShaderParameter.ToPair)) };
    public GLSLProgramAsset WithParameters(IEnumerable<ShaderParameter> parameters)
        => this with { Parameters = Parameters.SetItems(parameters.Select(ShaderParameter.ToPair)) };

    public GLSLProgramAsset WithFeedback(string feedback)
        => this with { Feedbacks = Feedbacks.Add(feedback) };
    public GLSLProgramAsset WithFeedbacks(params string[] feedbacks)
        => this with { Feedbacks = Feedbacks.Union(feedbacks) };
    public GLSLProgramAsset WithFeedbacks(IEnumerable<string> feedbacks)
        => this with { Feedbacks = Feedbacks.Union(feedbacks) };

    public GLSLProgramAsset WithSubroutine(ShaderType shaderType, ImmutableArray<string> names)
        => this with { Subroutines = Subroutines.SetItem(shaderType, names) };
    public GLSLProgramAsset WithSubroutines(params KeyValuePair<ShaderType, ImmutableArray<string>>[] subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
    public GLSLProgramAsset WithSubroutines(IEnumerable<KeyValuePair<ShaderType, ImmutableArray<string>>> subroutines)
        => this with { Subroutines = Subroutines.SetItems(subroutines) };
}