namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct Texture : IResourceObject<TextureResource>
{
    public TextureResource Resource { get; set; }
}