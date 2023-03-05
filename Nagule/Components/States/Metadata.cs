namespace Nagule;

public struct Metadata : IHashComponent
{
    public readonly Dictionary<string, object> Dictionary = new();
    
    public Metadata() {}
}