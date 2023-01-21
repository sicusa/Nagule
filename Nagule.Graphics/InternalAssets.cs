namespace Nagule.Graphics;

using System.Text;
using System.Reflection;
using System.Collections.Immutable;

public static class InternalAssets
{
    private static Dictionary<Type, Func<Stream, string, object>> s_resourceLoaders = new() {
        [typeof(Image)] = ImageLoader.Load,
        [typeof(Image<byte>)] = ImageLoader.Load,
        [typeof(Image<float>)] = ImageLoader.LoadFloat,
        [typeof(Model)] = ModelLoader.Load,
        [typeof(Text)] = (stream, hint) => {
            var reader = new StreamReader(stream, Encoding.UTF8);
            return new Text {
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

    public static TResource Load<TResource>(string name)
        => Load<TResource>(name, Assembly.GetCallingAssembly());

    private static TResource Load<TResource>(string name, Assembly assembly)
    {
        if (!s_resourceLoaders.TryGetValue(typeof(TResource), out var loader)) {
            throw new NotSupportedException("Resource type not supported: " + typeof(TResource));
        }
        var stream = LoadRaw(name, assembly)
            ?? throw new FileNotFoundException("Resource not found: " + name);
        return (TResource)loader(stream, name);
    }

    public static string LoadText(string name)
        => Load<Text>(name, Assembly.GetCallingAssembly()).Content;

    private static Stream? LoadRaw(string name, Assembly assembly)
        => assembly.GetManifestResourceStream(name);
}