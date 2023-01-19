namespace Nagule;

using System.Runtime.InteropServices;

public static class OrderedListHelper
{
    public delegate int FastComparison<T>(in T a, in T b);

    public static void Union<T>(List<T> list1, List<T> list2, FastComparison<T> comparison)
        => Union(list1, CollectionsMarshal.AsSpan(list2), comparison);

    public static void Union<T>(List<T> list, ReadOnlySpan<T> span, FastComparison<T> comparison)
    {
        if (list.Count == 0) {
            foreach (ref readonly var v in span) {
                list.Add(v);
            }
            return;
        }
        if (span.Length == 0) {
            return;
        }

        int count1 = list.Count;
        int count2 = span.Length;

        int i1 = 0;
        int i2 = 0;

        var v1 = list[0];
        ref readonly T v2 = ref span[0];

        while (true) {
            while (comparison(in v1, in v2) > 0) {
                list.Add(v2);
                if (++i2 >= count2) { return; }
                v2 = ref span[i2];
            }

            while (comparison(in v1, in v2) < 0) {
                if (++i1 >= count1) { goto EXIT; }
                v1 = list[i1];
            }

            while (comparison(in v1, in v2) == 0) {
                if (++i1 >= count1) { goto EXIT; }
                if (++i2 >= count2) { return; }
                v1 = list[i1];
                v2 = ref span[i2];
            }
        }
    
    EXIT:
        while (++i2 < count2) {
            list.Add(span[i2]);
        }
    }
}