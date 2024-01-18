namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public record struct MaterialReferences
{
    public RGLSLProgram ColorProgramAsset;
    public EntityRef ColorProgram;

    public EntityRef DepthProgram;
    public RGLSLProgram DepthProgramAsset;

    public Dictionary<string, EntityRef>? Textures;
}