namespace Nagule;

public struct Metadata : IPooledComponent
{
    public readonly Dictionary<string, object> Dictionary = new();
    
    public Metadata() {}
}