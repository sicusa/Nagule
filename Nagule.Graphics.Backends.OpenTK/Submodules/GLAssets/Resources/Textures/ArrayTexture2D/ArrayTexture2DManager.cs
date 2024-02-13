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
            (ArrayTexture2D.SetIsMipmapEnabled cmd) => cmd.Value);
        
        RegisterParameterListener((ref ArrayTexture2DState state, in ArrayTexture2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((ref ArrayTexture2DState state, in ArrayTexture2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        void Regenerate(in EntityRef entity)
        {
            var name = entity.GetDisplayName();
            var stateEntity = entity.GetStateEntity();

            ref var tex = ref entity.Get<ArrayTexture2D>();
            var usage = tex.Usage;
            var capacity = tex.Capacity;
            var images = tex.Images;

            RegenerateTexture(entity, () => {
                ref var state = ref stateEntity.Get<ArrayTexture2DState>();
                LoadImages(ref state, name, usage, capacity, images);
            });
        }
        
        Listen((in EntityRef e, in ArrayTexture2D.SetUsage cmd) => Regenerate(e));
        Listen((in EntityRef e, in ArrayTexture2D.SetImages cmd) => Regenerate(e));
        Listen((in EntityRef e, in ArrayTexture2D.SetCapacity cmd) => Regenerate(e));
    }

    public unsafe override void LoadAsset(in EntityRef entity, ref ArrayTexture2D asset, EntityRef stateEntity)
    {
        var name = entity.GetDisplayName();

        var usage = asset.Usage;
        var images = asset.Images;
        var capacity = asset.Capacity;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.IsMipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<ArrayTexture2DState>();
            state = new ArrayTexture2DState {
                Handle = new(GL.GenTexture()),
                MinFilter = minFilter,
                MagFilter = magFilter,
                IsMipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            LoadImages(ref state, name, usage, capacity, images);
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }

    private unsafe void LoadImages(
        ref ArrayTexture2DState state, string? name, TextureUsage usage,
        int? optionalCapacity, ImmutableList<RImageBase> images)
    {
        var imageCount = images.Count;
        var capacity = optionalCapacity ?? imageCount;

        if (capacity == 0) {
            return;
        }

        var count = Math.Min(imageCount, capacity);
        var firstImage = imageCount == 0 ? RImage.Hint : images[0];
        var width = firstImage.Width;
        var height = firstImage.Height;

        state.Width = width;
        state.Height = height;

        var pixelFormat = firstImage.PixelFormat;
        var (internalFormat, pixelType) = GLUtils.GetTexPixelInfo(firstImage);
        var glPixelFormat = GLUtils.SetPixelFormat(TextureTarget, pixelFormat, internalFormat, pixelType);

        if (GLUtils.IsSRGBTexture(usage)) {
            internalFormat = GLUtils.ToSRGBColorSpace(internalFormat);
        }

        GL.TexImage3D(TextureTarget, 0, internalFormat, width, height, capacity, 0, glPixelFormat, pixelType, (void*)0);

        for (int i = 0; i < count; ++i) {
            var image = images[i];
            if (image.Width != width || image.Height != height) {
                Logger.LogWarning(
                    "[{Name}] Failed to load {Index}th image: images in array texture must have the same width and height.",
                    name ?? "no name", i);
            }
            if (image.PixelFormat != pixelFormat) {
                Logger.LogWarning(
                    "[{Name}] Failed to load {Index}th image: images in array texture must have the same pixel format.",
                    name ?? "no name", i);
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