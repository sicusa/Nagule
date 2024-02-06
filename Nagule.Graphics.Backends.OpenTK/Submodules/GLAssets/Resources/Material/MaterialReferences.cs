namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public record struct MaterialReferences
{
    public RGLSLProgram ColorProgramAsset;
    public EntityRef ColorProgram;

    public RGLSLProgram? DepthProgramAsset;
    public EntityRef? DepthProgram;

    public Dictionary<string, EntityRef>? Textures;
}