namespace Nagule;

using System.Runtime.Serialization;

[DataContract]
public struct Parent : IReactiveComponent
{
    [DataMember] public uint Id = 0;

    public Parent() {}
}