namespace Nagule.Graphics;

using System.Collections.Immutable;

public struct MaterialProps : IHashComponent
{
    public ReactiveObject<RenderMode> RenderMode { get; } = new();
    public ReactiveObject<LightingMode> LightingMode { get; } = new();
    public ReactiveObject<bool> IsTwoSided { get; } = new();
    public ReactiveObject<GLSLProgram> ShaderProgram { get; } = new();
    public ReactiveDictionary<string, Dyn> Properties { get; } = new();

    public MaterialProps() {}

    public void Set(Material resource)
    {
        RenderMode.Value = resource.RenderMode;
        LightingMode.Value = resource.LightingMode;
        IsTwoSided.Value = resource.IsTwoSided;
        ShaderProgram.Value = resource.ShaderProgram;

        Properties.Clear();
        foreach (var (k, v) in resource.Properties) {
            Properties[k] = v;
        }
    }
}

public record Material : ResourceBase<MaterialProps>
{
    public static Material Default { get; } = new();

    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public LightingMode LightingMode { get; init; } = LightingMode.Full;
    public bool IsTwoSided { get; init; }
    public GLSLProgram ShaderProgram { get; init; } = GLSLProgram.Standard;

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