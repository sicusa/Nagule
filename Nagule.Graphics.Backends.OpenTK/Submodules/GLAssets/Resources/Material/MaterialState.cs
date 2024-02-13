namespace Nagule.Graphics.Backends.OpenTK;

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
    public bool IsShadowCaster;
    public bool IsShadowReceiver;

    public readonly void Bind(in GLSLProgramState programState)
    {
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, UniformBufferHandle.Handle);
        GL.UseProgram(programState.Handle.Handle);
    }

    public readonly uint ActivateTextures(
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

            ref var info = ref texState.Get<TextureInfo>();
            GL.ActiveTexture(TextureUnit.Texture0 + startIndex);
            GL.BindTexture(info.Target, info.Handle.Handle);
            GL.Uniform1i(location, (int)startIndex);
            
            ++startIndex;
        }

        return startIndex;
    }
}