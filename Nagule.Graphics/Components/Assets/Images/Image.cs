namespace Nagule.Graphics;

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public record RImage<TPixel> : RImageBase
    where TPixel : unmanaged
{
    public ImmutableArray<TPixel> Data { get; init; } = [];

    [SiaIgnore] public override int Length => Data.Length;
    [SiaIgnore] public override Type ChannelType => typeof(TPixel);
    [SiaIgnore] public override int ChannelSize => Marshal.SizeOf<TPixel>();

    public unsafe override ReadOnlySpan<byte> AsByteSpan()
        => Data.Length == 0 ? [] : new(
            Unsafe.AsPointer(ref Unsafe.AsRef(in Data.ItemRef(0))), sizeof(TPixel) * Data.Length);
}

[SiaTemplate(nameof(Image))]
[NaAsset]
public record RImage : RImage<byte>, ILoadableAssetRecord<RImage>
{
    public static RImage Empty { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [],
        Width = 1,
        Height = 1
    };

    public static RImage Hint { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [255, 0, 255],
        Width = 1,
        Height = 1
    };

    public static RImage White { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [255, 255, 255],
        Width = 1,
        Height = 1
    };

    public static RImage Load(byte[] bytes, string name = "")
        => ImageUtils.Load(bytes, name);

    public static RImage Load(Stream stream, string? name = null)
        => ImageUtils.Load(stream, name);
}