namespace Nagule.Graphics;

using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Material))]
[NaguleAsset<Material>]
public record MaterialAsset : AssetBase
{
    public static MaterialAsset Default { get; } = new() { Name = "Default" };

    public static MaterialAsset Unlit { get; } = new() {
        Name = "Default",
        LightingMode = LightingMode.Unlit
    };

    public static MaterialAsset White { get; } = new() {
        Name = "White",
        ShaderProgram = GLSLProgramAsset.White
    };

    public static MaterialAsset MinDepth { get; } = new() {
        Name = "MinDepth",
        ShaderProgram =
            new GLSLProgramAsset()
                .WithShaders(
                    new(ShaderType.Vertex, ShaderUtils.LoadEmbedded("min_depth.vert.glsl")),
                    new(ShaderType.Fragment, ShaderUtils.LoadEmbedded("min_depth.frag.glsl")))
    };

    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public LightingMode LightingMode { get; init; } = LightingMode.Full;
    public bool IsTwoSided { get; init; }
    public GLSLProgramAsset ShaderProgram { get; init; } = GLSLProgramAsset.Standard;

    [SiaProperty(Item = "Property")]
    public ImmutableDictionary<string, Dyn> Properties { get; init; } =
        ImmutableDictionary<string, Dyn>.Empty;

    public MaterialAsset WithProperty(string name, Dyn value)
        => this with { Properties = Properties.SetItem(name, value) };
    public MaterialAsset WithProperty(MaterialProperty property)
        => this with { Properties = Properties.SetItem(property.Name, property.Value) };
    public MaterialAsset WithProperties(params MaterialProperty[] properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
    public MaterialAsset WithProperties(IEnumerable<MaterialProperty> properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
}