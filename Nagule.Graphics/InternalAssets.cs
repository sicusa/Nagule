namespace Nagule.Graphics;

using System.Text;
using System.Reflection;

public static class InternalAssets
{
    private static Dictionary<Type, Func<Stream, string, object>> s_resourceLoaders = new() {
        [typeof(ImageResource)] = ImageHelper.Load,
        [typeof(ModelResource)] = ModelHelper.Load,
        [typeof(TextResource)] = (stream, hint) => {
            var reader = new StreamReader(stream, Encoding.UTF8);
            return new TextResource {
                Content = reader.ReadToEnd()
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
        return (TResource)loader(stream, name.Substring(name.LastIndexOf('.')));
    }

    public static string LoadText(string name)
        => Load<TextResource>(name, Assembly.GetCallingAssembly()).Content;

    private static Stream? LoadRaw(string name, Assembly assembly)
        => assembly.GetManifestResourceStream(name);
}