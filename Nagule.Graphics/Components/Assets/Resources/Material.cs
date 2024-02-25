namespace Nagule.Graphics;

using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Material))]
[NaAsset]
public record RMaterial : AssetBase
{
    public static RMaterial Standard { get; } = new() { Name = "Standard" };

    public static RMaterial Unlit { get; } = new() {
        Name = "default",
        LightingMode = LightingMode.Unlit
    };

    public static RMaterial White { get; } = new() {
        Name = "white",
        ShaderProgram = RGLSLProgram.White
    };

    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public LightingMode LightingMode { get; init; } = LightingMode.Full;

    public bool IsTwoSided { get; init; } = false;
    public bool IsShadowCaster { get; init; } = true;
    public bool IsShadowReceiver { get; init; } = true;

    public RGLSLProgram ShaderProgram { get; init; } = RGLSLProgram.Standard;

    [Sia(Item = "Property")]
    public ImmutableDictionary<string, Dyn> Properties { get; init; } =
        ImmutableDictionary<string, Dyn>.Empty;

    public RMaterial WithProperty(string name, Dyn value)
        => this with { Properties = Properties.SetItem(name, value) };
    public RMaterial WithProperty(MaterialProperty property)
        => this with { Properties = Properties.SetItem(property.Name, property.Value) };
    public RMaterial WithProperties(params MaterialProperty[] properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
    public RMaterial WithProperties(IEnumerable<MaterialProperty> properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
}