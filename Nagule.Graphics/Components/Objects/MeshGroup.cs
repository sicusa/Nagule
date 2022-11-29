namespace Nagule.Graphics;

using System.Collections.Immutable;
using System.Runtime.Serialization;

[DataContract]
public struct MeshGroup : IPooledComponent
{
    [DataMember] public ImmutableArray<Guid> MeshIds =
        ImmutableArray<Guid>.Empty;

    public MeshGroup() {}
}