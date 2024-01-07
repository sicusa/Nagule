namespace Nagule.Graphics;

using System.Text;
using Sia;

public record struct TextLoadOptions(Encoding Encoding)
{
    public static readonly TextLoadOptions Default = new(Encoding.UTF8);
}

[SiaTemplate(nameof(Text))]
[NaAsset]
public record RText : AssetBase, ILoadableAsset<RText, TextLoadOptions>
{
    public string Content { get; init; } = "";

    public static RText Load(Stream stream, string? name = null)
        => Load(stream, TextLoadOptions.Default, name);

    public static RText Load(Stream stream, TextLoadOptions options, string? name = null)
        => new() {
            Name = name,
            Content = new StreamReader(stream, options.Encoding).ReadToEnd()
        };
    
    public static implicit operator string(RText text) => text.Content;
}

public partial record struct Text : IAsset;