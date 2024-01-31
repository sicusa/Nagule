namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public partial class CubemapManager
{
    protected override TextureTarget TextureTarget => TextureTarget.TextureCubeMap;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        RegisterCommonListeners(
            (Cubemap.SetMinFilter cmd) => cmd.Value,
            (Cubemap.SetMagFilter cmd) => cmd.Value,
            (Cubemap.SetBorderColor cmd) => cmd.Value,
            (Cubemap.SetMipmapEnabled cmd) => cmd.Value);

        RegisterParameterListener((ref CubemapState state, in Cubemap.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((ref CubemapState state, in Cubemap.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((ref CubemapState state, in Cubemap.SetWrapW cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapR, TextureUtils.Cast(cmd.Value)));
        
        void Regenerate(in EntityRef entity)
        {
            ref var tex = ref entity.Get<Cubemap>();
            var usage = tex.Usage;
            var images = tex.Images;

            RegenerateTexture(entity, () => {
                foreach (var (target, image) in images) {
                    var textureTarget = TextureUtils.Cast(target);
                    GLUtils.TexImage2D(textureTarget, usage, image);
                }
            });
        }
        
        Listen((in EntityRef e, in Cubemap.SetUsage cmd) => Regenerate(e));
        Listen((in EntityRef e, in Cubemap.SetImages cmd) => Regenerate(e));
    }

    protected override void LoadAsset(EntityRef entity, ref Cubemap asset, EntityRef stateEntity)
    {
        var usage = asset.Usage;
        var images = asset.Images;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;
        var wrapW = asset.WrapW;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<CubemapState>();
            state = new CubemapState {
                Handle = new(GL.GenTexture()),
                MinFilter = minFilter,
                MagFilter = magFilter,
                MipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);

            foreach (var (target, image) in images) {
                var textureTarget = TextureUtils.Cast(target);
                GLUtils.TexImage2D(textureTarget, usage, image);
            }

            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapR, TextureUtils.Cast(wrapW));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }
}