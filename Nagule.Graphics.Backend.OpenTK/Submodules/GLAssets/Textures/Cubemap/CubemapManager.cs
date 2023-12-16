namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class CubemapManager : TextureManagerBase<Cubemap, CubemapAsset, CubemapState>
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

        RegisterParameterListener((in CubemapState state, in Cubemap.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in CubemapState state, in Cubemap.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in CubemapState state, in Cubemap.SetWrapW cmd) =>
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, TextureUtils.Cast(cmd.Value)));
    }

    protected override void LoadAsset(EntityRef entity, ref Cubemap asset)
    {
        var type = asset.Type;
        var images = asset.Images;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;
        var wrapW = asset.WrapW;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFrame.Enqueue(entity, () => {
            var state = new CubemapState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget.TextureCubeMap, state.Handle.Handle);

            foreach (var (target, image) in images) {
                var textureTarget = TextureUtils.Cast(target);
                GLUtils.TexImage2D(textureTarget, type, image);
            }

            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, TextureUtils.Cast(wrapW));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            RenderStates.Add(entity, state);
            Handles.Add(entity, state.Handle);
            return true;
        });
    }
}