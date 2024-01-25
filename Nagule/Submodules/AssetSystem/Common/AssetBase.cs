
namespace Nagule;

public abstract record AssetBase : IAssetRecord
{
    public string? Name { get; init; }
    public Guid? Id { get; init; }
}