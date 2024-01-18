namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Immutable;
using Sia;

public partial class ArrayTexture2DManager
{
    protected override TextureTarget TextureTarget => TextureTarget.Texture2dArray;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        RegisterCommonListeners(
            (ArrayTexture2D.SetMinFilter cmd) => cmd.Value,
            (ArrayTexture2D.SetMagFilter cmd) => cmd.Value,
            (ArrayTexture2D.SetBorderColor cmd) => cmd.Value,
            (ArrayTexture2D.SetMipmapEnabled cmd) => cmd.Value);
        
        RegisterParameterListener((in ArrayTexture2DState state, in ArrayTexture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in ArrayTexture2DState state, in ArrayTexture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        void Regenerate(in EntityRef entity, ImmutableList<RImageBase> prevImages)
        {
            ref var tex = ref entity.Get<ArrayTexture2D>();
            var type = tex.Type;
            var images = tex.Images;

            RegenerateTexture(entity, () => {
                int index = 0;
                foreach (var image in images) {
                    GLUtils.TexSubImage3D(TextureTarget, type, index, image);
                    index++;
                }
                for (; index < prevImages.Count; ++index) {
                    var image = prevImages[index];
                    GL.InvalidateTexSubImage((int)TextureTarget, 0, 0, 0, index, image.Width, image.Height, 1);
                }
            });
        }
        
        Listen((in EntityRef e, ref ArrayTexture2D snapshot, in ArrayTexture2D.SetType cmd) => Regenerate(e, snapshot.Images));
        Listen((in EntityRef e, ref ArrayTexture2D snapshot, in ArrayTexture2D.SetImages cmd) => Regenerate(e, snapshot.Images));
    }

    protected override void LoadAsset(EntityRef entity, ref ArrayTexture2D asset, EntityRef stateEntity)
    {
        var type = asset.Type;
        var images = asset.Images;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<ArrayTexture2DState>();
            state = new ArrayTexture2DState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled
            };

            if (images.Count != 0) {
                var firstImage = images[0];
                var width = firstImage.Width;
                var height = firstImage.Height;

                GL.BindTexture(TextureTarget, state.Handle.Handle);
                //GL.TexImage3D(TextureTarget.Texture2dArray, MipLevelCount, InternalFormat, Width, Height, Capacity, 0, PixelFormat, PixelType, (void*)0);

                int index = 0;
                foreach (var image in images) {
                    //GLUtils.TexSubImage3D(TextureTarget, index, type, image);
                    index++;
                }
            }

            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }
}