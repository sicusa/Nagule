namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public partial class Texture2DManager
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
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in Texture2DState state, in Texture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));
        
        void Regenerate(in EntityRef entity)
        {
            ref var tex = ref entity.Get<Texture2D>();
            var type = tex.Type;
            var image = tex.Image ?? RImage.Hint;

            RegenerateTexture(entity, () => {
                GLUtils.TexImage2D(type, image);
            });
        }

        Listen((in EntityRef entity, in Texture2D.SetType cmd) => Regenerate(entity));
        Listen((in EntityRef entity, in Texture2D.SetImage cmd) => Regenerate(entity));
    }

    protected override void LoadAsset(EntityRef entity, ref Texture2D asset, EntityRef stateEntity)
    {
        var type = asset.Type;
        var image = asset.Image ?? RImage.Hint;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Texture2DState>();
            state = new Texture2DState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            GLUtils.TexImage2D(type, image);

            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }
}