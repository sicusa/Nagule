namespace Nagule;

using System.Runtime.Serialization;

[DataContract]
public struct ResourceReferencers : IPooledComponent
{
    [DataMember] public readonly HashSet<Guid> Ids = new();

    public ResourceReferencers() {}
}