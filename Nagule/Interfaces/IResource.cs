namespace Nagule;

public interface IResource
{
    uint? Id { get; init; }
    string Name { get; set; }
}