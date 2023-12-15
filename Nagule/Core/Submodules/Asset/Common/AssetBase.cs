
namespace Nagule;

public abstract record AssetBase : IAsset
{
    public string? Name { get; init; }
    public Guid? Id { get; init; }
}