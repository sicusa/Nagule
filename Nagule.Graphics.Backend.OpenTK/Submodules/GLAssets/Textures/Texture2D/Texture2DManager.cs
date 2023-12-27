namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Texture2DManager : TextureManagerBase<Texture2D, Texture2DAsset, Texture2DState>
{
    protected override TextureTarget TextureTarget => TextureTarget.Texture2d;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        RegisterCommonListeners(
            (Texture2D.SetMinFilter cmd) => cmd.Value,
            (Texture2D.SetMagFilter cmd) => cmd.Value,
            (Texture2D.SetBorderColor cmd) => cmd.Value,
            (Texture2D.SetMipmapEnabled cmd) => cmd.Value);

        RegisterParameterListener((in Texture2DState state, in Texture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in Texture2DState state, in Texture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));
    }

    protected override void LoadAsset(EntityRef entity, ref Texture2D asset, EntityRef stateEntity)
    {
        var type = asset.Type;
        var image = asset.Image ?? ImageAsset.Hint;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Texture2DState>();
            state = new Texture2DState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget.Texture2d, state.Handle.Handle);
            GLUtils.TexImage2D(type, image);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            stateEntity.Get<TextureHandle>() = state.Handle;
            return true;
        });
    }
}