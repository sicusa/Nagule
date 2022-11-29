namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct ShaderProgram : IResourceObject<ShaderProgramResource>
{
    public ShaderProgramResource Resource { get; set; }
}