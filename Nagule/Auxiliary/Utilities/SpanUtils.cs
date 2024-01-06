namespace Nagule;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

public static class SpanUtils
{
    public static MemoryOwner<T> Concat<T>(ReadOnlySpan<T> s, List<T> l)
        => Concat(s, l.AsSpan());

    public static MemoryOwner<T> Concat<T>(List<T> l, ReadOnlySpan<T> s)
        => Concat(l.AsSpan(), s);

    public static MemoryOwner<T> Concat<T>(List<T> l1, List<T> l2)
        => Concat<T>(l1.AsSpan(), l2.AsSpan());

    public static MemoryOwner<T> Concat<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
    {
        var memory = MemoryOwner<T>.Allocate(s1.Length + s2.Length);
        var span = memory.Span;
        s1.CopyTo(span);
        s2.CopyTo(span[s1.Length..]);
        return memory;
    }

    public delegate TKey KeySelector<TKey, T>(in T value)
        where TKey : notnull;
    public delegate void MergeHandler<TKey, T>(in TKey Key, in T value)
        where TKey : notnull;

    public static void MergeOrdered<TKey, T>(
        ReadOnlySpan<T> span1, ReadOnlySpan<T> span2,
        KeySelector<TKey, T> keySelector, MergeHandler<TKey, T> mergeHandler)
        where TKey : notnull, IComparable<TKey>
    {
        if (span1.Length == 0) {
            foreach (ref readonly var v in span2) {
                mergeHandler(keySelector(v), v);
            }
            return;
        }

        if (span2.Length == 0) {
            return;
        }

        int count1 = span1.Length;
        int count2 = span2.Length;

        int i1 = 0;
        int i2 = 0;

        ref readonly T v1 = ref span1[0];
        ref readonly T v2 = ref span2[0];

        var v1Key = keySelector(v1);
        var v2Key = keySelector(v2);

        while (true) {
            while (v1Key.CompareTo(v2Key) > 0) {
                mergeHandler(v2Key, v2);
                if (++i2 >= count2) { return; }
                v2 = ref span2[i2];
                v2Key = keySelector(v2);
            }

            while (v1Key.CompareTo(v2Key) < 0) {
                if (++i1 >= count1) { goto EXIT; }
                v1 = ref span1[i1];
                v1Key = keySelector(v1);
            }

            while (v1Key.CompareTo(v2Key) == 0) {
                if (++i1 >= count1) { goto EXIT; }
                if (++i2 >= count2) { return; }
                v1 = ref span1[i1];
                v2 = ref span2[i2];
                v1Key = keySelector(v1);
                v2Key = keySelector(v2);
            }
        }
    
    EXIT:
        while (++i2 < count2) {
            v2 = ref span2[i2];
            v2Key = keySelector(v2);
            mergeHandler(v2Key, v2);
        }
    }
}