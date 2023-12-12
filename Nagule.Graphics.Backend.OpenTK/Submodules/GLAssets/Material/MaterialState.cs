namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Sia;

public record struct MaterialState
{
    public GLSLProgramAsset ColorProgramAsset;
    public EntityRef ColorProgram;

    public GLSLProgramAsset DepthProgramAsset;
    public EntityRef DepthProgram;

    public ImmutableDictionary<string, EntityRef> Textures;

    public BufferHandle UniformBufferHandle;
    public IntPtr Pointer;

    public RenderMode RenderMode;
    public LightingMode LightingMode;
    public bool IsTwoSided;

    public readonly int EnableTextures(
        TextureLibrary textureLibrary, in GLSLProgramState programState, int startIndex)
    {
        var textureLocations = programState.TextureLocations;
        if (Textures == null || textureLocations == null) {
            return startIndex;
        }

        foreach (var (name, texEntity) in Textures) {
            if (!textureLocations.TryGetValue(name, out var location)) {
                continue;
            }

            ref var texHandle = ref textureLibrary.Handles.GetOrNullRef(texEntity);
            if (Unsafe.IsNullRef(ref texHandle)) {
                GL.Uniform1i(location, 0);
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + (uint)startIndex);
            GL.BindTexture(TextureTarget.Texture2d, texHandle.Handle);
            GL.Uniform1i(location, startIndex);
            
            ++startIndex;
        }

        return startIndex;
    }
}