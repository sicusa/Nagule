namespace Nagule;

using System.Collections.Immutable;
using System.Runtime.Serialization;

[DataContract]
public struct ResourceReferencers : IPooledComponent
{
    [DataMember] public ImmutableHashSet<Guid> Ids = ImmutableHashSet<Guid>.Empty;

    public ResourceReferencers() {}
}