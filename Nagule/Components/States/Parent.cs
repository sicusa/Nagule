namespace Nagule;

using System.Runtime.Serialization;

[DataContract]
public struct Parent : IReactiveComponent
{
    [DataMember] public Guid Id = Guid.Empty;

    public Parent() {}
}