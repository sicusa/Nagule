namespace Nagule.Graphics;

using System.Text;
using System.Reflection;
using System.Collections.Immutable;

public static class EmbeddedAssets
{
    private static readonly Dictionary<Type, Func<Stream, string, object>> s_assetLoaders = new() {
        [typeof(RImage)] = ImageUtils.Load,
        [typeof(RImage<byte>)] = ImageUtils.Load,
        [typeof(RImage<float>)] = ImageUtils.LoadHDR,
        [typeof(RHDRImage)] = ImageUtils.LoadHDR,
        [typeof(RModel3D)] = ModelUtils.Load,
        [typeof(RText)] = (stream, hint) => {
            var reader = new StreamReader(stream, Encoding.UTF8);
            return new RText {
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

    public static TAsset LoadInternal<TAsset>(string name)
        => LoadInternal<TAsset>(name, Assembly.GetCallingAssembly());

    public static TAsset LoadInternal<TAsset>(string name, Assembly assembly)
    {
        if (!s_assetLoaders.TryGetValue(typeof(TAsset), out var loader)) {
            throw new NotSupportedException("Asset type not supported: " + typeof(TAsset));
        }
        var fullName = assembly.FullName![0..assembly.FullName!.IndexOf(',')] + ".Embedded." + name;
        var stream = LoadRaw(fullName, assembly)
            ?? throw new FileNotFoundException("Asset not found: " + fullName);
        return (TAsset)loader(stream, name);
    }

    public static string LoadInternalText(string name)
        => LoadInternalText(name, Assembly.GetCallingAssembly());

    public static string LoadInternalText(string name, Assembly assembly)
        => LoadInternal<RText>(name, assembly).Content;

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
        => Load<RText>(name, assembly).Content;

    private static Stream? LoadRaw(string name, Assembly assembly)
        => assembly.GetManifestResourceStream(name);
}