namespace Nagule;

using System.Reflection;

public static class EmbeddedAssets
{
    public static TAsset Load<TAsset>(AssetPath<TAsset> name)
        where TAsset : ILoadableAsset<TAsset>
        => Load(name, Assembly.GetCallingAssembly());

    public static TAsset Load<TAsset, TOptions>(AssetPath<TAsset> name, TOptions options)
        where TAsset : ILoadableAsset<TAsset, TOptions>
        => Load(name, options, Assembly.GetCallingAssembly());
    
    public static TAsset Load<TAsset>(AssetPath<TAsset> name, Assembly assembly)
        where TAsset : ILoadableAsset<TAsset>
        => TAsset.Load(GetStream(name, assembly), name);

    public static TAsset Load<TAsset, TOptions>(AssetPath<TAsset> name, TOptions options, Assembly assembly)
        where TAsset : ILoadableAsset<TAsset, TOptions>
        => TAsset.Load(GetStream(name, assembly), options, name);

    public static TAsset LoadInternal<TAsset>(AssetPath<TAsset> name)
        where TAsset : ILoadableAsset<TAsset>
        => LoadInternal(name, Assembly.GetCallingAssembly());

    public static TAsset LoadInternal<TAsset, TOptions>(AssetPath<TAsset> name, TOptions options)
        where TAsset : ILoadableAsset<TAsset, TOptions>
        => LoadInternal(name, options, Assembly.GetCallingAssembly());
    
    public static TAsset LoadInternal<TAsset>(AssetPath<TAsset> name, Assembly assembly)
        where TAsset : ILoadableAsset<TAsset>
        => TAsset.Load(GetStream(GetInternalName(name, assembly), assembly), name);

    public static TAsset LoadInternal<TAsset, TOptions>(AssetPath<TAsset> name, TOptions options, Assembly assembly)
        where TAsset : ILoadableAsset<TAsset, TOptions>
        => TAsset.Load(GetStream(GetInternalName(name, assembly), assembly), options, name);

    private static string GetInternalName(string name, Assembly assembly)
        => assembly.FullName![0..assembly.FullName!.IndexOf(',')] + ".Embedded." + name;

    private static Stream GetStream(string name, Assembly assembly)
        => assembly.GetManifestResourceStream(name)
            ?? throw new FileNotFoundException("Asset not found: " + name);
}