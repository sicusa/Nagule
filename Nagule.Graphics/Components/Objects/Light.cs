namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct Light : IResourceObject<LightResourceBase>
{
    public LightResourceBase Resource { get; set; }
}