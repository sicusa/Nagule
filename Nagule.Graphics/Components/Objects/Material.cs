namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct Material : IResourceObject<MaterialResource>
{
    public MaterialResource Resource { get; set; } = MaterialResource.Default;
    public ShaderProgramResource? ShaderProgram = null;

    public Material() {}
}