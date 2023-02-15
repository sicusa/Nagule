namespace Nagule.Graphics;

using System.Collections.Immutable;

public record Material : ResourceBase
{
    public static Material Default { get; } = new();

    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public LightingMode LightingMode { get; init; } = LightingMode.Full;
    public bool IsTwoSided { get; init; }
    public GLSLProgram? ShaderProgram { get; init; }

    public ImmutableDictionary<string, Dyn> Properties { get; init; } =
        ImmutableDictionary<string, Dyn>.Empty;

    public Material WithProperty(string name, Dyn value)
        => this with { Properties = Properties.SetItem(name, value) };
    public Material WithProperty(MaterialProperty property)
        => this with { Properties = Properties.SetItem(property.Name, property.Value) };
    public Material WithProperties(params MaterialProperty[] properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
    public Material WithProperties(IEnumerable<MaterialProperty> properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
}