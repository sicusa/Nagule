namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sia;

public partial class Tileset2DManager
{
    protected override TextureTarget TextureTarget => TextureTarget.Texture2dArray;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        RegisterCommonListeners(
            (Tileset2D.SetMinFilter cmd) => cmd.Value,
            (Tileset2D.SetMagFilter cmd) => cmd.Value,
            (Tileset2D.SetBorderColor cmd) => cmd.Value,
            (Tileset2D.SetMipmapEnabled cmd) => cmd.Value);
        
        RegisterParameterListener((in Tileset2DState state, in Tileset2D.SetWrapU cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(cmd.Value)));

        RegisterParameterListener((in Tileset2DState state, in Tileset2D.SetWrapV cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(cmd.Value)));

        void Regenerate(in EntityRef entity)
        {
            ref var tex = ref entity.Get<Tileset2D>();
            var usage = tex.Usage;

            var count = tex.Count;
            var tileWidth = tex.TileWidth;
            var tileHeight = tex.TileHeight;
            var image = tex.Image;

            RegenerateTexture(entity, () => {
                LoadImage(usage, tileWidth, tileHeight, count, image);
            });
        }
        
        Listen((in EntityRef e, in Tileset2D.SetUsage cmd) => Regenerate(e));
        Listen((in EntityRef e, in Tileset2D.SetImage cmd) => Regenerate(e));
        Listen((in EntityRef e, in Tileset2D.SetTileWidth cmd) => Regenerate(e));
        Listen((in EntityRef e, in Tileset2D.SetTileHeight cmd) => Regenerate(e));
        Listen((in EntityRef e, in Tileset2D.SetCount cmd) => Regenerate(e));
    }

    protected unsafe override void LoadAsset(EntityRef entity, ref Tileset2D asset, EntityRef stateEntity)
    {
        var usage = asset.Usage;

        var count = asset.Count;
        var tileWidth = asset.TileWidth;
        var tileHeight = asset.TileHeight;
        var image = asset.Image;

        var wrapU = asset.WrapU;
        var wrapV = asset.WrapV;

        var minFilter = asset.MinFilter;
        var magFilter = asset.MagFilter;
        var borderColor = asset.BorderColor;
        var mipmapEnabled = asset.MipmapEnabled;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Tileset2DState>();
            state = new Tileset2DState {
                Handle = new(GL.GenTexture()),
                MipmapEnabled = mipmapEnabled
            };

            GL.BindTexture(TextureTarget, state.Handle.Handle);
            LoadImage(usage, tileWidth, tileHeight, count, image);
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapS, TextureUtils.Cast(wrapU));
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureWrapT, TextureUtils.Cast(wrapV));
            
            SetCommonParameters(minFilter, magFilter, borderColor, mipmapEnabled);
            SetTextureInfo(stateEntity, state);
        });
    }

    private unsafe void LoadImage(
        TextureUsage usage, int tileWidth, int tileHeight, int? optionalCount, RImageBase image)
    {
        var tileXCount = image.Width / tileWidth;
        var tileYCount = image.Height / tileHeight;
        var count = optionalCount ?? tileXCount * tileYCount;

        var pixelFormat = image.PixelFormat;
        var (internalFormat, pixelType) = GLUtils.GetTexPixelInfo(image);
        var glPixelFormat = GLUtils.SetPixelFormat(TextureTarget, pixelFormat, internalFormat, pixelType);

        if (GLUtils.IsSRGBTexture(usage)) {
            internalFormat = GLUtils.ToSRGBColorSpace(internalFormat);
        }

        GL.TexImage3D(TextureTarget, 0, internalFormat, tileWidth, tileHeight, count, 0, glPixelFormat, pixelType, (void*)0);

        int channelByteCount = image.ChannelSize;
        int channelCount = image.PixelFormat switch {
            PixelFormat.Grey => 1,
            PixelFormat.GreyAlpha => 2,
            PixelFormat.RedGreenBlue => 3,
            PixelFormat.RedGreenBlueAlpha => 4,
            _ => throw new NaguleInternalException("Invalid pixel format")
        };
        int pixelByteCount = channelByteCount * channelCount;
        var imageBytes = image.AsByteSpan();

        GL.PixelStorei(PixelStoreParameter.UnpackRowLength, image.Width);
        GL.PixelStorei(PixelStoreParameter.UnpackImageHeight, image.Height);

        for (int y = 0; y < tileYCount; ++y) {
            for (int x = 0; x < tileXCount; ++x) {
                int i = (tileYCount - y - 1) * tileXCount + x;
                if (i >= count) {
                    goto Stop;
                }
                int offset = (y * tileHeight * image.Width + x * tileWidth) * pixelByteCount;
                GL.TexSubImage3D(TextureTarget, 0, 0, 0, i, tileWidth, tileHeight, 1, glPixelFormat, pixelType, imageBytes[offset]);
            }
        }
        
    Stop:
        GL.PixelStorei(PixelStoreParameter.UnpackRowLength, 0);
        GL.PixelStorei(PixelStoreParameter.UnpackImageHeight, 0);
    }
}