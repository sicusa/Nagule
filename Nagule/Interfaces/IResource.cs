namespace Nagule;

public interface IResource
{
    Guid? Id { get; init; }
    string Name { get; set; }
}