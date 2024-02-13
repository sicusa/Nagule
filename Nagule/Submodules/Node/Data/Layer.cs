namespace Nagule;

using System.Numerics;
using System.Runtime.CompilerServices;

public interface ILayer;

public readonly record struct LayerMask(int Value)
    : IBitwiseOperators<LayerMask, LayerMask, LayerMask>
{
    public static readonly LayerMask Empty = new(0);
    public static readonly LayerMask All = new(int.MinValue);

    public static LayerMask operator ~(LayerMask value) => new(-value.Value);
    public static LayerMask operator &(LayerMask left, LayerMask right)
        => new(left.Value & right.Value);
    public static LayerMask operator |(LayerMask left, LayerMask right)
        => new(left.Value | right.Value);
    public static LayerMask operator ^(LayerMask left, LayerMask right)
        => new(left.Value ^ right.Value);

    public static implicit operator int(LayerMask mask) => mask.Value;
}

public static class Layers
{
    public static ReadOnlySpan<Type?> Types => _masks.AsSpan();
    internal static readonly Type?[] _masks = new Type?[32];
}

internal static class LayerIndexerShared
{
    private static int _index = -1;
    public static int GetNext() => Interlocked.Increment(ref _index);
}

public static class LayerIndexer<T>
    where T : ILayer
{
    public static int Index { get; } = LayerIndexerShared.GetNext();
    public static LayerMask Mask { get; } = new(1 << Index);

    static LayerIndexer()
    {
        if (Index >= 32) {
            throw new InvalidDataException("Layer count exceeds 32");
        }
        Layers._masks[Index] = typeof(T);
    }
}

public readonly record struct Layer
{
    public class DefaultLayer : ILayer;
    public static readonly Layer Default = From<DefaultLayer>();

    public Type Type {
        get {
            var types = Layers.Types;
            if (Index < 0 || Index >= types.Length) {
                throw new InvalidDataException("Invalid layer");
            }
            return types[Index]
                ?? throw new InvalidDataException("Empty layer");
        }
    }

    public LayerMask Mask => new(1 << Index);

    public int Index { get; }

    internal Layer(int index)
    {
        Index = index;
    }

    public static Layer From<TLayer>()
        where TLayer : ILayer
        => new(LayerIndexer<TLayer>.Index);

    public static implicit operator LayerMask(Layer layer) => layer.Mask;
}