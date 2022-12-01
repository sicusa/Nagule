namespace Nagule;

using System.Runtime.Serialization;

[DataContract]
public struct TransformRef : IReactiveComponent
{
    [DataMember] public Guid Id = Guid.Empty;

    public TransformRef() {}
}