namespace Nagule;

using System.Collections.Immutable;

public struct Metadata : IPooledComponent
{
    public ImmutableDictionary<string, object> Dictionary =
        ImmutableDictionary<string, object>.Empty;
    
    public Metadata() {}
}