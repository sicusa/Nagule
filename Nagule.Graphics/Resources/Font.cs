namespace Nagule.Graphics;

using System.Collections.Immutable;

public record Font : ResourceBase
{
    public static readonly Font None = new();

    public ImmutableArray<byte> Bytes { get; init; } = ImmutableArray<byte>.Empty;
}