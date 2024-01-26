namespace Nagule;

using Sia;

public abstract record AssetBase : IAssetRecord
{
    [SiaIgnore] public string? Name { get; init; }
    [SiaIgnore] public Guid? Id { get; init; }
}