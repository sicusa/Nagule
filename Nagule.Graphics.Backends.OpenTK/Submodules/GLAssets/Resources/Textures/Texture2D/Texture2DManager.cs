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
            (Texture2D.SetIsMipmapEnabled cmd) => cmd.Value);

        RegisterParameterListener((ref Texture2DState state, in Texture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((ref Texture2DState state, in Texture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));
        
        void Regenerate(in EntityRef entity)
        {
            var stateEntity = entity.GetStateEntity();

            ref var tex = ref entity.Get<Texture2D>();
            var usage = tex.Usage;
            var image = tex.Image ?? RImage.Hint;

            RegenerateTexture(entity, () => {
                ref var state = ref stateEntity.Get<Texture2DState>();
                state.Width = image.Width;
                state.Height = image.Height;
                GLUtils.TexImage2D(usage, image);
            });
        }

        Listen((in EntityRef entity, in Texture2D.SetUsage cmd) => Regenerate(entity));
        Listen((in EntityRef entity, in Texture2D.SetImage cmd) => Regenerate(entity));
    }

    public override void LoadAsset(in EntityRef entity, ref Texture2D asset, EntityRef stateEntity)
    {
        var usage = asset.Usage;
        var image = asset.Image;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.IsMipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Texture2DState>();
            state = new Texture2DState {
                Width = image.Width,
                Height = image.Height,
                Handle = new(GL.GenTexture()),
                MinFilter = minFilter,
                MagFilter = magFilter,
                IsMipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            GLUtils.TexImage2D(usage, image);

            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }
}