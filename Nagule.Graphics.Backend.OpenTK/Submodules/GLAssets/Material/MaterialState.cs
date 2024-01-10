namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public record struct MaterialState : IAssetState
{
    public readonly bool Loaded => UniformBufferHandle != BufferHandle.Zero;

    public BufferHandle UniformBufferHandle;
    public IntPtr Pointer;

    public EntityRef ColorProgramState;
    public EntityRef DepthProgramState;

    public Dictionary<string, EntityRef>? TextureStates;

    public RenderMode RenderMode;
    public LightingMode LightingMode;
    public bool IsTwoSided;

    public readonly uint EnableTextures(
        in GLSLProgramState programState, uint startIndex)
    {
        var textureLocations = programState.TextureLocations;
        if (TextureStates == null || textureLocations == null) {
            return startIndex;
        }

        foreach (var (name, texState) in TextureStates) {
            if (!textureLocations.TryGetValue(name, out var location)
                    || !texState.Valid) {
                continue;
            }

            ref var texHandle = ref texState.Get<TextureHandle>();
            if (Unsafe.IsNullRef(ref texHandle)) {
                GL.Uniform1i(location, 0);
                continue;
            }

            GL.ActiveTexture(TextureUnit.Texture0 + startIndex);
            GL.BindTexture(TextureTarget.Texture2d, texHandle.Handle);
            GL.Uniform1i(location, (int)startIndex);
            
            ++startIndex;
        }

        return startIndex;
    }
}