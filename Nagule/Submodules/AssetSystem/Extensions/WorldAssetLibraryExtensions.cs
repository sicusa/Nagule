namespace Nagule;

using Sia;

public static class WorldAssetLibraryExtensions
{
    public static EntityRef GetAssetEntity(this World world, IAsset record)
        => world.GetAddon<AssetLibrary>()[record];

    public static bool TryGetAssetEntity(this World world, IAsset record, out EntityRef entity)
        => world.GetAddon<AssetLibrary>().TryGet(record, out entity);
}