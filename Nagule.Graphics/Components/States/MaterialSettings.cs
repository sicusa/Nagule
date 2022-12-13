namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct MaterialSettings : IReactiveComponent
{
    [DataMember]
    public readonly Dictionary<string, object> Parameters = new();

    public MaterialSettings() {}
}