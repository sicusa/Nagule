namespace Nagule.Graphics;

using System.Text;
using System.Reflection;
using System.Collections.Immutable;

public static class EmbeddedAssets
{
    private static readonly Dictionary<Type, Func<Stream, string, object>> s_assetLoaders = new() {
        [typeof(ImageAsset)] = ImageUtils.Load,
        [typeof(ImageAsset<byte>)] = ImageUtils.Load,
        [typeof(ImageAsset<float>)] = ImageUtils.LoadHDR,
        [typeof(HDRImageAsset)] = ImageUtils.LoadHDR,
        [typeof(Model3DAsset)] = ModelUtils.Load,
        [typeof(TextAsset)] = (stream, hint) => {
            var reader = new StreamReader(stream, Encoding.UTF8);
            return new TextAsset {
                Content = reader.ReadToEnd()
            };
        },
        [typeof(Font)] = (stream, hint) => {
            var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            return new Font {
                Bytes = ImmutableArray.Create(memStream.ToArray())
            };
        }
    };

    public static TAsset Load<TAsset>(string name)
        => Load<TAsset>(name, Assembly.GetCallingAssembly());

    public static TAsset Load<TAsset>(string name, Assembly assembly)
    {
        if (!s_assetLoaders.TryGetValue(typeof(TAsset), out var loader)) {
            throw new NotSupportedException("Asset type not supported: " + typeof(TAsset));
        }
        var stream = LoadRaw(name, assembly)
            ?? throw new FileNotFoundException("Asset not found: " + name);
        return (TAsset)loader(stream, name);
    }

    public static string LoadText(string name)
        => LoadText(name, Assembly.GetCallingAssembly());

    public static string LoadText(string name, Assembly assembly)
        => Load<TextAsset>(name, assembly).Content;

    private static Stream? LoadRaw(string name, Assembly assembly)
        => assembly.GetManifestResourceStream(name);
}