namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
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

        void Regenerate(in EntityRef entity)
        {
            ref var tex = ref entity.Get<ArrayTexture2D>();

            var name = tex.Name;
            var type = tex.Type;
            var capacity = tex.Capacity;
            var images = tex.Images;

            RegenerateTexture(entity, () => {
                LoadImages(name, type, capacity, images);
            });
        }
        
        Listen((in EntityRef e, in ArrayTexture2D.SetType cmd) => Regenerate(e));
        Listen((in EntityRef e, in ArrayTexture2D.SetImages cmd) => Regenerate(e));
    }

    protected unsafe override void LoadAsset(EntityRef entity, ref ArrayTexture2D asset, EntityRef stateEntity)
    {
        var name = asset.Name;
        var type = asset.Type;
        var images = asset.Images;
        var capacity = asset.Capacity;

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

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            LoadImages(name, type, capacity, images);
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }

    private unsafe void LoadImages(
        string? name, TextureType type, int? optionalCapacity, ImmutableList<RImageBase> images)
    {
        var imageCount = images.Count;
        var capacity = optionalCapacity ?? imageCount;

        if (capacity == 0 || imageCount == 0) {
            return;
        }

        var count = Math.Min(imageCount, capacity);
        var firstImage = images[0];
        var width = firstImage.Width;
        var height = firstImage.Height;

        var pixelFormat = firstImage.PixelFormat;
        var (internalFormat, pixelType) = GLUtils.GetTexPixelInfo(firstImage);
        var glPixelFormat = GLUtils.SetPixelFormat(TextureTarget, pixelFormat, internalFormat, pixelType);

        if (GLUtils.IsSRGBTexture(type)) {
            internalFormat = GLUtils.ToSRGBColorSpace(internalFormat);
        }

        GL.TexImage3D(TextureTarget, 0, internalFormat, width, height, capacity, 0, glPixelFormat, pixelType, (void*)0);

        for (int i = 1; i < count; ++i) {
            var image = images[i];
            if (image.Width != width || image.Height != height) {
                Logger.LogWarning(
                    "Failed to load {Index}th image for '{Name}': images in array texture must have the same width and height.",
                    i, name ?? "no name");
            }
            if (image.PixelFormat != pixelFormat) {
                Logger.LogWarning(
                    "Failed to load {Index}th image for '{Name}': images in array texture must have the same pixel format.",
                    i, name ?? "no name");
            }
            if (image.Length == 0) {
                GL.TexSubImage3D(TextureTarget, 0, 0, 0, i,
                    image.Width, image.Height, 1, glPixelFormat, pixelType, (void*)0);
            }
            else {
                GL.TexSubImage3D(TextureTarget, 0, 0, 0, i,
                    image.Width, image.Height, 1, glPixelFormat, pixelType, image.AsByteSpan());
            }
        }
    }
}